using Observatory.Core.Services;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a block of items for an <see cref="IndexRange"/>.
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    public class VirtualizingCacheBlock<T>
    {
        private readonly CompositeDisposable _requestSubscriptions = new CompositeDisposable();
        private readonly T[] _items;

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] => _items[index - Range.FirstIndex];

        /// <summary>
        /// Gets the range of the block.
        /// </summary>
        public IndexRange Range { get; }

        /// <summary>
        /// Gets the requests of new items.
        /// </summary>
        public IEnumerable<VirtualizingCacheBlockRequest<T>> Requests { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheBlock{T}"/>.
        /// </summary>
        /// <param name="range">The range of the block.</param>
        /// <param name="items">The items the block holds.</param>
        public VirtualizingCacheBlock(IndexRange range, T[] items)
        {
            _items = items;
            Range = range;
            Requests = Enumerable.Empty<VirtualizingCacheBlockRequest<T>>();
        }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheBlock{T}"/> with pending requests.
        /// </summary>
        /// <param name="range">The range of the block.</param>
        /// <param name="items">The items the block holds.</param>
        /// <param name="requests">The pending requests for new items.</param>
        public VirtualizingCacheBlock(IndexRange range, T[] items,
            IEnumerable<VirtualizingCacheBlockRequest<T>> requests)
        {
            _items = items;
            Range = range;
            Requests = requests;
        }

        /// <summary>
        /// Determines whether a given index is within the block.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool ContainsIndex(int index) => Range.Contains(index);

        /// <summary>
        /// Starts listening for items in the requests this block is holding and notifies a given observer whenever items are received.
        /// </summary>
        /// <param name="observer">The observer.</param>
        public void Subscribe(IObserver<IVirtualizingCacheEvent<T>> observer)
        {
            foreach (var request in Requests)
            {
                request.WhenItemsLoaded
                    .Subscribe(items =>
                    {
                        request.FullRange.Slice(items, request.EffectiveRange).CopyTo(Slice(request.EffectiveRange));
                        request.IsReceived = true;
                        observer.OnNext(new VirtualizingCacheItemsLoadedEvent<T>(request.EffectiveRange, this));
                    })
                    .DisposeWith(_requestSubscriptions);
            }
        }

        /// <summary>
        /// Stops listening for items in the requests this block is holding.
        /// </summary>
        public void Unsubscribe()
        {
            _requestSubscriptions.Dispose();
        }

        /// <summary>
        /// Gets a slice of the block's items based on a given subrange.
        /// </summary>
        /// <param name="subrange">The range describing the slice to get.</param>
        /// <returns>A <see cref="Span{T}"/> holding the slice.</returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public Span<T> Slice(IndexRange subrange) => Range.Slice(_items, subrange);

        /// <summary>
        /// Gets the index of a given item based on a given equality comparer.
        /// </summary>
        /// <param name="item">The item to get index.</param>
        /// <returns>The index if found, otherwise -1.</returns>
        public int IndexOf(T item, IEqualityComparer<T> comparer)
        {
            var index = Array.FindIndex(_items, x => comparer.Equals(x, item));
            return index != -1 ? index + Range.FirstIndex : -1;
        }

        /// <summary>
        /// Gets the index of a given item based on reference equality.
        /// </summary>
        /// <param name="item">The item to get index.</param>
        /// <returns>The index if found, otherwise -1.</returns>
        public int IndexOf(T item)
        {
            var index = Array.IndexOf(_items, item);
            return index == -1 ? index + Range.FirstIndex : -1;
        }
    }
}
