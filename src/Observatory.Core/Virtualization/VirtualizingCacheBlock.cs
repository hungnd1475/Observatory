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

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] => Items[index - Range.FirstIndex];

        /// <summary>
        /// Gets the range of the block.
        /// </summary>
        public IndexRange Range { get; }

        /// <summary>
        /// Gets the array of items the block is holding.
        /// </summary>
        public T[] Items { get; }

        /// <summary>
        /// Gets the requests of new items.
        /// </summary>
        public IEnumerable<VirtualizingCacheBlockRequest<T>> Requests { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="range">The range of the block.</param>
        /// <param name="items">The items the block holds.</param>
        /// <param name="requests">The requests for new items.</param>
        /// <param name="observer">The observer.</param>
        public VirtualizingCacheBlock(IndexRange range, T[] items,
            IEnumerable<VirtualizingCacheBlockRequest<T>> requests)
        {
            Range = range;
            Items = items;
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
                request.WhenItemsLoaded.Subscribe(items =>
                {
                    var sourceIndex = request.EffectiveRange.FirstIndex - request.FullRange.FirstIndex;
                    var destinationIndex = request.EffectiveRange.FirstIndex - Range.FirstIndex;
                    var length = Math.Min(request.EffectiveRange.Length, items.Length);
                    items.AsSpan(sourceIndex, length).CopyTo(Items.AsSpan(destinationIndex, length));
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
    }
}
