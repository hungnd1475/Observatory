using Observatory.Core.Persistence.Specifications;
using Observatory.Core.Services;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
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
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<IndexRange[]> _rangesObserver = new Subject<IndexRange[]>();
        private readonly Subject<IVirtualizingCacheEvent<T>> _cacheObserver = new Subject<IVirtualizingCacheEvent<T>>();
        private readonly BehaviorSubject<int> _countObserver = new BehaviorSubject<int>(0);
        private readonly IScheduler _scheduler = new NewThreadScheduler();

        /// <summary>
        /// Gets the current blocks the cache is tracking.
        /// </summary>
        public VirtualizingCacheBlock<T>[] CurrentBlocks { get; private set; } = new VirtualizingCacheBlock<T>[0];

        /// <summary>
        /// Gets an observable stream of <see cref="IVirtualizingCacheEvent{T}"/> fired whenever there are changes happened in the cache.
        /// </summary>
        public IObservable<IVirtualizingCacheEvent<T>> WhenCacheChanged => _cacheObserver.AsObservable();

        /// <summary>
        /// Gets an observable stream of event fired whenever the total number of items has changed.
        /// </summary>
        public IObservable<int> WhenCountChanged => _countObserver.AsObservable();

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCache{T}"/>.
        /// </summary>
        /// <param name="source">The source where items are retrieved from.</param>
        public VirtualizingCache(IVirtualizingSource<T> source,
            IObservable<IEnumerable<DeltaEntity<T>>> sourceObserver,
            IEqualityComparer<T> itemComparer)
        {
            _rangesObserver
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .Select(Normalize)
                .ObserveOn(_scheduler)
                .Subscribe(newRanges =>
                {
                    if (Differs(CurrentBlocks, newRanges))
                    {
                        var removedRanges = Purge(CurrentBlocks, newRanges);
                        CurrentBlocks = UpdateBlocks(CurrentBlocks, newRanges, source);

                        _cacheObserver.OnNext(new VirtualizingCacheRangesUpdatedEvent<T>(removedRanges));
                        foreach (var b in CurrentBlocks)
                        {
                            b.Subscribe(_cacheObserver);
                        }
                    }
                })
                .DisposeWith(_disposables);

            sourceObserver
                .ObserveOn(_scheduler)
                .Select(changes => changes.Select(c => c.State switch
                {
                    DeltaState.Add => new VirtualizingCacheSourceChange<T>(c, source.IndexOf(c.Entity), -1),
                    DeltaState.Update => new VirtualizingCacheSourceChange<T>(c, source.IndexOf(c.Entity), IndexOf(c.Entity, itemComparer)),
                    DeltaState.Remove => new VirtualizingCacheSourceChange<T>(c, IndexOf(c.Entity, itemComparer), -1),
                    _ => throw new NotSupportedException($"{c.State} is not supported."),
                }).ToList().AsEnumerable())
                .Subscribe(changes =>
                {
                    var totalCount = source.GetTotalCount();
                    CurrentBlocks = RefreshBlocks(CurrentBlocks, totalCount, source);

                    _countObserver.OnNext(totalCount);
                    _cacheObserver.OnNext(new VirtualizingCacheSourceUpdatedEvent<T>(changes));

                    foreach (var b in CurrentBlocks)
                    {
                        b.Subscribe(_cacheObserver);
                    }
                })
                .DisposeWith(_disposables);

            Observable.Start(() => source.GetTotalCount(), RxApp.TaskpoolScheduler)
                .Subscribe(totalCount =>
                {
                    _countObserver.OnNext(totalCount);
                    _cacheObserver.OnNext(new VirtualizingCacheInitializedEvent<T>());
                });
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
        /// Returns the index of a given item.
        /// </summary>
        /// <param name="item">The item to get the index.</param>
        /// <param name="itemComparer">The equality comparer to find item in the cache.</param>
        /// <returns></returns>
        public int IndexOf(T item, IEqualityComparer<T> itemComparer)
        {
            foreach (var b in CurrentBlocks)
            {
                var index = Array.FindIndex(b.Items, x => itemComparer.Equals(x, item));
                if (index != -1) return index + b.Range.FirstIndex;
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

            // dispose count
            _countObserver.OnCompleted();
            _countObserver.Dispose();
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
        private static IndexRange[] Purge(VirtualizingCacheBlock<T>[] currentBlocks, IndexRange[] newRanges)
        {
            var removedRanges = new List<IndexRange>();
            int i = 0, j = 0;
            while (i < currentBlocks.Length)
            {
                IndexRange? leftDiff, rightDiff = currentBlocks[i].Range;
                while (j < newRanges.Length && rightDiff.HasValue)
                {
                    (leftDiff, rightDiff) = newRanges[j].Difference(rightDiff.Value);
                    if (leftDiff.HasValue)
                    {
                        removedRanges.Add(leftDiff.Value);
                    }
                    if (rightDiff.HasValue)
                    {
                        j += 1;
                    }
                }
                if (rightDiff.HasValue)
                {
                    removedRanges.Add(rightDiff.Value);
                }
                i += 1;
            }
            return removedRanges.ToArray();
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
                        var sourceIndex = intersect.Value.FirstIndex - oldRange.FirstIndex;
                        var destinationIndex = intersect.Value.FirstIndex - newRanges[j].FirstIndex;
                        var length = intersect.Value.Length;
                        oldBlocks[i].Items.AsSpan(sourceIndex, length).CopyTo(newItems.AsSpan(destinationIndex, length));

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
        /// <param name="totalCount">The new total count of items in the source.</param>
        /// <param name="oldBlocks">The current blocks to be refreshed.</param>
        /// <param name="source">The source where items are retrieved from.</param>
        /// <returns></returns>
        private static VirtualizingCacheBlock<T>[] RefreshBlocks(VirtualizingCacheBlock<T>[] oldBlocks,
            int totalCount,
            IVirtualizingSource<T> source)
        {
            var newBlocks = new List<VirtualizingCacheBlock<T>>(oldBlocks.Length);
            foreach (var b in oldBlocks)
            {
                b.Unsubscribe();
                if (b.Range.FirstIndex >= totalCount)
                {
                    continue;
                }

                var range = new IndexRange(b.Range.FirstIndex, Math.Min(b.Range.LastIndex, totalCount - 1));
                var requests = new VirtualizingCacheBlockRequest<T>[1]
                {
                    new VirtualizingCacheBlockRequest<T>(range, source),
                };

                newBlocks.Add(new VirtualizingCacheBlock<T>(range,
                    b.Items.AsSpan(0, range.Length).ToArray(),
                    requests));
            }
            return newBlocks.ToArray();
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
    }
}
