﻿using ReactiveUI;
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
    /// <typeparam name="T">The source type.</typeparam>
    public class VirtualizingCacheBlockRequest<T>
    {
        /// <summary>
        /// Gets the items, exposed as an <see cref="IObservable{T}"/> so that the owner <see cref="VirtualizingCacheBlock{T}"/> 
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
        /// Constructs an instance of <see cref="VirtualizingCacheBlockRequest{T}"/> that retrieves items from source.
        /// </summary>
        /// <param name="range">The full range of the items.</param>
        /// <param name="source">The source where items are fetched from.</param>
        public VirtualizingCacheBlockRequest(IndexRange range,
            IVirtualizingSource<T> source)
        {
            FullRange = range;
            EffectiveRange = range;
            IsReceived = false;
            WhenItemsLoaded = Observable.Start(() => source.GetItems(range.FirstIndex, range.Length).ToArray(), RxApp.TaskpoolScheduler);
        }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheBlockRequest{T}"/> that gets items from another request.
        /// </summary>
        /// <param name="fullRange"></param>
        /// <param name="effectiveRange"></param>
        /// <param name="whenItemsLoaded"></param>
        public VirtualizingCacheBlockRequest(IndexRange fullRange,
            IndexRange effectiveRange,
            IObservable<T[]> whenItemsLoaded)
        {
            FullRange = fullRange;
            EffectiveRange = effectiveRange;
            IsReceived = false;
            WhenItemsLoaded = whenItemsLoaded;
        }
    }
}
