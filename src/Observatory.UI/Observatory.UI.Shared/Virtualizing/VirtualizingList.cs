using Observatory.Core.Services;
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
using Windows.UI.Xaml.Data;

namespace Observatory.UI.Virtualizing
{
    public class VirtualizingList<TSource, TTarget> : IList, INotifyCollectionChanged, IItemsRangeInfo, IEnableLogger,
        IVirtualizingCacheEventProcessor<TSource, IEnumerable<NotifyCollectionChangedEventArgs>>
        where TSource : class
        where TTarget : class
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly Func<TSource, TTarget> _targetFactory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly VirtualizingCache<TSource> _sourceCache;
        private readonly Dictionary<TSource, TTarget> _targetCache = new Dictionary<TSource, TTarget>();

        public VirtualizingList(VirtualizingCache<TSource> sourceCache,
            Func<TSource, TTarget> targetFactory)
        {
            _sourceCache = sourceCache;
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
                    return _sourceCache.IndexOf(entry.Key);
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
            var count = 0;
            foreach (var index in e.Range)
            {
                count += 1;
                var source = e.Block[index];
                var target = _targetFactory(source);
                _targetCache[source] = target;
                events.Add(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, target,
                    new VirtualizingPlaceholder(index), index));
            }
            this.Log().Debug($"Loaded {count} items.");
            return events;
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheRangesUpdatedEvent<TSource> e)
        {
            var count = 0;
            foreach (var source in e.Removals.SelectMany(r => r.Items))
            {
                if (_targetCache.ContainsKey(source))
                {
                    count += 1;
                    (_targetCache[source] as IDisposable)?.Dispose();
                    _targetCache.Remove(source);
                }
            }
            this.Log().Debug($"Disposed {count} items.");
            return Enumerable.Empty<NotifyCollectionChangedEventArgs>();
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheSourceUpdatedEvent<TSource> e)
        {
            Count = e.TotalCount;
            return e.Changes.Select(c => c.Change.State switch
            {
                DeltaState.Add => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, (object)null, c.Index),
                DeltaState.Update => c.PreviousIndex.Value != c.Index
                    ? new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Move,
                        new VirtualizingPlaceholder(c.Index),
                        c.Index,
                        c.PreviousIndex.Value)
                    : new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        null,
                        new VirtualizingPlaceholder(c.Index),
                        c.Index),
                DeltaState.Remove => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    new VirtualizingPlaceholder(c.Index), c.Index),
                _ => throw new NotSupportedException(),
            })
            .ToList();
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
