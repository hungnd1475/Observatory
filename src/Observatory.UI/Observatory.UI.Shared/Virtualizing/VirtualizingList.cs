using Observatory.Core.Services;
using Observatory.Core.Virtualization;
using ReactiveUI;
using Splat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Data;

namespace Observatory.UI.Virtualizing
{
    public class VirtualizingList<TSource, TTarget> : IList, INotifyCollectionChanged, IItemsRangeInfo, IEnableLogger, 
        IVirtualizingCacheEventProcessor<TSource, IEnumerable<NotifyCollectionChangedEventArgs>>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly Func<TSource, TTarget> _targetFactory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly VirtualizingCache<TSource> _sourceCache;
        private readonly Dictionary<int, TTarget> _targetCache = new Dictionary<int, TTarget>();

        public VirtualizingList(VirtualizingCache<TSource> sourceCache,
            Func<TSource, TTarget> targetFactory)
        {
            _sourceCache = sourceCache;
            _targetFactory = targetFactory;

            foreach (var b in _sourceCache.CurrentBlocks)
            {
                foreach (var index in b.Range)
                {
                    _targetCache.Add(index, targetFactory.Invoke(b[index]));
                }
            }

            _sourceCache.WhenCacheChanged
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(e => e.Process(this))
                //.Select(sourceEvent =>
                //{
                //    var targetEvents = new List<NotifyCollectionChangedEventArgs>();
                //    switch (sourceEvent)
                //    {
                //        case VirtualizingCacheItemsLoadedEvent<TSource> e:
                //            foreach (var index in e.Range)
                //            {
                //                if (_targetCache.ContainsKey(index))
                //                {
                //                    (_targetCache[index] as IDisposable)?.Dispose();
                //                }
                //                _targetCache[index] = targetFactory.Invoke(e.Block[index]);

                //                targetEvents.Add(new NotifyCollectionChangedEventArgs(
                //                    NotifyCollectionChangedAction.Replace, e.Block[index],
                //                    new VirtualizingPlaceholder(index), index));
                //            }
                //            break;
                //        case VirtualizingCacheInitializedEvent<TSource> _:
                //            targetEvents.Add(new NotifyCollectionChangedEventArgs(
                //                NotifyCollectionChangedAction.Reset));
                //            break;
                //        case VirtualizingCacheRangesUpdatedEvent<TSource> e:
                //            foreach (var index in e.RemovedRanges.SelectMany(r => r))
                //            {
                //                if (_targetCache.ContainsKey(index))
                //                {
                //                    (_targetCache[index] as IDisposable)?.Dispose();
                //                    _targetCache.Remove(index);
                //                }
                //            }
                //            break;
                //        case VirtualizingCacheSourceUpdatedEvent<TSource> e:
                //            foreach (var c in e.Changes)
                //            {
                //                switch (c.Change.State)
                //                {
                //                    case DeltaState.Add:
                //                        targetEvents.Add(new NotifyCollectionChangedEventArgs(
                //                            NotifyCollectionChangedAction.Add, (object)null, c.Index));
                //                        break;
                //                    case DeltaState.Update:
                //                        if (c.PreviousIndex != c.Index)
                //                        {
                //                            targetEvents.Add(new NotifyCollectionChangedEventArgs(
                //                                NotifyCollectionChangedAction.Move, 
                //                                new VirtualizingPlaceholder(c.Index),
                //                                c.Index,
                //                                c.PreviousIndex.Value));
                //                        }
                //                        else
                //                        {
                //                            targetEvents.Add(new NotifyCollectionChangedEventArgs(
                //                                NotifyCollectionChangedAction.Replace,
                //                                null,
                //                                new VirtualizingPlaceholder(c.Index),
                //                                c.Index));
                //                        }
                //                        break;
                //                    case DeltaState.Remove:
                //                        targetEvents.Add(new NotifyCollectionChangedEventArgs(
                //                            NotifyCollectionChangedAction.Remove,
                //                            new VirtualizingPlaceholder(c.Index), c.Index));
                //                        break;
                //                }
                //            }
                //            break;
                //    }
                //    return targetEvents;
                //})
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(targetEvents =>
                {
                    foreach (var e in targetEvents)
                    {
                        CollectionChanged?.Invoke(this, e);
                    }
                })
                .DisposeWith(_disposables);
            _sourceCache.WhenCountChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(count => Count = count)
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
                    return entry.Key;
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
            yield return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheItemsLoadedEvent<TSource> e)
        {
            foreach (var index in e.Range)
            {
                if (_targetCache.ContainsKey(index))
                {
                    (_targetCache[index] as IDisposable)?.Dispose();
                }
                _targetCache[index] = _targetFactory.Invoke(e.Block[index]);
            }

            return e.Range.Select(index => new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Replace, e.Block[index],
                new VirtualizingPlaceholder(index), index)).ToList();
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheRangesUpdatedEvent<TSource> e)
        {
            foreach (var index in e.RemovedRanges.SelectMany(r => r))
            {
                if (_targetCache.ContainsKey(index))
                {
                    (_targetCache[index] as IDisposable)?.Dispose();
                    _targetCache.Remove(index);
                }
            }
            return Enumerable.Empty<NotifyCollectionChangedEventArgs>();
        }

        public IEnumerable<NotifyCollectionChangedEventArgs> Process(VirtualizingCacheSourceUpdatedEvent<TSource> e)
        {
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
            get => _targetCache.ContainsKey(index) ? _targetCache[index] : default;
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
