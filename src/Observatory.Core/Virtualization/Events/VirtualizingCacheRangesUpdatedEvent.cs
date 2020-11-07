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
        /// Gets the ranges that are no longer needed.
        /// </summary>
        public IndexRange[] RemovedRanges { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheRangesUpdatedEvent{T}"/>.
        /// </summary>
        /// <param name="removedRanges"></param>
        public VirtualizingCacheRangesUpdatedEvent(IndexRange[] removedRanges)
        {
            RemovedRanges = removedRanges;
        }

        public R Process<R>(IVirtualizingCacheEventProcessor<T, R> processor)
        {
            return processor.Process(this);
        }
    }
}
