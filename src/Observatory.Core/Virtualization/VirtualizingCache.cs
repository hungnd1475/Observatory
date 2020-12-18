using Observatory.Core.Persistence.Specifications;
using Observatory.Core.Services;
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
        private readonly Subject<IVirtualizingCacheEvent<TSource>> _cacheSubject =
            new Subject<IVirtualizingCacheEvent<TSource>>();
        private readonly BehaviorSubject<List<TKey>> _keySubject = new BehaviorSubject<List<TKey>>(null);
        private readonly object _lock = new object();

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
        public IObservable<IVirtualizingCacheEvent<TSource>> CacheChanged => _cacheSubject.AsObservable();

        public int Count { get; set; }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public bool IsSynchronized => true;

        public object SyncRoot => throw new NotImplementedException();

        object IList.this[int index]
        {
            get
            {
                var key = _keySubject.Value[index];
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
                .Select(Normalize)
                .Synchronize(_lock)
                .Where(newRanges => Differs(CurrentBlocks, newRanges))
                .Subscribe(newRanges =>
                {
                    var discardedItems = Purge(CurrentBlocks, newRanges);
                    CurrentBlocks = UpdateBlocks(CurrentBlocks, newRanges, source);
                    _cacheSubject.OnNext(new VirtualizingCacheRangesUpdatedEvent<TSource>(discardedItems));
                    foreach (var b in CurrentBlocks)
                    {
                        b.Subscribe(_cacheSubject);
                    }
                    this.Log().Debug($"Tracking {CurrentBlocks.Length} block(s): [{string.Join(",", CurrentBlocks.Select(b => b.Range))}]");
                })
                .DisposeWith(_disposables);

            sourceChanged
                .ObserveOn(RxApp.TaskpoolScheduler)
                .WithLatestFrom(_keySubject, (changes, keys) => (Changes: changes, Keys: keys))
                .SkipWhile(x => x.Keys == null)
                .Synchronize(_lock)
                .Subscribe(x =>
                {
                    var totalCount = source.GetTotalCount();
                    var logicalChanges = x.Changes.Select(c =>
                    {
                        int currentIndex, previousIndex;
                        switch (c.State)
                        {
                            case DeltaState.Add:
                                currentIndex = source.IndexOf(c.Entity);
                                return LogicalChange.Addition(c.Entity, currentIndex);
                            case DeltaState.Remove:
                                previousIndex = x.Keys.IndexOf(c.Entity.Id);
                                return LogicalChange.Removal(this[previousIndex], previousIndex);
                            case DeltaState.Update:
                                currentIndex = source.IndexOf(c.Entity);
                                previousIndex = x.Keys.IndexOf(c.Entity.Id);
                                return LogicalChange.Update(c.Entity, currentIndex, this[previousIndex], previousIndex);
                            default:
                                throw new NotSupportedException($"{c.State} is not supported.");
                        }
                    }).ToArray();

                    var (newBlocks, discardedItems) = RefreshBlocks(CurrentBlocks, totalCount, logicalChanges, source);
                    CurrentBlocks = newBlocks;

                    var serializedChanges = LogicalChange.Serialize(logicalChanges);
                    foreach (var c in serializedChanges)
                    {
                        switch (c.State)
                        {
                            case DeltaState.Add:
                                x.Keys.Insert(c.CurrentIndex.Value, c.CurrentItem.Id);
                                break;
                            case DeltaState.Remove:
                                x.Keys.RemoveAt(c.PreviousIndex.Value);
                                break;
                            case DeltaState.Update:
                                if (c.PreviousIndex.Value != c.CurrentIndex.Value)
                                {
                                    x.Keys.RemoveAt(c.PreviousIndex.Value);
                                    x.Keys.Insert(c.CurrentIndex.Value, c.CurrentItem.Id);
                                }
                                break;
                        }
                    }

                    _keySubject.OnNext(x.Keys);
                    _cacheSubject.OnNext(new VirtualizingCacheSourceUpdatedEvent<TSource>(discardedItems, serializedChanges, totalCount));

                    foreach (var b in CurrentBlocks)
                    {
                        b.Subscribe(_cacheSubject);
                    }

                    this.Log().Debug($"Changes:\n{string.Join("\n", serializedChanges)}");
                })
                .DisposeWith(_disposables);

            _cacheSubject
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(e => e.Process(this))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(targetEvents =>
                {
                    foreach (var e in targetEvents)
                    {
                        CollectionChanged?.Invoke(this, e);
                    }
                })
                .DisposeWith(_disposables);

            Observable.Start(() => source.GetAllKeys(), RxApp.TaskpoolScheduler)
                .Subscribe(keys =>
                {
                    _keySubject.OnNext(keys);
                    _cacheSubject.OnNext(new VirtualizingCacheInitializedEvent<TSource>(keys.Count));
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
                                _targetCache[key],
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
        public int IndexOf(TKey key) => _keySubject.Value?.IndexOf(key) ?? -1;

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

        public bool Contains(object value) => IndexOf((value as TTarget).Id) != -1;

        public int IndexOf(object value) => IndexOf((value as TTarget).Id);

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
                        requests.Add(VirtualizingCacheBlockRequest<TSource>.FromSource(leftDiff.Value, source));
                    }
                    if (rightDiff.HasValue)
                    {
                        i += 1;
                    }
                }

                if (rightDiff.HasValue)
                {
                    requests.Add(VirtualizingCacheBlockRequest<TSource>.FromSource(rightDiff.Value, source));
                }

                newBlocks[j] = new VirtualizingCacheBlock<TSource>(newRanges[j], newItems, requests);
                j += 1;
            }

            return newBlocks;
        }

        /// <summary>
        /// Refreshes the current blocks when changes happened to the source.
        /// </summary>
        /// <param name="oldBlocks">The current blocks to be refreshed.</param>
        /// <param name="totalCount">The new total count of items in the source.</param>
        /// <param name="logicalChanges">The changes happened to the source.</param>
        /// <param name="source">The source where items are retrieved from.</param>
        /// <returns>A new array of <see cref="VirtualizingCacheBlock{T}"/> reflecting the changes.</returns>
        private static (VirtualizingCacheBlock<TSource>[], IReadOnlyList<TSource>) RefreshBlocks(VirtualizingCacheBlock<TSource>[] oldBlocks,
            int totalCount, LogicalChange[] logicalChanges,
            IVirtualizingSource<TSource, TKey> source)
        {
            var removals = logicalChanges.Where(c => c.State != DeltaState.Add)
                .Select(c => new PhysicalChange(PhysicalChangeAction.Remove, c.PreviousEntity, c.PreviousIndex.Value))
                .OrderBy(c => c.Index)
                .ToArray();
            var additions = logicalChanges.Where(c => c.State != DeltaState.Remove)
                .Select(c => new PhysicalChange(PhysicalChangeAction.Add, c.CurrentEntity, c.CurrentIndex.Value))
                .OrderBy(c => c.Index)
                .ToArray();

            var newBlocks = new List<VirtualizingCacheBlock<TSource>>(oldBlocks.Length);
            var discardedItems = new List<TSource>();
            var removalIndex = 0;
            var additionIndex = 0;
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
                    if (removalIndex < removals.Length && removals[removalIndex].Index < oldBlock.Range.FirstIndex)
                    {
                        shift -= 1;
                        removalIndex += 1;
                        shouldStop = false;
                    }
                    if (additionIndex < additions.Length && additions[additionIndex].Index < newRange.FirstIndex)
                    {
                        shift += 1;
                        additionIndex += 1;
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
                    if (additionIndex < additions.Length && additions[additionIndex].Index < destinationIndex)
                    {
                        var currentAddition = additions[additionIndex];
                        newItems[currentAddition.Index - newRange.FirstIndex] = currentAddition.Entity;
                        additionIndex += 1;
                        destinationIndex += 1;
                        shift += 1;

                        if (requestIndex == currentAddition.Index)
                        {
                            requestIndex += 1;
                        }
                        else if (requestIndex < currentAddition.Index)
                        {
                            var range = new IndexRange(requestIndex, currentAddition.Index - 1);
                            newRequests.Add(VirtualizingCacheBlockRequest<TSource>.FromSource(range, source));
                            requestIndex = currentAddition.Index + 1;
                        }
                    }
                    else
                    {
                        var range = new IndexRange(requestIndex, destinationIndex - 1);
                        newRequests.Add(VirtualizingCacheBlockRequest<TSource>.FromSource(range, source));
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
                    PhysicalChange? currentChange = null;
                    if (removalIndex < removals.Length && removals[removalIndex].Index <= oldBlock.Range.LastIndex &&
                        additionIndex < additions.Length && additions[additionIndex].Index <= newRange.LastIndex)
                    {
                        if (removals[removalIndex].Index <= additions[additionIndex].Index)
                        {
                            currentChange = removals[removalIndex];
                            removalIndex += 1;
                        }
                        else
                        {
                            currentChange = additions[additionIndex];
                            additionIndex += 1;
                        }
                    }
                    else if (removalIndex < removals.Length && removals[removalIndex].Index <= oldBlock.Range.LastIndex)
                    {
                        currentChange = removals[removalIndex];
                        removalIndex += 1;
                    }
                    else if (additionIndex < additions.Length && additions[additionIndex].Index <= newRange.LastIndex)
                    {
                        currentChange = additions[additionIndex];
                        additionIndex += 1;
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
                    newRequests.Add(VirtualizingCacheBlockRequest<TSource>.FromSource(range, source));
                }

                // discards any remaining items from source if necessary
                if (sourceIndex <= oldBlock.Range.LastIndex)
                {
                    var range = new IndexRange(sourceIndex, oldBlock.Range.LastIndex);
                    var items = oldBlock.Slice(range).ToArray();
                    discardedItems.AddRange(items);
                }

                // copy the items, transfering any overlapping pending requests
                foreach (var (SourceRange, DestinationRange) in copyRanges)
                {
                    oldBlock.Slice(SourceRange).CopyTo(newRange.Slice(newItems, DestinationRange));
                    foreach (var oldRequest in oldBlock.Requests.Where(r => !r.IsReceived))
                    {
                        var sourceEffectiveRange = oldRequest.FullRange.Intersect(SourceRange);
                        if (sourceEffectiveRange.HasValue)
                        {
                            var destinationEffectiveRange = new IndexRange(
                                DestinationRange.FirstIndex + sourceEffectiveRange.Value.FirstIndex - SourceRange.FirstIndex,
                                DestinationRange.FirstIndex + sourceEffectiveRange.Value.Length - 1);
                            newRequests.Add(VirtualizingCacheBlockRequest<TSource>.FromSource(destinationEffectiveRange, source));
                        }
                    }
                }

                newBlocks.Add(new VirtualizingCacheBlock<TSource>(newRange, newItems, newRequests));
            }
            return (newBlocks.ToArray(), discardedItems.AsReadOnly());
        }

        /// <summary>
        /// Sorts then compacts a given array of ranges.
        /// </summary>
        /// <param name="ranges">The ranges.</param>
        /// <returns>A new array of ranges that are sorted and compacted.</returns>
        private static IndexRange[] Normalize(IndexRange[] ranges)
        {
            var sortedRanges = ranges.OrderBy(r => r.FirstIndex).ToList();
            var normalizedRanges = new List<IndexRange>();
            if (sortedRanges.Count > 0)
            {
                var anchor = sortedRanges[0];
                var index = 1;

                while (true)
                {
                    if (index >= sortedRanges.Count)
                    {
                        normalizedRanges.Add(anchor);
                        break;
                    }

                    var current = sortedRanges[index++];
                    if (!anchor.TryUnion(current, ref anchor))
                    {
                        normalizedRanges.Add(anchor);
                        anchor = current;
                    }
                }
            }
            return normalizedRanges.ToArray();
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

        private readonly struct LogicalChange
        {
            public readonly DeltaState State;
            public readonly TSource CurrentEntity;
            public readonly TSource PreviousEntity;
            public readonly int? CurrentIndex;
            public readonly int? PreviousIndex;

            private LogicalChange(DeltaState state, TSource currentEntity, TSource previousEntity, int? currentIndex, int? previousIndex)
            {
                State = state;
                CurrentEntity = currentEntity;
                PreviousEntity = previousEntity;
                CurrentIndex = currentIndex;
                PreviousIndex = previousIndex;
            }

            public static LogicalChange Addition(TSource currentEntity, int currentIndex)
            {
                return new LogicalChange(DeltaState.Add, currentEntity, null, currentIndex, null);
            }

            public static LogicalChange Removal(TSource previousEntity, int previousIndex)
            {
                return new LogicalChange(DeltaState.Remove, null, previousEntity, null, previousIndex);
            }

            public static LogicalChange Update(TSource currentEntity, int currentIndex, TSource previousEntity, int previousIndex)
            {
                return new LogicalChange(DeltaState.Update, currentEntity, previousEntity, currentIndex, previousIndex);
            }

            public static IReadOnlyList<VirtualizingCacheSourceChange<TSource>> Serialize(LogicalChange[] logicalChanges)
            {
                var serializedChanges = new List<VirtualizingCacheSourceChange<TSource>>();
                var additions = logicalChanges.Where(c => c.State == DeltaState.Add)
                    .OrderBy(c => c.CurrentIndex.Value)
                    .ToArray();
                var removalsAndUpdates = logicalChanges.Where(c => c.State != DeltaState.Add)
                    .OrderBy(c => c.PreviousIndex.Value)
                    .ToArray();

                var additionIndex = 0;
                var removalAndUpdateIndex = 0;
                var shift = 0;

                while (true)
                {
                    LogicalChange? currentChange = null;
                    if (additionIndex < additions.Length && removalAndUpdateIndex < removalsAndUpdates.Length)
                    {
                        if (additions[additionIndex].CurrentIndex.Value < removalsAndUpdates[removalAndUpdateIndex].PreviousIndex.Value + shift)
                        {
                            currentChange = additions[additionIndex];
                            additionIndex += 1;
                        }
                        else
                        {
                            currentChange = removalsAndUpdates[removalAndUpdateIndex];
                            removalAndUpdateIndex += 1;
                        }
                    }
                    else if (additionIndex < additions.Length)
                    {
                        currentChange = additions[additionIndex];
                        additionIndex += 1;
                    }
                    else if (removalAndUpdateIndex < removalsAndUpdates.Length)
                    {
                        currentChange = removalsAndUpdates[removalAndUpdateIndex];
                        removalAndUpdateIndex += 1;
                    }

                    if (currentChange.HasValue)
                    {
                        switch (currentChange.Value.State)
                        {
                            case DeltaState.Add:
                                serializedChanges.Add(VirtualizingCacheSourceChange<TSource>.Addition(
                                    currentChange.Value.CurrentEntity,
                                    currentChange.Value.CurrentIndex.Value));
                                shift += 1;
                                break;
                            case DeltaState.Remove:
                                serializedChanges.Add(VirtualizingCacheSourceChange<TSource>.Removal(
                                    currentChange.Value.PreviousEntity,
                                    currentChange.Value.PreviousIndex.Value + shift));
                                shift -= 1;
                                break;
                            case DeltaState.Update:
                                serializedChanges.Add(VirtualizingCacheSourceChange<TSource>.Update(
                                    currentChange.Value.CurrentEntity,
                                    currentChange.Value.CurrentIndex.Value,
                                    currentChange.Value.PreviousEntity,
                                    currentChange.Value.PreviousIndex.Value + shift));
                                break;
                        }
                    }
                    else
                        break;
                }

                return serializedChanges;
            }
        }

        private readonly struct PhysicalChange
        {
            public readonly PhysicalChangeAction Action;
            public readonly TSource Entity;
            public readonly int Index;

            public PhysicalChange(PhysicalChangeAction action, TSource entity, int index)
            {
                Action = action;
                Entity = entity;
                Index = index;
            }
        }

        private enum PhysicalChangeAction
        {
            Add,
            Remove,
        }
    }
}
