﻿using Observatory.Core.Persistence.Specifications;
using Observatory.Core.Services;
using Observatory.Core.Services.ChangeTracking;
using Observatory.Core.Virtualization.Internals;
using ReactiveUI;
using Splat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a cache of items with virtualization capability.
    /// </summary>
    /// <typeparam name="TSource">The type of items retrieved from source.</typeparam>
    /// <typeparam name="TTarget">The type of items displayed on the UI.</typeparam>
    /// <typeparam name="TKey">The type of key of both <typeparamref name="TSource"/> and <typeparamref name="TTarget"/>.</typeparam>
    public class VirtualizingCache<TSource, TTarget, TKey> : IList, INotifyCollectionChanged, IDisposable, IEnableLogger,
        IVirtualizingCacheEventProcessor<TSource, IEnumerable<NotifyCollectionChangedEventArgs>>
        where TSource : class, IVirtualizableSource<TKey>
        where TTarget : class, IVirtualizableTarget<TSource, TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<IndexRange[]> _rangeSubject = new Subject<IndexRange[]>();
        private readonly Subject<IVirtualizingCacheEvent<TSource>> _eventSubject
            = new Subject<IVirtualizingCacheEvent<TSource>>();
        private readonly object _lock = new object();
        private readonly List<TKey> _keys = new List<TKey>();
        private readonly BehaviorSubject<IndexRange[]> _selectionSubject
            = new BehaviorSubject<IndexRange[]>(new IndexRange[0]);
        private readonly Dictionary<TKey, TTarget> _targetCache = new Dictionary<TKey, TTarget>();
        private readonly Func<TSource, TTarget> _targetFactory;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets the current blocks the cache is tracking.
        /// </summary>
        public VirtualizingCacheBlock<TSource>[] CurrentBlocks { get; private set; } = new VirtualizingCacheBlock<TSource>[0];

        /// <summary>
        /// Gets an observable stream of <see cref="IVirtualizingCacheEvent{T}"/> fired whenever there are changes happened in the cache.
        /// </summary>
        public IObservable<IVirtualizingCacheEvent<TSource>> CacheChanged => _eventSubject.AsObservable();

        /// <summary>
        /// Gets an observable stream of selected ranges.
        /// </summary>
        public IObservable<IReadOnlyList<IndexRange>> SelectionChanged => _selectionSubject.AsObservable();

        public int Count { get; set; }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public bool IsSynchronized => true;

        public object SyncRoot => throw new NotImplementedException();

        object IList.this[int index]
        {
            get
            {
                var key = _keys[index];
                return _targetCache.ContainsKey(key) ? _targetCache[key] : null;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item instance if the index is within the ranges the cache is tracking, otherwise the default value of type <typeparamref name="TSource"/>.</returns>
        public TSource this[int index] => SearchItem(CurrentBlocks, index);

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCache{TSource, TTarget, TKey}"/>.
        /// </summary>
        /// <param name="source">The source where items are retrieved from.</param>
        public VirtualizingCache(IVirtualizingSource<TSource, TKey> source,
            IObservable<IReadOnlyList<DeltaEntity<TSource>>> sourceChanged,
            Func<TSource, TTarget> targetFactory)
        {
            _targetFactory = targetFactory;

            _rangeSubject
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .Select(newRanges => newRanges.Normalize())
                .Synchronize(_lock)
                .Where(newRanges => Differs(CurrentBlocks, newRanges))
                .Subscribe(newRanges =>
                {
                    var discardedItems = Purge(CurrentBlocks, newRanges);

                    CurrentBlocks = UpdateBlocks(CurrentBlocks, newRanges, source);
                    _eventSubject.OnNext(new VirtualizingCacheRangesUpdatedEvent<TSource>(discardedItems));

                    foreach (var b in CurrentBlocks)
                        b.Subscribe(_eventSubject);

                    this.Log().Debug($"Tracking {CurrentBlocks.Length} block(s): [{string.Join(",", CurrentBlocks.Select(b => b.Range))}]");
                })
                .DisposeWith(_disposables);

            sourceChanged
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Synchronize(_lock)
                .SkipWhile(_ => _keys == null)
                .Subscribe(changes =>
                {
                    var totalCount = source.GetTotalCount();
                    var logicalChanges = changes.Select(c =>
                    {
                        int currentIndex, previousIndex;
                        switch (c.State)
                        {
                            case DeltaState.Add:
                                currentIndex = source.IndexOf(c.Entity);
                                return LogicalChange.Addition(c.Entity, currentIndex);
                            case DeltaState.Remove:
                                previousIndex = _keys.IndexOf(c.Entity.Id);
                                return LogicalChange.Removal(this[previousIndex], previousIndex);
                            case DeltaState.Update:
                                currentIndex = source.IndexOf(c.Entity);
                                previousIndex = _keys.IndexOf(c.Entity.Id);
                                return LogicalChange.Update(c.Entity, currentIndex, this[previousIndex], previousIndex);
                            default:
                                throw new NotSupportedException($"{c.State} is not supported.");
                        }
                    }).ToArray();

                    var (newBlocks, discardedItems) = ApplyChanges(CurrentBlocks, totalCount,
                        logicalChanges.ToPhysicalChangeSet(), source);

                    var serializedChanges = logicalChanges.Serialize();
                    foreach (var c in serializedChanges)
                    {
                        switch (c.State)
                        {
                            case DeltaState.Add:
                                _keys.Insert(c.CurrentIndex.Value, c.CurrentItem.Id);
                                break;
                            case DeltaState.Remove:
                                _keys.RemoveAt(c.PreviousIndex.Value);
                                break;
                            case DeltaState.Update:
                                if (c.PreviousIndex.Value != c.CurrentIndex.Value)
                                {
                                    _keys.RemoveAt(c.PreviousIndex.Value);
                                    _keys.Insert(c.CurrentIndex.Value, c.CurrentItem.Id);
                                }
                                break;
                        }
                    }

                    CurrentBlocks = newBlocks;
                    _eventSubject.OnNext(new VirtualizingCacheSourceUpdatedEvent<TSource>(
                        discardedItems, serializedChanges, totalCount));

                    foreach (var b in CurrentBlocks)
                        b.Subscribe(_eventSubject);

                    this.Log().Debug($"Changes:\n{string.Join("\n", serializedChanges)}");
                })
                .DisposeWith(_disposables);

            _eventSubject
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(e => e.Process(this))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(targetEvents =>
                {
                    foreach (var e in targetEvents)
                    {
                        _selectionSubject.OnNext(e.ApplyToSelection(_selectionSubject.Value));
                        CollectionChanged?.Invoke(this, e);
                    }
                })
                .DisposeWith(_disposables);

            Observable.Start(() => source.GetAllKeys(), RxApp.TaskpoolScheduler)
                .Subscribe(keys =>
                {
                    _keys.AddRange(keys);
                    _eventSubject.OnNext(new VirtualizingCacheInitializedEvent<TSource>(keys.Count));
                })
                .DisposeWith(_disposables);
        }

        /// <summary>
        /// Update the ranges of items the cache holds. Based on the newly requested ranges, the cache
        /// will figure out which items to load from source and which items are longer needed. Call this function whenever the UI
        /// needs to display a new set of items.
        /// </summary>
        /// <param name="newRanges">The ranges of items that need to be displayed.</param>
        public void UpdateRanges(IndexRange[] newRanges)
        {
            _rangeSubject.OnNext(newRanges);
        }

        public void SelectRange(IndexRange range)
        {
            if (range.Length <= 0) return;
            _selectionSubject.OnNext(_selectionSubject.Value.Merge(range));
        }

        public void DeselectRange(IndexRange range)
        {
            if (range.Length == 0) return;
            _selectionSubject.OnNext(_selectionSubject.Value.Subtract(range));
        }

        public bool IsSelected(int index) => _selectionSubject.Value.Contains(index);

        public IReadOnlyList<IndexRange> GetSelectedRanges()
        {
            this.Log().Debug($"Current selection: {string.Join(", ", _selectionSubject.Value)}.");
            return _selectionSubject.Value;
        }

        public IReadOnlyList<TKey> GetSelectedKeys() => _selectionSubject.Value.EnumerateIndex().Select(i => _keys[i]).ToArray();

        #region ++ IVirtualizingCacheEventProcessor ++

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheInitializedEvent<TSource> e)
        {
            Count = e.TotalCount;
            yield return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheItemsLoadedEvent<TSource> e)
        {
            var events = new List<NotifyCollectionChangedEventArgs>(e.Range.Length);
            foreach (var index in e.Range)
            {
                var source = e.Block[index];
                var target = _targetFactory(source);
                _targetCache[source.Id] = target;
                events.Add(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, target,
                    new VirtualizingCachePlaceholder(index), index));
            }
            return events;
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheRangesUpdatedEvent<TSource> e)
        {
            foreach (var source in e.DiscardedItems.Where(i => i != null))
            {
                var key = source.Id;
                if (_targetCache.ContainsKey(key))
                {
                    (_targetCache[key] as IDisposable)?.Dispose();
                    _targetCache.Remove(key);
                }
            }
            return Enumerable.Empty<NotifyCollectionChangedEventArgs>();
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheSourceUpdatedEvent<TSource> e)
        {
            var events = new List<NotifyCollectionChangedEventArgs>(e.Changes.Count);
            foreach (var c in e.Changes)
            {
                TKey key;
                switch (c.State)
                {
                    case DeltaState.Add:
                        key = c.CurrentItem.Id;
                        _targetCache[key] = _targetFactory(c.CurrentItem);
                        events.Add(new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            _targetCache[key],
                            c.CurrentIndex.Value));
                        break;
                    case DeltaState.Update:
                        key = c.CurrentItem.Id;
                        if (_targetCache.TryGetValue(key, out var target))
                        {
                            RxApp.MainThreadScheduler.Schedule(target, (scheduler, target) =>
                            {
                                target.Refresh(c.CurrentItem);
                                return Disposable.Empty;
                            });
                        }
                        if (c.PreviousIndex.Value != c.CurrentIndex.Value)
                        {
                            events.Add(new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Move,
                                target,
                                c.CurrentIndex.Value,
                                c.PreviousIndex.Value));
                        }
                        break;
                    case DeltaState.Remove:
                        if (c.PreviousItem != null)
                        {
                            key = c.PreviousItem.Id;
                            if (_targetCache.TryGetValue(key, out var oldItem))
                            {
                                (oldItem as IDisposable)?.Dispose();
                                _targetCache.Remove(key);
                            }
                            events.Add(new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Remove,
                                oldItem,
                                c.PreviousIndex.Value));
                        }
                        else
                        {
                            events.Add(new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Remove,
                                new VirtualizingCachePlaceholder(c.PreviousIndex.Value),
                                c.PreviousIndex.Value));
                        }
                        break;
                }
            }

            foreach (var source in e.DiscardedItems.Where(i => i != null))
            {
                var key = source.Id;
                if (_targetCache.ContainsKey(key))
                {
                    (_targetCache[key] as IDisposable)?.Dispose();
                    _targetCache.Remove(key);
                }
            }

            Count = e.TotalCount;
            return events;
        }

        #endregion

        /// <summary>
        /// Returns the index of a given key.
        /// </summary>
        /// <param name="key">The item to get the index.</param>
        /// <returns>The index if found, otherwise -1.</returns>
        public int IndexOf(TTarget item) => _keys.IndexOf(item.Id);

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            foreach (var t in _targetCache.Values)
            {
                (t as IDisposable)?.Dispose();
            }
            _targetCache.Clear();

            foreach (var b in CurrentBlocks)
            {
                b.Unsubscribe();
            }
            CurrentBlocks = new VirtualizingCacheBlock<TSource>[0];

            _keys.Clear();
            _selectionSubject.OnNext(new IndexRange[0]);
        }

        /// <summary>
        /// Clears and disposes the cache.
        /// </summary>
        public void Dispose()
        {
            Clear();
            _disposables.Dispose();
        }

        public int Add(object value) => throw new NotImplementedException();

        public bool Contains(object value) => IndexOf(value as TTarget) != -1;

        public int IndexOf(object value)
        {
            return value switch
            {
                TTarget x => IndexOf(x),
                VirtualizingCachePlaceholder x => x.Index,
                _ => -1,
            };
        }

        public void Insert(int index, object value) => throw new NotImplementedException();

        public void Remove(object value) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();

        public void CopyTo(Array array, int index) => throw new NotImplementedException();

        public IEnumerator GetEnumerator() => throw new NotImplementedException();

        /// <summary>
        /// Determines if there is any difference between the current ranges and the newly requested ranges.
        /// </summary>
        /// <param name="currentBlocks">The current blocks.</param>
        /// <param name="newRanges">The newly requested ranges.</param>
        /// <returns>True if there is any difference, otherwise false.</returns>
        private static bool Differs(VirtualizingCacheBlock<TSource>[] currentBlocks, IndexRange[] newRanges)
        {
            int i = 0, j = 0;
            while (i < currentBlocks.Length && j < newRanges.Length)
            {
                if (currentBlocks[i].Range.Covers(newRanges[j]))
                {
                    j += 1;
                }
                else
                {
                    var (leftDiff, rightDiff) = currentBlocks[i].Range.Difference(newRanges[j]);
                    if (leftDiff.HasValue)
                    {
                        return true;
                    }
                    else
                    {
                        if (rightDiff.HasValue && rightDiff.Value != newRanges[j])
                        {
                            return true;
                        }
                        else
                        {
                            i += 1;
                        }
                    }
                }
            }

            return j < newRanges.Length;
        }

        /// <summary>
        /// Figures out which ranges should be discarded according the newly requested ranges.
        /// </summary>
        /// <param name="currentBlocks">The current blocks.</param>
        /// <param name="newRanges">The newly requested ranges.</param>
        /// <returns>An array of <see cref="IndexRange"/> that should be discarded.</returns>
        private static IReadOnlyList<TSource> Purge(VirtualizingCacheBlock<TSource>[] currentBlocks, IndexRange[] newRanges)
        {
            var discardedItems = new List<TSource>();
            int i = 0, j = 0;
            while (i < currentBlocks.Length)
            {
                IndexRange? leftDiff, rightDiff = currentBlocks[i].Range;
                while (j < newRanges.Length && rightDiff.HasValue)
                {
                    (leftDiff, rightDiff) = newRanges[j].Difference(rightDiff.Value);
                    if (leftDiff.HasValue)
                    {
                        var range = leftDiff.Value;
                        var items = currentBlocks[i].Slice(range).ToArray();
                        discardedItems.AddRange(items);
                    }
                    if (rightDiff.HasValue)
                    {
                        j += 1;
                    }
                }
                if (rightDiff.HasValue)
                {
                    var range = rightDiff.Value;
                    var items = currentBlocks[i].Slice(range).ToArray();
                    discardedItems.AddRange(items);
                }
                i += 1;
            }
            return discardedItems.AsReadOnly();
        }

        /// <summary>
        /// Updates the current blocks to keep only in memory the items requested by the new ranges.
        /// </summary>
        /// <param name="newRanges">The newly requested ranges.</param>
        /// <param name="oldBlocks">The current blocks.</param>
        /// <param name="source">The source where items are retrieved from.</param>
        /// <returns>An array of <see cref="VirtualizingCacheBlock{T}"/> that tracks only items in the newly requested ranges.</returns>
        private static VirtualizingCacheBlock<TSource>[] UpdateBlocks(VirtualizingCacheBlock<TSource>[] oldBlocks,
            IndexRange[] newRanges,
            IVirtualizingSource<TSource, TKey> source)
        {
            foreach (var b in oldBlocks)
            {
                b.Unsubscribe();
            }

            var newBlocks = new VirtualizingCacheBlock<TSource>[newRanges.Length];
            int i = 0, j = 0;
            while (j < newRanges.Length)
            {
                var requests = new List<VirtualizingCacheBlockRequest<TSource>>();
                var newItems = new TSource[newRanges[j].Length];

                IndexRange? leftDiff, rightDiff = newRanges[j];
                while (i < oldBlocks.Length && rightDiff.HasValue)
                {
                    var oldRange = oldBlocks[i].Range;
                    var intersect = oldRange.Intersect(rightDiff.Value);
                    (leftDiff, rightDiff) = oldRange.Difference(rightDiff.Value);

                    if (intersect.HasValue)
                    {
                        oldBlocks[i].Slice(intersect.Value).CopyTo(newRanges[j].Slice(newItems, intersect.Value));
                        foreach (var r in oldBlocks[i].Requests.Where(r => !r.IsReceived))
                        {
                            var effectiveRange = r.FullRange.Intersect(intersect.Value);
                            if (effectiveRange.HasValue)
                            {
                                requests.Add(r.Transfer(effectiveRange.Value));
                            }
                        }
                    }

                    if (leftDiff.HasValue)
                    {
                        requests.Add(VirtualizingCacheBlockRequest.FromSource(leftDiff.Value, source));
                    }
                    if (rightDiff.HasValue)
                    {
                        i += 1;
                    }
                }

                if (rightDiff.HasValue)
                {
                    requests.Add(VirtualizingCacheBlockRequest.FromSource(rightDiff.Value, source));
                }

                newBlocks[j] = new VirtualizingCacheBlock<TSource>(newRanges[j], newItems, requests);
                j += 1;
            }

            return newBlocks;
        }

        /// <summary>
        /// Applies the changes to the array of cache blocks.
        /// </summary>
        /// <param name="oldBlocks">The current blocks to be refreshed.</param>
        /// <param name="totalCount">The new total count of items in the source.</param>
        /// <param name="changes">The changes to apply.</param>
        /// <param name="source">The source where items are retrieved from.</param>
        /// <returns>A new array of <see cref="VirtualizingCacheBlock{T}"/> reflecting the changes.</returns>
        private static (VirtualizingCacheBlock<TSource>[], IReadOnlyList<TSource>) ApplyChanges(
            VirtualizingCacheBlock<TSource>[] oldBlocks,
            int totalCount, PhysicalChangeSet<TSource> changes,
            IVirtualizingSource<TSource, TKey> source)
        {
            var additions = changes.Additions;
            var removals = changes.Removals;

            var newBlocks = new List<VirtualizingCacheBlock<TSource>>(oldBlocks.Length);
            var discardedItems = new List<TSource>();
            var removalPointer = 0;
            var additionPointer = 0;
            var shift = 0;

            foreach (var oldBlock in oldBlocks)
            {
                oldBlock.Unsubscribe();
                if (oldBlock.Range.FirstIndex >= totalCount)
                {
                    continue;
                }

                var newRange = new IndexRange(oldBlock.Range.FirstIndex, Math.Min(oldBlock.Range.LastIndex, totalCount - 1));
                var newItems = new TSource[newRange.Length];
                var newRequests = new List<VirtualizingCacheBlockRequest<TSource>>();

                // iterates through all changes prior to the current blocks
                while (true)
                {
                    var shouldStop = true;
                    if (removalPointer < removals.Length && removals[removalPointer].Index < oldBlock.Range.FirstIndex)
                    {
                        shift -= 1;
                        removalPointer += 1;
                        shouldStop = false;
                    }
                    if (additionPointer < additions.Length && additions[additionPointer].Index < newRange.FirstIndex)
                    {
                        shift += 1;
                        additionPointer += 1;
                        shouldStop = false;
                    }
                    if (shouldStop) break;
                }

                // the starting index in the source array indicating the position to copy from
                var sourceIndex = oldBlock.Range.FirstIndex - Math.Min(0, shift);
                // the starting index in the destination array indicating the position to copy to
                var destinationIndex = newRange.FirstIndex + Math.Max(0, shift);
                // the pairs of ranges indicating the source and destination
                var copyRanges = new List<(IndexRange SourceRange, IndexRange DestinationRange)>();

                // iterates through all additions within the current block but prior to the destination index
                // to figure out which ranges to request
                var requestIndex = newRange.FirstIndex;
                while (requestIndex < destinationIndex)
                {
                    if (additionPointer < additions.Length && additions[additionPointer].Index < destinationIndex)
                    {
                        var currentAddition = additions[additionPointer];
                        newItems[currentAddition.Index - newRange.FirstIndex] = currentAddition.Entity;
                        additionPointer += 1;
                        destinationIndex += 1;
                        shift += 1;

                        if (requestIndex == currentAddition.Index)
                        {
                            requestIndex += 1;
                        }
                        else if (requestIndex < currentAddition.Index)
                        {
                            var range = new IndexRange(requestIndex, currentAddition.Index - 1);
                            newRequests.Add(VirtualizingCacheBlockRequest.FromSource(range, source));
                            requestIndex = currentAddition.Index + 1;
                        }
                    }
                    else
                    {
                        var range = new IndexRange(requestIndex, destinationIndex - 1);
                        newRequests.Add(VirtualizingCacheBlockRequest.FromSource(range, source));
                        requestIndex = destinationIndex;
                    }
                }

                if (sourceIndex > oldBlock.Range.FirstIndex)
                {
                    var range = new IndexRange(oldBlock.Range.FirstIndex, sourceIndex - 1);
                    var items = oldBlock.Slice(range).ToArray();
                    discardedItems.AddRange(items);
                }

                // iterates through all remaining changes within the current block to figure out which ranges to copy
                while (true)
                {
                    PhysicalChange<TSource>? currentChange = null;
                    if (removalPointer < removals.Length && removals[removalPointer].Index <= oldBlock.Range.LastIndex &&
                        additionPointer < additions.Length && additions[additionPointer].Index <= newRange.LastIndex)
                    {
                        if (removals[removalPointer].Index <= additions[additionPointer].Index)
                        {
                            currentChange = removals[removalPointer];
                            removalPointer += 1;
                        }
                        else
                        {
                            currentChange = additions[additionPointer];
                            additionPointer += 1;
                        }
                    }
                    else if (removalPointer < removals.Length && removals[removalPointer].Index <= oldBlock.Range.LastIndex)
                    {
                        currentChange = removals[removalPointer];
                        removalPointer += 1;
                    }
                    else if (additionPointer < additions.Length && additions[additionPointer].Index <= newRange.LastIndex)
                    {
                        currentChange = additions[additionPointer];
                        additionPointer += 1;
                    }

                    if (currentChange.HasValue)
                    {
                        switch (currentChange.Value.Action)
                        {
                            case PhysicalChangeAction.Add:
                                shift += 1;
                                newItems[currentChange.Value.Index - newRange.FirstIndex] = currentChange.Value.Entity;

                                if (destinationIndex == currentChange.Value.Index)
                                {
                                    destinationIndex += 1;
                                }
                                else
                                {
                                    var length = Math.Min(currentChange.Value.Index - destinationIndex, oldBlock.Range.LastIndex - sourceIndex + 1);
                                    if (length > 0)
                                    {
                                        var sourceRange = new IndexRange(sourceIndex, sourceIndex + length - 1);
                                        var destinationRange = new IndexRange(destinationIndex, destinationIndex + length - 1);
                                        copyRanges.Add((sourceRange, destinationRange));
                                        sourceIndex = sourceRange.LastIndex + 1;
                                        destinationIndex = destinationRange.LastIndex + 1;

                                        if (destinationIndex == currentChange.Value.Index)
                                        {
                                            destinationIndex += 1;
                                        }
                                    }
                                }

                                break;
                            case PhysicalChangeAction.Remove:
                                shift -= 1;

                                if (sourceIndex >= currentChange.Value.Index)
                                {
                                    if (sourceIndex > currentChange.Value.Index)
                                    {
                                        discardedItems.Add(oldBlock[sourceIndex]);
                                    }
                                    sourceIndex += 1;
                                }
                                else
                                {
                                    var length = Math.Min(currentChange.Value.Index - sourceIndex, newRange.LastIndex - destinationIndex + 1);
                                    if (length > 0)
                                    {
                                        var sourceRange = new IndexRange(sourceIndex, sourceIndex + length - 1);
                                        var destinationRange = new IndexRange(destinationIndex, destinationIndex + length - 1);
                                        copyRanges.Add((sourceRange, destinationRange));
                                        sourceIndex = sourceRange.LastIndex + 1;
                                        destinationIndex = destinationRange.LastIndex + 1;

                                        if (sourceIndex == currentChange.Value.Index)
                                        {
                                            sourceIndex += 1;
                                        }
                                    }
                                }

                                break;
                        }
                    }
                    else break;
                }

                // copy any remaining items from source if necessary
                if (destinationIndex <= newRange.LastIndex)
                {
                    var remainingLength = Math.Min(oldBlock.Range.LastIndex - sourceIndex + 1, newRange.LastIndex - destinationIndex + 1);
                    if (remainingLength > 0)
                    {
                        var sourceRange = new IndexRange(sourceIndex, sourceIndex + remainingLength - 1);
                        var destinationRange = new IndexRange(destinationIndex, destinationIndex + remainingLength - 1);
                        copyRanges.Add((sourceRange, destinationRange));
                        sourceIndex = sourceRange.LastIndex + 1;
                        destinationIndex = destinationRange.LastIndex + 1;
                    }
                }

                // request new items for the destination if necessary
                if (destinationIndex <= newRange.LastIndex)
                {
                    var range = new IndexRange(destinationIndex, newRange.LastIndex);
                    newRequests.Add(VirtualizingCacheBlockRequest.FromSource(range, source));
                }

                // discards any remaining items from source if necessary
                if (sourceIndex <= oldBlock.Range.LastIndex)
                {
                    var range = new IndexRange(sourceIndex, oldBlock.Range.LastIndex);
                    var items = oldBlock.Slice(range).ToArray();
                    discardedItems.AddRange(items);
                }

                // copy the items, transfering any overlapping pending requests
                foreach (var (sourceRange, destinationRange) in copyRanges)
                {
                    oldBlock.Slice(sourceRange).CopyTo(newRange.Slice(newItems, destinationRange));
                    foreach (var oldRequest in oldBlock.Requests.Where(r => !r.IsReceived))
                    {
                        var sourceEffectiveRange = oldRequest.FullRange.Intersect(sourceRange);
                        if (sourceEffectiveRange.HasValue)
                        {
                            var destinationEffectiveRange = new IndexRange(
                                destinationRange.FirstIndex + sourceEffectiveRange.Value.FirstIndex - sourceRange.FirstIndex,
                                destinationRange.FirstIndex + sourceEffectiveRange.Value.Length - 1);
                            newRequests.Add(VirtualizingCacheBlockRequest.FromSource(destinationEffectiveRange, source));
                        }
                    }
                }

                newBlocks.Add(new VirtualizingCacheBlock<TSource>(newRange, newItems, newRequests));
            }

            return (newBlocks.ToArray(), discardedItems.AsReadOnly());
        }

        internal static IndexRange[] ApplyChanges(IndexRange[] oldSelection, PhysicalChangeSet<TSource> changes)
        {
            if (oldSelection.Length == 0) return oldSelection;

            var newSelection = new List<IndexRange>(oldSelection.Length + changes.Additions.Length);
            var removalPointer = 0;
            var additionPointer = 0;
            var removalShift = 0;
            var additionShift = 0;

            foreach (var range in oldSelection)
            {
                var newRange = range.Shift(removalShift);
                while (removalPointer < changes.Removals.Length && changes.Removals[removalPointer].Index <= range.LastIndex)
                {
                    if (changes.Removals[removalPointer].Index < range.FirstIndex)
                    {
                        removalShift -= 1;
                        newRange = newRange.Shift(-1);
                    }
                    else
                    {
                        newRange = newRange.ShrinkLast(1);
                    }
                    ++removalPointer;
                }

                if (newRange.Length <= 0)
                {
                    continue;
                }

                newRange = newRange.Shift(additionShift);
                while (additionPointer < changes.Additions.Length && changes.Additions[additionPointer].Index <= newRange.LastIndex)
                {
                    var additionIndex = changes.Additions[additionPointer].Index;
                    if (additionIndex <= newRange.FirstIndex)
                    {
                        additionShift += 1;
                        newRange = newRange.Shift(1);
                    }
                    else
                    {
                        newSelection.Add(new IndexRange(newRange.FirstIndex, additionIndex - 1));
                        newRange = new IndexRange(additionIndex + 1, newRange.LastIndex + 1);
                    }
                    ++additionPointer;
                }

                newSelection.Add(newRange);
            }

            return newSelection.ToArray();
        }

        /// <summary>
        /// Performs binary search on the current blocks to get the item at a given index.
        /// </summary>
        /// <param name="blocks">The blocks to search.</param>
        /// <param name="index">The index of the item to search for.</param>
        /// <returns>The item instance if the index is within the ranges the cache is tracking, otherwise the default value of type <typeparamref name="TSource"/>.</returns>
        private static TSource SearchItem(VirtualizingCacheBlock<TSource>[] blocks, int index)
        {
            var startIndex = 0;
            var endIndex = blocks.Length - 1;

            while (startIndex <= endIndex)
            {
                var midIndex = (startIndex + endIndex) / 2;
                var midBlock = blocks[midIndex];

                if (midBlock.ContainsIndex(index))
                {
                    return midBlock[index];
                }
                else if (index < midBlock.Range.FirstIndex)
                {
                    endIndex = midIndex - 1;
                }
                else
                {
                    startIndex = midIndex + 1;
                }
            }

            return default;
        }

        //private readonly struct LogicalChange
        //{
        //    public readonly DeltaState State;
        //    public readonly TSource CurrentEntity;
        //    public readonly TSource PreviousEntity;
        //    public readonly int? CurrentIndex;
        //    public readonly int? PreviousIndex;

        //    private LogicalChange(DeltaState state, TSource currentEntity, TSource previousEntity, int? currentIndex, int? previousIndex)
        //    {
        //        State = state;
        //        CurrentEntity = currentEntity;
        //        PreviousEntity = previousEntity;
        //        CurrentIndex = currentIndex;
        //        PreviousIndex = previousIndex;
        //    }

        //    public static LogicalChange Addition(TSource currentEntity, int currentIndex)
        //    {
        //        return new LogicalChange(DeltaState.Add, currentEntity, null, currentIndex, null);
        //    }

        //    public static LogicalChange Removal(TSource previousEntity, int previousIndex)
        //    {
        //        return new LogicalChange(DeltaState.Remove, null, previousEntity, null, previousIndex);
        //    }

        //    public static LogicalChange Update(TSource currentEntity, int currentIndex, TSource previousEntity, int previousIndex)
        //    {
        //        return new LogicalChange(DeltaState.Update, currentEntity, previousEntity, currentIndex, previousIndex);
        //    }

        //    /// <summary>
        //    /// Rearranges the logical changes in an order that can be applied one-by-one by the UI.
        //    /// </summary>
        //    /// <param name="logicalChanges">The logical changes.</param>
        //    /// <returns></returns>
        //    public static IReadOnlyList<VirtualizingCacheSourceChange<TSource>> Rearrange(LogicalChange[] logicalChanges)
        //    {
        //        var orderedChanges = new List<VirtualizingCacheSourceChange<TSource>>();
        //        var additions = logicalChanges.Where(c => c.State == DeltaState.Add)
        //            .OrderBy(c => c.CurrentIndex.Value)
        //            .ToArray();
        //        var removalsAndUpdates = logicalChanges.Where(c => c.State != DeltaState.Add)
        //            .OrderBy(c => c.PreviousIndex.Value)
        //            .ToArray();

        //        var additionIndex = 0;
        //        var removalAndUpdateIndex = 0;
        //        var shift = 0;

        //        while (true)
        //        {
        //            LogicalChange? currentChange = null;
        //            if (additionIndex < additions.Length && removalAndUpdateIndex < removalsAndUpdates.Length)
        //            {
        //                if (additions[additionIndex].CurrentIndex.Value < removalsAndUpdates[removalAndUpdateIndex].PreviousIndex.Value + shift)
        //                {
        //                    currentChange = additions[additionIndex];
        //                    additionIndex += 1;
        //                }
        //                else
        //                {
        //                    currentChange = removalsAndUpdates[removalAndUpdateIndex];
        //                    removalAndUpdateIndex += 1;
        //                }
        //            }
        //            else if (additionIndex < additions.Length)
        //            {
        //                currentChange = additions[additionIndex];
        //                additionIndex += 1;
        //            }
        //            else if (removalAndUpdateIndex < removalsAndUpdates.Length)
        //            {
        //                currentChange = removalsAndUpdates[removalAndUpdateIndex];
        //                removalAndUpdateIndex += 1;
        //            }

        //            if (currentChange.HasValue)
        //            {
        //                switch (currentChange.Value.State)
        //                {
        //                    case DeltaState.Add:
        //                        orderedChanges.Add(VirtualizingCacheSourceChange<TSource>.Addition(
        //                            currentChange.Value.CurrentEntity,
        //                            currentChange.Value.CurrentIndex.Value));
        //                        shift += 1;
        //                        break;
        //                    case DeltaState.Remove:
        //                        orderedChanges.Add(VirtualizingCacheSourceChange<TSource>.Removal(
        //                            currentChange.Value.PreviousEntity,
        //                            currentChange.Value.PreviousIndex.Value + shift));
        //                        shift -= 1;
        //                        break;
        //                    case DeltaState.Update:
        //                        orderedChanges.Add(VirtualizingCacheSourceChange<TSource>.Update(
        //                            currentChange.Value.CurrentEntity,
        //                            currentChange.Value.CurrentIndex.Value,
        //                            currentChange.Value.PreviousEntity,
        //                            currentChange.Value.PreviousIndex.Value + shift));
        //                        break;
        //                }
        //            }
        //            else
        //                break;
        //        }

        //        return orderedChanges.AsReadOnly();
        //    }
        //}

        /// <summary>
        /// Represents a "physical" change to a collection. The only difference between this and <see cref="LogicalChange"/>
        /// is that an update is treated as a removal (of the old item) followed by an addition (of the new item with updated information).
        /// This treatment allows updated items to move to different positions and makes it easier to apply the changes to the array of
        /// cache blocks.
        /// </summary>
        //private readonly struct PhysicalChange
        //{
        //    /// <summary>
        //    /// Gets the action of the change.
        //    /// </summary>
        //    public readonly PhysicalChangeAction Action;

        //    /// <summary>
        //    /// Gets the entity affected by the change.
        //    /// </summary>
        //    public readonly TSource Entity;

        //    /// <summary>
        //    /// Gets the index of the change.
        //    /// </summary>
        //    public readonly int Index;

        //    /// <summary>
        //    /// Constructs an instance of <see cref="PhysicalChange"/>.
        //    /// </summary>
        //    /// <param name="action"></param>
        //    /// <param name="entity"></param>
        //    /// <param name="index"></param>
        //    public PhysicalChange(PhysicalChangeAction action, TSource entity, int index)
        //    {
        //        Action = action;
        //        Entity = entity;
        //        Index = index;
        //    }
        //}

        ///// <summary>
        ///// Represents an action of a physical change.
        ///// </summary>
        //private enum PhysicalChangeAction
        //{
        //    /// <summary>
        //    /// The physical change is an addition.
        //    /// </summary>
        //    Add,
        //    /// <summary>
        //    /// The physical change is a removal.
        //    /// </summary>
        //    Remove,
        //}

        ///// <summary>
        ///// Represents a collection of <see cref="PhysicalChange"/>.
        ///// </summary>
        //private readonly struct PhysicalChangeSet
        //{
        //    /// <summary>
        //    /// Gets the changes that are additions.
        //    /// </summary>
        //    public readonly PhysicalChange[] Additions;

        //    /// <summary>
        //    /// Gets the changes that are removals.
        //    /// </summary>
        //    public readonly PhysicalChange[] Removals;

        //    /// <summary>
        //    /// Constructs an instance of <see cref="PhysicalChangeSet"/> from an array of <see cref="LogicalChange"/>.
        //    /// </summary>
        //    /// <param name="logicalChanges">The array of <see cref="LogicalChange"/>.</param>
        //    public PhysicalChangeSet(LogicalChange<TSource>[] logicalChanges)
        //    {
        //        Additions = logicalChanges.Where(c => c.State != DeltaState.Remove)
        //            .Select(c => new PhysicalChange(PhysicalChangeAction.Add, c.CurrentEntity, c.CurrentIndex.Value))
        //            .OrderBy(c => c.Index)
        //            .ToArray();
        //        Removals = logicalChanges.Where(c => c.State != DeltaState.Add)
        //            .Select(c => new PhysicalChange(PhysicalChangeAction.Remove, c.PreviousEntity, c.PreviousIndex.Value))
        //            .OrderBy(c => c.Index)
        //            .ToArray();
        //    }
        //}
    }
}
