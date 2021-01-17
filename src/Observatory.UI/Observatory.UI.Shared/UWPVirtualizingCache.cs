using Observatory.Core.Services;
using Observatory.Core.Virtualization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Data;

namespace Observatory.UI
{
    public class UWPVirtualizingCache<TSource, TTarget, TKey> : IList, INotifyCollectionChanged, IItemsRangeInfo, ISelectionInfo
        where TSource : class, IVirtualizableSource<TKey>
        where TTarget : class, IVirtualizableTarget<TSource, TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly VirtualizingCache<TSource, TTarget, TKey> _cache;

        public UWPVirtualizingCache(VirtualizingCache<TSource, TTarget, TKey> cache)
        {
            _cache = cache;
        }

        public object this[int index] 
        { 
            get => ((IList)_cache)[index]; 
            set => throw new NotSupportedException(); 
        }

        public bool IsFixedSize => _cache.IsFixedSize;

        public bool IsReadOnly => _cache.IsReadOnly;

        public int Count => _cache.Count;

        public bool IsSynchronized => _cache.IsSynchronized;

        public object SyncRoot => _cache.SyncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _cache.CollectionChanged += value;
            remove => _cache.CollectionChanged -= value;
        }

        public int Add(object value) => _cache.Add(value);

        public void Clear() => _cache.Clear();

        public bool Contains(object value) => _cache.Contains(value);

        public void CopyTo(Array array, int index) => _cache.CopyTo(array, index);

        public void Dispose() => _cache.Dispose();

        public IEnumerator GetEnumerator() => _cache.GetEnumerator();

        public int IndexOf(object value) => _cache.IndexOf(value);

        public void Insert(int index, object value) => _cache.Insert(index, value);

        public void Remove(object value) => _cache.Remove(value);

        public void RemoveAt(int index) => _cache.RemoveAt(index);

        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            _cache.UpdateRanges(trackedItems.Select(i => new IndexRange(i.FirstIndex, i.LastIndex)).ToArray());
        }

        public void SelectRange(ItemIndexRange itemIndexRange)
        {
            _cache.SelectRange(new IndexRange(itemIndexRange.FirstIndex, itemIndexRange.LastIndex));
        }

        public void DeselectRange(ItemIndexRange itemIndexRange)
        {
            _cache.DeselectRange(new IndexRange(itemIndexRange.FirstIndex, itemIndexRange.LastIndex));
        }

        public bool IsSelected(int index)
        {
            return _cache.IsSelected(index);
        }

        public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
        {
            return _cache.GetSelectedRanges()
                .Select(r => new ItemIndexRange(r.FirstIndex, (uint)r.Length))
                .ToArray();
        }
    }

    public static class VirtualizingCacheExtensions
    {
        public static UWPVirtualizingCache<TSource, TTarget, TKey> ToNative<TSource, TTarget, TKey>(this VirtualizingCache<TSource, TTarget, TKey> cache)
            where TSource : class, IVirtualizableSource<TKey>
            where TTarget : class, IVirtualizableTarget<TSource, TKey>
            where TKey : IEquatable<TKey>
        {
            return new UWPVirtualizingCache<TSource, TTarget, TKey>(cache);
        }
    }
}
