using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a request for items of a subrange in a <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public class VirtualizingCacheBlockRequest<TSource, TTarget>
    {
        private readonly IConnectableObservable<TTarget[]> _connectableItems;

        /// <summary>
        /// The items to be loaded.
        /// </summary>
        public IObservable<TTarget[]> Items { get; }

        /// <summary>
        /// The full range of this items.
        /// </summary>
        public IndexRange FullRange { get; }

        /// <summary>
        /// The effective range.
        /// </summary>
        public IndexRange EffectiveRange { get; }

        /// <summary>
        /// Gets the connection to the items publication.
        /// </summary>
        public IDisposable ItemsConnection { get; private set; }

        /// <summary>
        /// Gets or sets whether the <see cref="VirtualizingCacheBlock{TSource, TTarget}"/> already received the items.
        /// </summary>
        public bool IsReceived { get; set; } = false;

        /// <summary>
        /// Constructs a <see cref="VirtualizingCacheBlockRequest{TSource, TTarget}"/> that gets items from source,
        /// hence making effective range the same as full range.
        /// </summary>
        /// <param name="range">The full range of the items.</param>
        /// <param name="source">The source where items are fetched from.</param>
        /// <param name="targetFactory">The factory function transforming <see cref="TSource"/> to <see cref="TTarget"/>.</param>
        public VirtualizingCacheBlockRequest(IndexRange range,
            IVirtualizingSource<TSource> source,
            Func<TSource, TTarget> targetFactory)
        {
            _connectableItems = Observable.Defer(() => Observable.Start(() =>
            {
                return source.GetItems(range.FirstIndex, range.Length)
                    .Select(targetFactory)
                    .ToArray();
            })).Publish();
            FullRange = range;
            EffectiveRange = range;
            Items = _connectableItems;
        }

        /// <summary>
        /// Constructs a <see cref="VirtualizingCacheBlockRequest{TSource, TTarget}"/> that get items from another request,
        /// with the effective range potentially different from the full range.
        /// </summary>
        /// <param name="fullRange">The full range of the items.</param>
        /// <param name="effectiveRange">The effective range of the items.</param>
        /// <param name="items">The items from another request.</param>
        public VirtualizingCacheBlockRequest(IndexRange fullRange,
            IndexRange effectiveRange,
            IObservable<TTarget[]> items,
            IDisposable itemsConnection)
        {
            FullRange = fullRange;
            EffectiveRange = effectiveRange;
            Items = items;
            ItemsConnection = itemsConnection;
        }

        /// <summary>
        /// Starts fetching items if they originates from this request.
        /// </summary>
        public void Start()
        {
            if (_connectableItems != null && ItemsConnection == null)
            {
                ItemsConnection = _connectableItems.Connect();
            }
        }
    }
}
