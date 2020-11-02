﻿using Observatory.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
    public class VirtualizingCache<TSource, TTarget> : IDisposable
    {
        private readonly IVirtualizingSource<TSource> _source;
        private readonly Func<TSource, TTarget> _targetFactory;
        private readonly IComparer<TTarget> _targetComparer;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<IndexRange[]> _rangesObserver = new Subject<IndexRange[]>();
        private readonly ScheduledSubject<VirtualizingCacheBlockLoadedEvent<TSource, TTarget>> _cacheObserver = 
            new ScheduledSubject<VirtualizingCacheBlockLoadedEvent<TSource, TTarget>>(RxApp.MainThreadScheduler);
        private readonly BehaviorSubject<int> _countObserver = new BehaviorSubject<int>(0);

        /// <summary>
        /// Stores the current blocks the cache is keeping track of.
        /// </summary>
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
            IObservable<IEnumerable<DeltaEntity<TSource>>> sourceObserver,
            Func<TSource, TTarget> targetFactory,
            IComparer<TTarget> targetComparer)
        {
            _source = source;
            _targetFactory = targetFactory;
            _targetComparer = targetComparer;

            _rangesObserver
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Throttle(TimeSpan.FromMilliseconds(20))
                .Select(Normalize)
                .Where(Differs)
                .Select(UpdateBlocks)
                .Subscribe(newBlocks =>
                {
                    foreach (var b in _currentBlocks)
                    {
                        b.Dispose();
                    }
                    _currentBlocks = newBlocks;
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
            _rangesObserver.OnNext(newRanges);
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
            foreach (var b in _currentBlocks)
            {
                b.Dispose();
            }
        }

        /// <summary>
        /// Determines if there is any difference between the ranges the cache is tracking and the given <paramref name="ranges"/>.
        /// </summary>
        /// <param name="ranges">The other set of ranges.</param>
        /// <returns>True if there is any difference, otherwise false.</returns>
        private bool Differs(IndexRange[] ranges)
        {
            var currentBlocks = _currentBlocks;
            int i = 0, j = 0;
            while (i < currentBlocks.Length && j < ranges.Length)
            {
                if (currentBlocks[i].Range.Covers(ranges[j]))
                {
                    j += 1;
                }
                else 
                {
                    var (leftDiff, rightDiff) = currentBlocks[i].Range.Diff(ranges[j]);
                    if (leftDiff.HasValue)
                    {
                        return true;
                    }
                    else
                    {
                        if (rightDiff.HasValue && rightDiff.Value != ranges[j])
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

            return j < ranges.Length;
        }

        /// <summary>
        /// Updates the current array of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>, figuring out which items 
        /// stay which items need to be retrieved from source.
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
                var requests = new Queue<VirtualizingCacheBlockRequest<TSource, TTarget>>();
                var newItems = new TTarget[newRanges[j].Length];

                IndexRange? leftDiff = null, rightDiff = newRanges[j];
                while (i < oldBlocks.Length && rightDiff.HasValue)
                {
                    var oldRange = oldBlocks[i].Range;
                    var intersect = oldRange.Intersect(rightDiff.Value);
                    (leftDiff, rightDiff) = oldRange.Diff(rightDiff.Value);

                    if (intersect.HasValue)
                    {
                        var oldDestination = intersect.Value.FirstIndex - oldRange.FirstIndex;
                        var newDestination = intersect.Value.FirstIndex - newRanges[j].FirstIndex;
                        var length = intersect.Value.Length;
                        Array.Copy(oldBlocks[i].Items, oldDestination, newItems, newDestination, length);

                        while (oldBlocks[i].Requests.Count > 0)
                        {
                            var r = oldBlocks[i].Requests.Dequeue();
                            var effectiveRange = r.FullRange.Intersect(intersect.Value);
                            if (!r.IsReceived && effectiveRange.HasValue)
                            {
                                r.EffectiveRange = effectiveRange.Value;
                                requests.Enqueue(r);
                            }
                            else
                            {
                                r.Dispose();
                            }
                        }
                    }

                    if (leftDiff.HasValue)
                    {
                        requests.Enqueue(new VirtualizingCacheBlockRequest<TSource, TTarget>(leftDiff.Value, _source, _targetFactory));
                    }
                    if (rightDiff.HasValue)
                    {
                        i += 1;
                    }
                }

                if (rightDiff.HasValue)
                {
                    requests.Enqueue(new VirtualizingCacheBlockRequest<TSource, TTarget>(rightDiff.Value, _source, _targetFactory));
                }

                newBlocks[j] = new VirtualizingCacheBlock<TSource, TTarget>(
                    newRanges[j], newItems, requests, _cacheObserver);
                j += 1;
            }

            foreach (var b in oldBlocks)
            {
                b.Dispose();
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
