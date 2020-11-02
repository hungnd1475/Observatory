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
    public class VirtualizingList<TSource, TTarget> : INotifyPropertyChanged, IList, INotifyCollectionChanged, IItemsRangeInfo
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly VirtualizingCache<TSource, TTarget> _cache;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private int _count;

        public VirtualizingList(VirtualizingCache<TSource, TTarget> cache)
        {
            _cache = cache;
            _cache.CacheChanged
                .Subscribe(x =>
                {
                    foreach (var index in x.Range)
                    {
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace, x.Block[index],
                            new VirtualizingPlaceholder(index), index));
                    }
                })
                .DisposeWith(_disposables);
            _cache.CountChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(count => Count = count)
                .DisposeWith(_disposables);
        }

        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            _cache.UpdateRanges(trackedItems.Select(i => new IndexRange(i.FirstIndex, i.LastIndex)).ToArray());
        }

        #region IList Implementation

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public object this[int index]
        {
            get => _cache[index];
            set => throw new NotSupportedException();
        }

        public int Count
        {
            get => _count;
            private set
            {
                _count = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _cache.Dispose();
            _disposables.Dispose();
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
                TTarget x => _cache.IndexOf(x),
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
