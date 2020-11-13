using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a type of <see cref="IVirtualizingCacheEvent{T}"/> that gets fired after the ranges are updated in the producing cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VirtualizingCacheRangesUpdatedEvent<T> : IVirtualizingCacheEvent<T>
        where T : class
    {
        /// <summary>
        /// Gets the items that are discarded.
        /// </summary>
        public IReadOnlyList<T> DiscardedItems { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheRangesUpdatedEvent{T}"/>.
        /// </summary>
        /// <param name="discardedItems"></param>
        public VirtualizingCacheRangesUpdatedEvent(IReadOnlyList<T> discardedItems)
        {
            DiscardedItems = discardedItems;
        }

        public R Process<R>(IVirtualizingCacheEventProcessor<T, R> processor)
        {
            return processor.Process(this);
        }
    }
}
