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
    public class UWPVirtualizingCache<TSource, TTarget, TKey> : IList, INotifyCollectionChanged, IItemsRangeInfo
        where TSource : class, IVirtualizableSource<TKey>
        where TTarget : class, IVirtualizableTarget<TSource, TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly VirtualizingCache<TSource, TTarget, TKey> _cache;

        public UWPVirtualizingCache(VirtualizingCache<TSource, TTarget, TKey> cache)
        {
            _cache = cache;
        }

        public object this[int index] { get => ((IList)_cache)[index]; set => ((IList)_cache)[index] = value; }

        public bool IsFixedSize => ((IList)_cache).IsFixedSize;

        public bool IsReadOnly => ((IList)_cache).IsReadOnly;

        public int Count => ((ICollection)_cache).Count;

        public bool IsSynchronized => ((ICollection)_cache).IsSynchronized;

        public object SyncRoot => ((ICollection)_cache).SyncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                ((INotifyCollectionChanged)_cache).CollectionChanged += value;
            }

            remove
            {
                ((INotifyCollectionChanged)_cache).CollectionChanged -= value;
            }
        }

        public int Add(object value)
        {
            return ((IList)_cache).Add(value);
        }

        public void Clear()
        {
            ((IList)_cache).Clear();
        }

        public bool Contains(object value)
        {
            return ((IList)_cache).Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_cache).CopyTo(array, index);
        }

        public void Dispose()
        {
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_cache).GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return ((IList)_cache).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            ((IList)_cache).Insert(index, value);
        }

        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            _cache.UpdateRanges(trackedItems.Select(i => new IndexRange(i.FirstIndex, i.LastIndex)).ToArray());
        }

        public void Remove(object value)
        {
            ((IList)_cache).Remove(value);
        }

        public void RemoveAt(int index)
        {
            ((IList)_cache).RemoveAt(index);
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
