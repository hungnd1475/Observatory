using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Defines a common interface for all events produced by a <see cref="VirtualizingCache{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items the cache holds.</typeparam>
    public interface IVirtualizingCacheEvent<T>
    {
        /// <summary>
        /// Given an instance of <see cref="IVirtualizingCacheEventProcessor{T, R}"/>, processes the event and returns the result.
        /// </summary>
        /// <typeparam name="R">The type of result.</typeparam>
        /// <param name="processor">The processor that know how to processes the event.</param>
        /// <returns></returns>
        R Process<R>(IVirtualizingCacheEventProcessor<T, R> processor);
    }
}
