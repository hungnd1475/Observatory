﻿using Observatory.Core.Services;
using Observatory.Core.Virtualization;
using ReactiveUI;
using Splat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

namespace Observatory.UI.Virtualizing
{
    /// <summary>
    /// Represents a virtualizing list that implements <see cref="IItemsRangeInfo"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the items retrieved from source.</typeparam>
    /// <typeparam name="TTarget">The type of items displayed on the UI.</typeparam>
    public class VirtualizingList<TSource, TTarget> : IList, INotifyCollectionChanged, IItemsRangeInfo, IEnableLogger,
        IVirtualizingCacheEventProcessor<TSource, IEnumerable<NotifyCollectionChangedEventArgs>>
        where TSource : class
        where TTarget : class, IVirtualizingTarget<TSource>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly Func<TSource, TTarget> _targetFactory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly VirtualizingCache<TSource> _sourceCache;
        private readonly Dictionary<TSource, TTarget> _targetCache;

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingList{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="sourceCache">The cache.</param>
        /// <param name="targetFactory">The factory function transforming <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.</param>
        public VirtualizingList(VirtualizingCache<TSource> sourceCache,
            Func<TSource, TTarget> targetFactory)
        {
            _sourceCache = sourceCache;
            _targetCache = new Dictionary<TSource, TTarget>(sourceCache.ItemComparer);
            _targetFactory = targetFactory;

            _sourceCache.WhenCacheChanged
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
        }

        /// <summary>
        /// Called by the UI to update the ranges of items displayed.
        /// </summary>
        /// <param name="visibleRange"></param>
        /// <param name="trackedItems"></param>
        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            _sourceCache.UpdateRanges(trackedItems.Select(i => new IndexRange(i.FirstIndex, i.LastIndex)).ToArray());
        }

        private int IndexOf(TTarget item)
        {
            foreach (var entry in _targetCache)
            {
                if (ReferenceEquals(entry.Value, item))
                {
                    return _sourceCache.IndexOf(entry.Key, _sourceCache.ItemComparer);
                }
            }
            return -1;
        }

        public void Dispose()
        {
            foreach (var index in _targetCache.Keys)
            {
                (_targetCache[index] as IDisposable)?.Dispose();
            }
            _targetCache.Clear();
            _disposables.Dispose();
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
                _targetCache[source] = target;
                events.Add(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, target,
                    new VirtualizingPlaceholder(index), index));
            }
            return events;
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheRangesUpdatedEvent<TSource> e)
        {
            foreach (var source in e.DiscardedItems)
            {
                if (_targetCache.ContainsKey(source))
                {
                    (_targetCache[source] as IDisposable)?.Dispose();
                    _targetCache.Remove(source);
                }
            }
            return Enumerable.Empty<NotifyCollectionChangedEventArgs>();
        }
        
        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheSourceUpdatedEvent<TSource> e)
        {
            var events = new List<NotifyCollectionChangedEventArgs>(e.Changes.Count);
            foreach (var c in e.Changes)
            {
                switch (c.State)
                {
                    case DeltaState.Add:
                        _targetCache[c.CurrentItem] = _targetFactory(c.CurrentItem);
                        events.Add(new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            _targetCache[c.CurrentItem],
                            c.CurrentIndex.Value));
                        break;
                    case DeltaState.Update:
                        {
                            if (_targetCache.TryGetValue(c.PreviousItem, out var target))
                            {
                                _targetCache.Remove(c.PreviousItem);
                                _targetCache[c.CurrentItem] = target;
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
                                    _targetCache[c.CurrentItem],
                                    c.CurrentIndex.Value,
                                    c.PreviousIndex.Value));
                            }
                        }
                        break;
                    case DeltaState.Remove:
                        {
                            if (_targetCache.TryGetValue(c.PreviousItem, out var oldItem))
                            {
                                (oldItem as IDisposable)?.Dispose();
                                _targetCache.Remove(c.PreviousItem);
                            }
                            events.Add(new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Remove,
                                oldItem,
                                c.PreviousIndex.Value));
                        }
                        break;
                }
            }

            foreach (var source in e.DiscardedItems)
            {
                if (_targetCache.ContainsKey(source))
                {
                    (_targetCache[source] as IDisposable)?.Dispose();
                    _targetCache.Remove(source);
                }
            }

            Count = e.TotalCount;
            return events;
        }

        #endregion

        #region ++ IList ++

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public object this[int index]
        {
            get
            {
                var source = _sourceCache[index];
                return source != null ? _targetCache[source] : null;
            }
            set => throw new NotSupportedException();
        }

        public int Count { get; private set; }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            return IndexOf(value) != -1;
        }

        public int IndexOf(object value)
        {
            return value switch
            {
                VirtualizingPlaceholder x => x.Index,
                TTarget x => IndexOf(x),
                _ => -1,
            };
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
