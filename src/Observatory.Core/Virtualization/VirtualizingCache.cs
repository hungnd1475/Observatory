using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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
    /// <typeparam name="TSource">The type of items retrieved from source.</typeparam>
    /// <typeparam name="TTarget">The type of items the cache holds.</typeparam>
    public class VirtualizingCache<TSource, TTarget> : IDisposable, IEnableLogger
    {
        private readonly IVirtualizingSource<TSource> _source;
        private readonly Func<TSource, TTarget> _targetFactory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<IndexRange[]> _rangesChangedSubject = new Subject<IndexRange[]>();
        private readonly ScheduledSubject<VirtualizingCacheBlockLoadedEvent<TSource, TTarget>> _cacheObserver = 
            new ScheduledSubject<VirtualizingCacheBlockLoadedEvent<TSource, TTarget>>(RxApp.MainThreadScheduler);
        private readonly BehaviorSubject<int> _countObserver = new BehaviorSubject<int>(0);
        private VirtualizingCacheBlock<TSource, TTarget>[] _currentBlocks = new VirtualizingCacheBlock<TSource, TTarget>[0];

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public TTarget this[int index] => SearchItem(_currentBlocks, index);

        /// <summary>
        /// Gets an observable stream of changes happen in the cache.
        /// </summary>
        public IObservable<VirtualizingCacheBlockLoadedEvent<TSource, TTarget>> CacheChanged => _cacheObserver.AsObservable();

        /// <summary>
        /// Gets an observable stream of total items count in this cache.
        /// </summary>
        public IObservable<int> CountChanged => _countObserver.AsObservable();

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCache{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="source">The source where items are retrieved from.</param>
        /// <param name="targetFactory">The factory function transforming <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.</param>
        public VirtualizingCache(IVirtualizingSource<TSource> source,
            Func<TSource, TTarget> targetFactory)
        {
            _source = source;
            _targetFactory = targetFactory;

            _rangesChangedSubject
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .Select(Normalize)
                .Select(UpdateBlocks)
                .Subscribe(newBlocks =>
                {
                    foreach (var b in _currentBlocks)
                    {
                        b.Dispose();
                    }
                    _currentBlocks = newBlocks;
                    this.Log().Debug($"Tracking {_currentBlocks.Length} block(s): {string.Join(";", _currentBlocks.AsEnumerable())}");
                })
                .DisposeWith(_disposables);

            Observable.Start(() => _source.GetTotalCount(), RxApp.TaskpoolScheduler)
                .Subscribe(count => _countObserver.OnNext(count));
        }

        /// <summary>
        /// Update the ranges of items the cache holds. Based on a given collection of <see cref="IndexRange"/>, the cache
        /// will figure out which items to load from source and which items are longer needed. Call this function whenever the UI
        /// needs to display a new set of items.
        /// </summary>
        /// <param name="newRanges">The ranges of items that need to be displayed.</param>
        public void UpdateRanges(IndexRange[] newRanges)
        {
            _rangesChangedSubject.OnNext(newRanges);
        }

        /// <summary>
        /// Notifies the cache about the changes happened to the source.
        /// </summary>
        /// <param name="changes">The changes happen to the source.</param>
        public void OnSourceChanged(DeltaEntity<TSource>[] changes)
        {
            Observable.Start(() => _source.GetTotalCount(), RxApp.TaskpoolScheduler)
                .Subscribe(count => _countObserver.OnNext(count));
        }

        /// <summary>
        /// Returns the index of a given item.
        /// </summary>
        /// <param name="item">The item to get the index.</param>
        /// <returns></returns>
        public int IndexOf(TTarget item)
        {
            foreach (var b in _currentBlocks)
            {
                var index = Array.IndexOf(b.Items, item);
                if (index != -1) return index + b.Range.FirstIndex;
            }
            return -1;
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            foreach (var b in _currentBlocks)
            {
                b.Dispose();
            }
            _currentBlocks = new VirtualizingCacheBlock<TSource, TTarget>[0];
        }

        public override string ToString()
        {
            return string.Join(", ", _currentBlocks.Select(b => b.Range));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        /// <summary>
        /// Updates the current array of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>, figuring out which items are removed 
        /// and which items need to be retrieved from source.
        /// </summary>
        /// <param name="newRanges">The new ranges that the new blocks need to keep track of.</param>
        /// <returns>A new array of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>.</returns>
        private VirtualizingCacheBlock<TSource, TTarget>[] UpdateBlocks(IndexRange[] newRanges)
        {
            var oldBlocks = _currentBlocks;
            var newBlocks = new VirtualizingCacheBlock<TSource, TTarget>[newRanges.Length];
            int i = 0, j = 0;
            while (j < newRanges.Length)
            {
                var requests = new List<VirtualizingCacheBlockRequest<TSource, TTarget>>();
                var newItems = new TTarget[newRanges[j].Length];

                // keeps track of right-most difference between the old and new ranges,
                // as long as there is any, keep looking at the next old block
                IndexRange? newRange = newRanges[j];
                while (i < oldBlocks.Length && newRange.HasValue)
                {
                    var oldRange = oldBlocks[i].Range;
                    var intersect = oldRange.Intersect(newRange.Value);
                    var (leftDiff, rightDiff) = oldRange.Diff(newRange.Value);

                    if (intersect.HasValue)
                    {
                        var oldDestination = intersect.Value.FirstIndex - oldRange.FirstIndex;
                        var newDestination = intersect.Value.FirstIndex - newRanges[j].FirstIndex;
                        var length = intersect.Value.Length;
                        Array.Copy(oldBlocks[i].Items, oldDestination, newItems, newDestination, length);

                        foreach (var r in oldBlocks[i].Requests)
                        {
                            if (r.IsReceived)
                            {
                                r.ItemsConnection.Dispose();
                                continue;
                            }

                            var effectiveRange = r.FullRange.Intersect(intersect.Value);
                            if (effectiveRange.HasValue)
                            {
                                requests.Add(new VirtualizingCacheBlockRequest<TSource, TTarget>(
                                    r.FullRange, effectiveRange.Value, r.Items, r.ItemsConnection));
                            }
                            else
                            {
                                r.ItemsConnection.Dispose();
                            }
                        }
                    }

                    if (leftDiff.HasValue)
                    {
                        requests.Add(new VirtualizingCacheBlockRequest<TSource, TTarget>(leftDiff.Value, _source, _targetFactory));
                    }
                    if (rightDiff.HasValue)
                    {
                        i += 1;
                    }
                    newRange = rightDiff;
                }

                if (newRange.HasValue)
                {
                    requests.Add(new VirtualizingCacheBlockRequest<TSource, TTarget>(newRange.Value, _source, _targetFactory));
                }

                newBlocks[j] = new VirtualizingCacheBlock<TSource, TTarget>(
                    newRanges[j], newItems, requests.ToArray(), _cacheObserver);
                j += 1;
            }
            return newBlocks;
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
        /// Performs binary search on an array of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/> to get the item at a given index.
        /// </summary>
        /// <param name="blocks">The blocks to search.</param>
        /// <param name="index">The index of the item to search for.</param>
        /// <returns></returns>
        private static TTarget SearchItem(VirtualizingCacheBlock<TSource, TTarget>[] blocks, int index)
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
    }
}
