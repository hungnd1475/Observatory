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
    {
        /// <summary>
        /// Gets the ranges and items that are discarded.
        /// </summary>
        public (IndexRange Range, T[] Items)[] Removals { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheRangesUpdatedEvent{T}"/>.
        /// </summary>
        /// <param name="removals"></param>
        public VirtualizingCacheRangesUpdatedEvent((IndexRange, T[])[] removals)
        {
            Removals = removals;
        }

        public R Process<R>(IVirtualizingCacheEventProcessor<T, R> processor)
        {
            return processor.Process(this);
        }
    }
}
