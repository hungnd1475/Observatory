using Observatory.Core.Persistence.Specifications;
using Observatory.Core.Services;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a cache of items with virtualization capability.
    /// </summary>
    /// <typeparam name="T">The type of items retrieved from source.</typeparam>
    public class VirtualizingCache<T> : IDisposable, IEnableLogger
        where T : class
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<IndexRange[]> _rangesObserver = new Subject<IndexRange[]>();
        private readonly ReplaySubject<IVirtualizingCacheEvent<T>> _cacheObserver =
            new ReplaySubject<IVirtualizingCacheEvent<T>>();
        private readonly object _lock = new object();

        /// <summary>
        /// Gets the current blocks the cache is tracking.
        /// </summary>
        public VirtualizingCacheBlock<T>[] CurrentBlocks { get; private set; } = new VirtualizingCacheBlock<T>[0];

        /// <summary>
        /// Gets an observable stream of <see cref="IVirtualizingCacheEvent{T}"/> fired whenever there are changes happened in the cache.
        /// </summary>
        public IObservable<IVirtualizingCacheEvent<T>> WhenCacheChanged => _cacheObserver.AsObservable();

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item instance if the index is within the ranges the cache is tracking, otherwise the default value of type <typeparamref name="T"/>.</returns>
        public T this[int index] => SearchItem(CurrentBlocks, index);

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> of all the indices the cache is holding.
        /// </summary>
        public IEnumerable<int> Indices => CurrentBlocks.Select(b => b.Range).SelectMany(r => r);

        /// <summary>
        /// Gets the equality comparer for items stored by the cache.
        /// </summary>
        public IEqualityComparer<T> ItemComparer { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCache{T}"/>.
        /// </summary>
        /// <param name="source">The source where items are retrieved from.</param>
        public VirtualizingCache(IVirtualizingSource<T> source,
            IObservable<IReadOnlyList<DeltaEntity<T>>> sourceObserver,
            IEqualityComparer<T> itemComparer)
        {
            ItemComparer = itemComparer;

            _rangesObserver
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .Select(Normalize)
                .Synchronize(_lock)
                .Where(newRanges => Differs(CurrentBlocks, newRanges))
                .Subscribe(newRanges =>
                {
                    var discardedItems = Purge(CurrentBlocks, newRanges);
                    CurrentBlocks = UpdateBlocks(CurrentBlocks, newRanges, source);
                    _cacheObserver.OnNext(new VirtualizingCacheRangesUpdatedEvent<T>(discardedItems));
                    foreach (var b in CurrentBlocks)
                    {
                        b.Subscribe(_cacheObserver);
                    }
                })
                .DisposeWith(_disposables);

            sourceObserver
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Synchronize(_lock)
                .Select(changes => changes.Select(c =>
                {
                    int currentIndex, previousIndex;
                    switch (c.State)
                    {
                        case DeltaState.Add:
                            currentIndex = source.IndexOf(c.Entity);
                            return LogicalChange.Addition(c.Entity, currentIndex);
                        case DeltaState.Remove:
                            previousIndex = IndexOf(c.Entity, itemComparer);
                            return LogicalChange.Removal(this[previousIndex], previousIndex);
                        case DeltaState.Update:
                            currentIndex = source.IndexOf(c.Entity);
                            previousIndex = IndexOf(c.Entity, itemComparer);
                            return LogicalChange.Update(c.Entity, currentIndex, this[previousIndex], previousIndex);
                        default:
                            throw new NotSupportedException($"{c.State} is not supported.");
                    }
                }).ToArray())
                .Subscribe(changes =>
                {
                    var totalCount = source.GetTotalCount();
                    var (newBlocks, discardedItems) = RefreshBlocks(CurrentBlocks, totalCount, changes, source);
                    var serializedChanges = LogicalChange.Serialize(changes);

                    CurrentBlocks = newBlocks;
                    _cacheObserver.OnNext(new VirtualizingCacheSourceUpdatedEvent<T>(discardedItems, serializedChanges, totalCount));

                    foreach (var b in CurrentBlocks)
                    {
                        b.Subscribe(_cacheObserver);
                    }

                    this.Log().Debug($"Changes:\n{string.Join("\n", serializedChanges)}");
                })
                .DisposeWith(_disposables);

            Observable.Start(() => source.GetTotalCount(), RxApp.TaskpoolScheduler)
                .Subscribe(totalCount => _cacheObserver.OnNext(new VirtualizingCacheInitializedEvent<T>(totalCount)))
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
            _rangesObserver.OnNext(newRanges);
        }

        /// <summary>
        /// Returns the index of a given item based on a given equality comparer.
        /// </summary>
        /// <param name="item">The item to get the index.</param>
        /// <param name="itemComparer">The equality comparer to find item in the cache.</param>
        /// <returns>The index if found, otherwise -1.</returns>
        public int IndexOf(T item, IEqualityComparer<T> itemComparer)
        {
            foreach (var b in CurrentBlocks)
            {
                var index = b.IndexOf(item, itemComparer);
                if (index != -1) return index;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of a given item based on reference equality.
        /// </summary>
        /// <param name="item">The item to get the index.</param>
        /// <returns>The index if found, otherwise -1.</returns>
        public int IndexOf(T item)
        {
            foreach (var b in CurrentBlocks)
            {
                var index = b.IndexOf(item);
                if (index != -1) return index;
            }
            return -1;
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            foreach (var b in CurrentBlocks)
            {
                b.Unsubscribe();
            }
            CurrentBlocks = new VirtualizingCacheBlock<T>[0];
        }

        public void Dispose()
        {
            _disposables.Dispose();

            // disconnect any pending requests
            foreach (var b in CurrentBlocks)
            {
                b.Unsubscribe();
            }
            CurrentBlocks = null;

            // dispose events
            _cacheObserver.OnCompleted();
            _cacheObserver.Dispose();
        }

        /// <summary>
        /// Determines if there is any difference between the current ranges and the newly requested ranges.
        /// </summary>
        /// <param name="currentBlocks">The current blocks.</param>
        /// <param name="newRanges">The newly requested ranges.</param>
        /// <returns>True if there is any difference, otherwise false.</returns>
        private static bool Differs(VirtualizingCacheBlock<T>[] currentBlocks, IndexRange[] newRanges)
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
        private static IReadOnlyList<T> Purge(VirtualizingCacheBlock<T>[] currentBlocks, IndexRange[] newRanges)
        {
            var discardedItems = new List<T>();
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
        private static VirtualizingCacheBlock<T>[] UpdateBlocks(VirtualizingCacheBlock<T>[] oldBlocks,
            IndexRange[] newRanges,
            IVirtualizingSource<T> source)
        {
            foreach (var b in oldBlocks)
            {
                b.Unsubscribe();
            }

            var newBlocks = new VirtualizingCacheBlock<T>[newRanges.Length];
            int i = 0, j = 0;
            while (j < newRanges.Length)
            {
                var requests = new List<VirtualizingCacheBlockRequest<T>>();
                var newItems = new T[newRanges[j].Length];

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
                                requests.Add(new VirtualizingCacheBlockRequest<T>(r.FullRange, effectiveRange.Value, r.WhenItemsLoaded));
                            }
                        }
                    }

                    if (leftDiff.HasValue)
                    {
                        requests.Add(new VirtualizingCacheBlockRequest<T>(leftDiff.Value, source));
                    }
                    if (rightDiff.HasValue)
                    {
                        i += 1;
                    }
                }

                if (rightDiff.HasValue)
                {
                    requests.Add(new VirtualizingCacheBlockRequest<T>(rightDiff.Value, source));
                }

                newBlocks[j] = new VirtualizingCacheBlock<T>(newRanges[j], newItems, requests);
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
        private static (VirtualizingCacheBlock<T>[], IReadOnlyList<T>) RefreshBlocks(VirtualizingCacheBlock<T>[] oldBlocks,
            int totalCount, LogicalChange[] logicalChanges,
            IVirtualizingSource<T> source)
        {
            var removals = logicalChanges.Where(c => c.State != DeltaState.Add)
                .Select(c => new PhysicalChange(PhysicalChangeAction.Remove, c.PreviousEntity, c.PreviousIndex.Value))
                .OrderBy(c => c.Index)
                .ToArray();
            var additions = logicalChanges.Where(c => c.State != DeltaState.Remove)
                .Select(c => new PhysicalChange(PhysicalChangeAction.Add, c.CurrentEntity, c.CurrentIndex.Value))
                .OrderBy(c => c.Index)
                .ToArray();

            var newBlocks = new List<VirtualizingCacheBlock<T>>(oldBlocks.Length);
            var discardedItems = new List<T>();
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
                var newItems = new T[newRange.Length];
                var newRequests = new List<VirtualizingCacheBlockRequest<T>>();

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
                            newRequests.Add(new VirtualizingCacheBlockRequest<T>(range, source));
                            requestIndex = currentAddition.Index + 1;
                        }
                    }
                    else
                    {
                        var range = new IndexRange(requestIndex, destinationIndex - 1);
                        newRequests.Add(new VirtualizingCacheBlockRequest<T>(range, source));
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
                    newRequests.Add(new VirtualizingCacheBlockRequest<T>(range, source));
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
                            newRequests.Add(new VirtualizingCacheBlockRequest<T>(destinationEffectiveRange, source));
                        }
                    }
                }

                newBlocks.Add(new VirtualizingCacheBlock<T>(newRange, newItems, newRequests));
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
        /// <returns>The item instance if the index is within the ranges the cache is tracking, otherwise the default value of type <typeparamref name="T"/>.</returns>
        private static T SearchItem(VirtualizingCacheBlock<T>[] blocks, int index)
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
            public readonly T CurrentEntity;
            public readonly T PreviousEntity;
            public readonly int? CurrentIndex;
            public readonly int? PreviousIndex;

            private LogicalChange(DeltaState state, T currentEntity, T previousEntity, int? currentIndex, int? previousIndex)
            {
                State = state;
                CurrentEntity = currentEntity;
                PreviousEntity = previousEntity;
                CurrentIndex = currentIndex;
                PreviousIndex = previousIndex;
            }

            public static LogicalChange Addition(T currentEntity, int currentIndex)
            {
                return new LogicalChange(DeltaState.Add, currentEntity, null, currentIndex, null);
            }

            public static LogicalChange Removal(T previousEntity, int previousIndex)
            {
                return new LogicalChange(DeltaState.Remove, null, previousEntity, null, previousIndex);
            }

            public static LogicalChange Update(T currentEntity, int currentIndex, T previousEntity, int previousIndex)
            {
                return new LogicalChange(DeltaState.Update, currentEntity, previousEntity, currentIndex, previousIndex);
            }

            public static IReadOnlyList<VirtualizingCacheSourceChange<T>> Serialize(LogicalChange[] logicalChanges)
            {
                var serializedChanges = new List<VirtualizingCacheSourceChange<T>>();
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
                                serializedChanges.Add(VirtualizingCacheSourceChange<T>.Addition(
                                    currentChange.Value.CurrentEntity,
                                    currentChange.Value.CurrentIndex.Value));
                                shift += 1;
                                break;
                            case DeltaState.Remove:
                                serializedChanges.Add(VirtualizingCacheSourceChange<T>.Removal(
                                    currentChange.Value.PreviousEntity,
                                    currentChange.Value.PreviousIndex.Value + shift));
                                shift -= 1;
                                break;
                            case DeltaState.Update:
                                serializedChanges.Add(VirtualizingCacheSourceChange<T>.Update(
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
            public readonly T Entity;
            public readonly int Index;

            public PhysicalChange(PhysicalChangeAction action, T entity, int index)
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
