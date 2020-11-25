using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a request for items of a subrange in a <see cref="VirtualizingCacheBlock{T}"/>.
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    public class VirtualizingCacheBlockRequest<T>
    {
        /// <summary>
        /// Gets the items, exposed as an <see cref="IObservable{T}"/> so that the owning <see cref="VirtualizingCacheBlock{T}"/> 
        /// can subscribe to observe the result when it finished loading.
        /// </summary>
        public IObservable<T[]> WhenItemsLoaded { get; }

        /// <summary>
        /// Gets the full range of this request.
        /// </summary>
        public IndexRange FullRange { get; }

        /// <summary>
        /// Gets or sets the effective range.
        /// </summary>
        public IndexRange EffectiveRange { get; }

        /// <summary>
        /// Gets whether the request is disposed.
        /// </summary>
        public bool IsReceived { get; set; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheBlockRequest{T}"/>.
        /// </summary>
        /// <param name="fullRange">The full range of the retrieved items.</param>
        /// <param name="effectiveRange">The effective range that the owning <see cref="VirtualizingCacheBlock{T}"/> needs.</param>
        /// <param name="source">The source of items transfered from another request.</param>
        private VirtualizingCacheBlockRequest(IndexRange fullRange,
            IndexRange effectiveRange,
            IObservable<T[]> source)
        {
            FullRange = fullRange;
            EffectiveRange = effectiveRange;
            IsReceived = false;
            WhenItemsLoaded = source;
        }

        /// <summary>
        /// Transfers to another request with a given effective range.
        /// </summary>
        /// <param name="effectiveRange">The effective range of the other request.</param>
        /// <returns></returns>
        public VirtualizingCacheBlockRequest<T> Transfer(IndexRange effectiveRange)
        {
            return new VirtualizingCacheBlockRequest<T>(FullRange, effectiveRange, WhenItemsLoaded);
        }

        /// <summary>
        /// Constructs a new <see cref="VirtualizingCacheBlockRequest{T}"/> that gets items from a <see cref="IVirtualizingSource{TEntity, TKey}"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of item's key.</typeparam>
        /// <param name="range">The range of the request.</param>
        /// <param name="source">The source to retrieve items from.</param>
        /// <returns></returns>
        public static VirtualizingCacheBlockRequest<T> FromSource<TKey>(IndexRange range, IVirtualizingSource<T, TKey> source)
        {
            return new VirtualizingCacheBlockRequest<T>(range, range,
                Observable.Start(() => source.GetItems(range.FirstIndex, range.Length).ToArray(), RxApp.TaskpoolScheduler));
        }
    }
}
