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
    /// Represents a request for items of a subrange in a <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public class VirtualizingCacheBlockRequest<TSource, TTarget>
    {
        /// <summary>
        /// Gets the items, exposed as an <see cref="IObservable{T}"/> so that the associated <see cref="VirtualizingCacheBlock{TSource, TTarget}"/> 
        /// can subscribe to observe the result when it finished loading.
        /// </summary>
        public IObservable<TTarget[]> Items { get; }

        /// <summary>
        /// Gets the full range of this request.
        /// </summary>
        public IndexRange FullRange { get; }

        /// <summary>
        /// Gets or sets the effective range.
        /// </summary>
        public IndexRange EffectiveRange { get; set; }

        /// <summary>
        /// Gets or sets whether the associated <see cref="VirtualizingCacheBlock{TSource, TTarget}"/> already received the items.
        /// </summary>
        public bool IsReceived { get; set; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheBlockRequest{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="range">The full range of the items.</param>
        /// <param name="source">The source where items are fetched from.</param>
        /// <param name="targetFactory">The factory function transforming <see cref="TSource"/> to <see cref="TTarget"/>.</param>
        public VirtualizingCacheBlockRequest(IndexRange range,
            IVirtualizingSource<TSource> source,
            Func<TSource, TTarget> targetFactory)
        {
            FullRange = range;
            EffectiveRange = range;
            IsReceived = false;
            Items = Observable.Start(() =>
            {
                return source.GetItems(range.FirstIndex, range.Length)
                    .Select(targetFactory)
                    .ToArray();
            }, 
            RxApp.TaskpoolScheduler);
        }
    }
}
