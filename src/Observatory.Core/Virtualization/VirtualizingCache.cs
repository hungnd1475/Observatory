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
    /// <typeparam name="TEntity">The type of items retrieved from source.</typeparam>
    public class VirtualizingCache<TEntity, TKey> : IDisposable, IEnableLogger
        where TEntity : class, IVirtualizableSource<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<IndexRange[]> _rangeSubject = new Subject<IndexRange[]>();
        private readonly Subject<IVirtualizingCacheEvent<TEntity>> _cacheSubject =
            new Subject<IVirtualizingCacheEvent<TEntity>>();
        private readonly BehaviorSubject<List<TKey>> _keySubject = new BehaviorSubject<List<TKey>>(null);
        private readonly IVirtualizingSource<TEntity, TKey> _source;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets the current blocks the cache is tracking.
        /// </summary>
        public VirtualizingCacheBlock<TEntity>[] CurrentBlocks { get; private set; } = new VirtualizingCacheBlock<TEntity>[0];

        /// <summary>
        /// Gets an observable stream of <see cref="IVirtualizingCacheEvent{T}"/> fired whenever there are changes happened in the cache.
        /// </summary>
        public IObservable<IVirtualizingCacheEvent<TEntity>> WhenCacheChanged => _cacheSubject.AsObservable();

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item instance if the index is within the ranges the cache is tracking, otherwise the default value of type <typeparamref name="TEntity"/>.</returns>
        public TEntity this[int index] => SearchItem(CurrentBlocks, index);

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> of all the indices the cache is holding.
        /// </summary>
        public IEnumerable<int> Indices => CurrentBlocks.Select(b => b.Range).SelectMany(r => r);

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCache{T}"/>.
        /// </summary>
        /// <param name="source">The source where items are retrieved from.</param>
        public VirtualizingCache(IVirtualizingSource<TEntity, TKey> source,
            IObservable<IReadOnlyList<DeltaEntity<TEntity>>> whenSourceChanged)
        {
            _source = source;

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
                    _cacheSubject.OnNext(new VirtualizingCacheRangesUpdatedEvent<TEntity>(discardedItems));
                    foreach (var b in CurrentBlocks)
                    {
                        b.Subscribe(_cacheSubject);
                    }
                    this.Log().Debug($"Tracking {CurrentBlocks.Length} block(s): [{string.Join(",", CurrentBlocks.Select(b => b.Range))}]");
                })
                .DisposeWith(_disposables);

            whenSourceChanged
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
                    _cacheSubject.OnNext(new VirtualizingCacheSourceUpdatedEvent<TEntity>(discardedItems, serializedChanges, totalCount));

                    foreach (var b in CurrentBlocks)
                    {
                        b.Subscribe(_cacheSubject);
                    }

                    this.Log().Debug($"Changes:\n{string.Join("\n", serializedChanges)}");
                })
                .DisposeWith(_disposables);

            Observable.Start(() => source.GetAllKeys(), RxApp.TaskpoolScheduler)
                .Subscribe(_keySubject.OnNext)
                .DisposeWith(_disposables);
        }

        public void Initialize()
        {
            Observable.Start(() => _source.GetTotalCount(), RxApp.TaskpoolScheduler)
                .Select(totalCount => new VirtualizingCacheInitializedEvent<TEntity>(totalCount))
                .Subscribe(_cacheSubject.OnNext);
        }

        public void Reset()
        {
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
            foreach (var b in CurrentBlocks)
            {
                b.Unsubscribe();
            }
            CurrentBlocks = new VirtualizingCacheBlock<TEntity>[0];
        }

        public void Dispose()
        {
            // dispose subscriptions
            _disposables.Dispose();

            // unsubsribe any pending requests
            foreach (var b in CurrentBlocks)
            {
                b.Unsubscribe();
            }
            CurrentBlocks = null;

            // dispose events
            _cacheSubject.OnCompleted();
            _cacheSubject.Dispose();

            // dispose keys
            _keySubject.OnCompleted();
            _keySubject.Dispose();
        }

        /// <summary>
        /// Determines if there is any difference between the current ranges and the newly requested ranges.
        /// </summary>
        /// <param name="currentBlocks">The current blocks.</param>
        /// <param name="newRanges">The newly requested ranges.</param>
        /// <returns>True if there is any difference, otherwise false.</returns>
        private static bool Differs(VirtualizingCacheBlock<TEntity>[] currentBlocks, IndexRange[] newRanges)
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
        private static IReadOnlyList<TEntity> Purge(VirtualizingCacheBlock<TEntity>[] currentBlocks, IndexRange[] newRanges)
        {
            var discardedItems = new List<TEntity>();
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
        private static VirtualizingCacheBlock<TEntity>[] UpdateBlocks(VirtualizingCacheBlock<TEntity>[] oldBlocks,
            IndexRange[] newRanges,
            IVirtualizingSource<TEntity, TKey> source)
        {
            foreach (var b in oldBlocks)
            {
                b.Unsubscribe();
            }

            var newBlocks = new VirtualizingCacheBlock<TEntity>[newRanges.Length];
            int i = 0, j = 0;
            while (j < newRanges.Length)
            {
                var requests = new List<VirtualizingCacheBlockRequest<TEntity>>();
                var newItems = new TEntity[newRanges[j].Length];

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
                        requests.Add(VirtualizingCacheBlockRequest<TEntity>.FromSource(leftDiff.Value, source));
                    }
                    if (rightDiff.HasValue)
                    {
                        i += 1;
                    }
                }

                if (rightDiff.HasValue)
                {
                    requests.Add(VirtualizingCacheBlockRequest<TEntity>.FromSource(rightDiff.Value, source));
                }

                newBlocks[j] = new VirtualizingCacheBlock<TEntity>(newRanges[j], newItems, requests);
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
        private static (VirtualizingCacheBlock<TEntity>[], IReadOnlyList<TEntity>) RefreshBlocks(VirtualizingCacheBlock<TEntity>[] oldBlocks,
            int totalCount, LogicalChange[] logicalChanges,
            IVirtualizingSource<TEntity, TKey> source)
        {
            var removals = logicalChanges.Where(c => c.State != DeltaState.Add)
                .Select(c => new PhysicalChange(PhysicalChangeAction.Remove, c.PreviousEntity, c.PreviousIndex.Value))
                .OrderBy(c => c.Index)
                .ToArray();
            var additions = logicalChanges.Where(c => c.State != DeltaState.Remove)
                .Select(c => new PhysicalChange(PhysicalChangeAction.Add, c.CurrentEntity, c.CurrentIndex.Value))
                .OrderBy(c => c.Index)
                .ToArray();

            var newBlocks = new List<VirtualizingCacheBlock<TEntity>>(oldBlocks.Length);
            var discardedItems = new List<TEntity>();
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
                var newItems = new TEntity[newRange.Length];
                var newRequests = new List<VirtualizingCacheBlockRequest<TEntity>>();

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
                            newRequests.Add(VirtualizingCacheBlockRequest<TEntity>.FromSource(range, source));
                            requestIndex = currentAddition.Index + 1;
                        }
                    }
                    else
                    {
                        var range = new IndexRange(requestIndex, destinationIndex - 1);
                        newRequests.Add(VirtualizingCacheBlockRequest<TEntity>.FromSource(range, source));
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
                    newRequests.Add(VirtualizingCacheBlockRequest<TEntity>.FromSource(range, source));
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
                            newRequests.Add(VirtualizingCacheBlockRequest<TEntity>.FromSource(destinationEffectiveRange, source));
                        }
                    }
                }

                newBlocks.Add(new VirtualizingCacheBlock<TEntity>(newRange, newItems, newRequests));
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
        /// <returns>The item instance if the index is within the ranges the cache is tracking, otherwise the default value of type <typeparamref name="TEntity"/>.</returns>
        private static TEntity SearchItem(VirtualizingCacheBlock<TEntity>[] blocks, int index)
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
            public readonly TEntity CurrentEntity;
            public readonly TEntity PreviousEntity;
            public readonly int? CurrentIndex;
            public readonly int? PreviousIndex;

            private LogicalChange(DeltaState state, TEntity currentEntity, TEntity previousEntity, int? currentIndex, int? previousIndex)
            {
                State = state;
                CurrentEntity = currentEntity;
                PreviousEntity = previousEntity;
                CurrentIndex = currentIndex;
                PreviousIndex = previousIndex;
            }

            public static LogicalChange Addition(TEntity currentEntity, int currentIndex)
            {
                return new LogicalChange(DeltaState.Add, currentEntity, null, currentIndex, null);
            }

            public static LogicalChange Removal(TEntity previousEntity, int previousIndex)
            {
                return new LogicalChange(DeltaState.Remove, null, previousEntity, null, previousIndex);
            }

            public static LogicalChange Update(TEntity currentEntity, int currentIndex, TEntity previousEntity, int previousIndex)
            {
                return new LogicalChange(DeltaState.Update, currentEntity, previousEntity, currentIndex, previousIndex);
            }

            public static IReadOnlyList<VirtualizingCacheSourceChange<TEntity>> Serialize(LogicalChange[] logicalChanges)
            {
                var serializedChanges = new List<VirtualizingCacheSourceChange<TEntity>>();
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
                                serializedChanges.Add(VirtualizingCacheSourceChange<TEntity>.Addition(
                                    currentChange.Value.CurrentEntity,
                                    currentChange.Value.CurrentIndex.Value));
                                shift += 1;
                                break;
                            case DeltaState.Remove:
                                serializedChanges.Add(VirtualizingCacheSourceChange<TEntity>.Removal(
                                    currentChange.Value.PreviousEntity,
                                    currentChange.Value.PreviousIndex.Value + shift));
                                shift -= 1;
                                break;
                            case DeltaState.Update:
                                serializedChanges.Add(VirtualizingCacheSourceChange<TEntity>.Update(
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
            public readonly TEntity Entity;
            public readonly int Index;

            public PhysicalChange(PhysicalChangeAction action, TEntity entity, int index)
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
