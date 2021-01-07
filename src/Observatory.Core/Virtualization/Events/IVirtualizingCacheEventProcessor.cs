using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Defines a contract for implementing an event processor that processes instances of <see cref="IVirtualizingCacheEvent{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the items held by the cache that produces instances of <see cref="IVirtualizingCacheEvent{T}"/>.</typeparam>
    /// <typeparam name="R">The type of result returned after event is processed.</typeparam>
    public interface IVirtualizingCacheEventProcessor<T, R>
        where T : class
    {
        R Process(VirtualizingCacheInitializedEvent<T> e);
        R Process(VirtualizingCacheItemsLoadedEvent<T> e);
        R Process(VirtualizingCacheRangesUpdatedEvent<T> e);
        R Process(VirtualizingCacheSourceUpdatedEvent<T> e);
    }

    public interface IVirtualizingCacheEventProcessor<T>
        where T : class
    {
        void Process(VirtualizingCacheInitializedEvent<T> e);
        void Process(VirtualizingCacheItemsLoadedEvent<T> e);
        void Process(VirtualizingCacheRangesUpdatedEvent<T> e);
        void Process(VirtualizingCacheSourceUpdatedEvent<T> e);
    }
}
