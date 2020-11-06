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
    {
        /// <summary>
        /// Gets the changes happened to the source.
        /// </summary>
        public IEnumerable<VirtualizingCacheSourceChange<T>> Changes { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheSourceUpdatedEvent{T}"/>.
        /// </summary>
        /// <param name="changes"></param>
        public VirtualizingCacheSourceUpdatedEvent(IEnumerable<VirtualizingCacheSourceChange<T>> changes)
        {
            Changes = changes;
        }

        public R Process<R>(IVirtualizingCacheEventProcessor<T, R> processor)
        {
            return processor.Process(this);
        }
    }
}
