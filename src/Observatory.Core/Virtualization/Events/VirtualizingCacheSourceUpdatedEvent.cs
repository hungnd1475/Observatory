using Observatory.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a type of <see cref="IVirtualizingCacheEvent{T}"/> that gets fired after the source of items has been changed in the producing cache.
    /// </summary>
    /// <typeparam name="T">The type of items the cache holds.</typeparam>
    public class VirtualizingCacheSourceUpdatedEvent<T> : IVirtualizingCacheEvent<T>
        where T : class
    {
        /// <summary>
        /// Gets the items that are discarded from the cache.
        /// </summary>
        public IReadOnlyList<T> DiscardedItems { get; }

        /// <summary>
        /// Gets the changes happened to the source.
        /// </summary>
        public IReadOnlyList<VirtualizingCacheSourceChange<T>> Changes { get; }

        /// <summary>
        /// Gets the new total number of items in the source.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheSourceUpdatedEvent{T}"/>.
        /// </summary>
        /// <param name="changes"></param>
        public VirtualizingCacheSourceUpdatedEvent(IReadOnlyList<T> discardedItems,
            IReadOnlyList<VirtualizingCacheSourceChange<T>> changes, int totalCount)
        {
            DiscardedItems = discardedItems;
            Changes = changes;
            TotalCount = totalCount;
        }

        public R Process<R>(IVirtualizingCacheEventProcessor<T, R> processor)
        {
            return processor.Process(this);
        }
    }
}
