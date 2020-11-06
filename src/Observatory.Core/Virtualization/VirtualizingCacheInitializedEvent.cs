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
    {
        public TResult Process<TResult>(IVirtualizingCacheEventProcessor<T, TResult> processor)
        {
            return processor.Process(this);
        }
    }
}
