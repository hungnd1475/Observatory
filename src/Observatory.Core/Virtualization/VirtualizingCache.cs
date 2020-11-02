using Observatory.Core.Services;
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
    public class VirtualizingCache<TSource, TTarget> : IDisposable, IEnableLogger
    {
        private readonly IVirtualizingSource<TSource> _source;
        private readonly Func<TSource, TTarget> _targetFactory;
        private readonly IComparer<TTarget> _targetComparer;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Subject<IndexRange[]> _rangesObserver = new Subject<IndexRange[]>();
        private readonly ScheduledSubject<IVirtualizingCacheChangedEvent> _cacheObserver = 
            new ScheduledSubject<IVirtualizingCacheChangedEvent>(RxApp.MainThreadScheduler);
        private readonly BehaviorSubject<int> _countObserver = new BehaviorSubject<int>(0);
        private readonly BehaviorSubject<VirtualizingCacheBlock<TSource, TTarget>[]> _blocksObserver =
            new BehaviorSubject<VirtualizingCacheBlock<TSource, TTarget>[]>(new VirtualizingCacheBlock<TSource, TTarget>[0]);

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public TTarget this[int index] => SearchItem(_blocksObserver.Value, index);

        /// <summary>
        /// Gets an observable stream of changes happen in the cache.
        /// </summary>
        public IObservable<IVirtualizingCacheChangedEvent> CacheChanged => _cacheObserver.AsObservable();

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
                .WithLatestFrom(_blocksObserver, (newRanges, currentBlocks) => (NewRanges: newRanges, CurrentBlocks: currentBlocks))
                .Where(x => Differs(x.NewRanges, x.CurrentBlocks))
                .Select(x => (OldBlocks: x.CurrentBlocks, NewBlocks: UpdateBlocks(x.NewRanges, x.CurrentBlocks, source, targetFactory, _cacheObserver)))
                .Subscribe(x => _blocksObserver.OnNext(x.NewBlocks))
                .DisposeWith(_disposables);

            Observable.Start(() => _source.GetTotalCount(), RxApp.TaskpoolScheduler)
                .Subscribe(count =>
                {
                    _countObserver.OnNext(count);
                    _cacheObserver.OnNext(new VirtualizingCacheResetEvent());
                });
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
            foreach (var b in _blocksObserver.Value)
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
            foreach (var b in _blocksObserver.Value)
            {
                b.Dispose();
            }
            _blocksObserver.OnNext(new VirtualizingCacheBlock<TSource, TTarget>[0]);
        }

        public override string ToString()
        {
            return string.Join(", ", _blocksObserver.Value.Select(b => b.Range));
        }

        public void Dispose()
        {
            _disposables.Dispose();
            foreach (var b in _blocksObserver.Value)
            {
                b.Dispose();
            }
            _blocksObserver.OnCompleted();
            _blocksObserver.Dispose();
        }

        /// <summary>
        /// Add or update items when source changed.
        /// </summary>
        /// <param name="changes">The changes notified by source.</param>
        private void AddOrUpdate(IEnumerable<DeltaEntity<TSource>> changes)
        {
            var addedItems = changes.Where(d => d.State == DeltaState.Add)
                .Select(d => _targetFactory(d.Entity))
                .OrderBy(i => i, _targetComparer)
                .ToArray();
        }

        /// <summary>
        /// Determines if there is any difference between the ranges the cache is tracking and the given <paramref name="newRanges"/>.
        /// </summary>
        /// <param name="newRanges">The other set of ranges.</param>
        /// <returns>True if there is any difference, otherwise false.</returns>
        private static bool Differs(IndexRange[] newRanges, VirtualizingCacheBlock<TSource, TTarget>[] currentBlocks)
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
                    var (leftDiff, rightDiff) = currentBlocks[i].Range.Diff(newRanges[j]);
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
        /// Updates the current array of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>, figuring out which items 
        /// stay which items need to be retrieved from source.
        /// </summary>
        /// <param name="newRanges">The new ranges that the new blocks need to keep track of.</param>
        /// <returns>A new array of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>.</returns>
        private static VirtualizingCacheBlock<TSource, TTarget>[] UpdateBlocks(IndexRange[] newRanges, 
            VirtualizingCacheBlock<TSource, TTarget>[] oldBlocks,
            IVirtualizingSource<TSource> source,
            Func<TSource, TTarget> targetFactory,
            IObserver<IVirtualizingCacheChangedEvent> cacheObserver)
        {
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
                        requests.Enqueue(new VirtualizingCacheBlockRequest<TSource, TTarget>(leftDiff.Value, source, targetFactory));
                    }
                    if (rightDiff.HasValue)
                    {
                        i += 1;
                    }
                }

                if (rightDiff.HasValue)
                {
                    requests.Enqueue(new VirtualizingCacheBlockRequest<TSource, TTarget>(rightDiff.Value, source, targetFactory));
                }

                newBlocks[j] = new VirtualizingCacheBlock<TSource, TTarget>(
                    newRanges[j], newItems, requests, cacheObserver);
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
