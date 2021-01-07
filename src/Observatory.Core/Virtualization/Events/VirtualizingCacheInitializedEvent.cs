using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a concrete type of <see cref="IVirtualizingCacheEvent{T}"/> that is fired the first time the producing cache is initialized.
    /// </summary>
    /// <typeparam name="T">The type of items the cache holds.</typeparam>
    public class VirtualizingCacheInitializedEvent<T> : IVirtualizingCacheEvent<T>
        where T : class
    {
        /// <summary>
        /// Gets the total number of items in the source.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheInitializedEvent{T}"/>.
        /// </summary>
        /// <param name="totalCount">The total number of items in the source.</param>
        public VirtualizingCacheInitializedEvent(int totalCount)
        {
            TotalCount = totalCount;
        }

        public TResult Process<TResult>(IVirtualizingCacheEventProcessor<T, TResult> processor)
        {
            return processor.Process(this);
        }

        public void Process(IVirtualizingCacheEventProcessor<T> processor)
        {
            processor.Process(this);
        }
    }
}
