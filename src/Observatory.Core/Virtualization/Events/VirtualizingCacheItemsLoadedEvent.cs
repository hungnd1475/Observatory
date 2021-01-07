using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a type of <see cref="IVirtualizingCacheEvent{T}"/> that is fired whenever the items are loaded into the producing cache.
    /// </summary>
    /// <typeparam name="T">The type of items the cache holds.</typeparam>
    public class VirtualizingCacheItemsLoadedEvent<T> : IVirtualizingCacheEvent<T>
        where T : class
    {
        /// <summary>
        /// Gets the range of loaded items. 
        /// </summary>
        public IndexRange Range { get; }

        /// <summary>
        /// Gets the cache block that received the loaded items.
        /// </summary>
        public VirtualizingCacheBlock<T> Block { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="VirtualizingCacheItemsLoadedEvent{T}"/>.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="block"></param>
        public VirtualizingCacheItemsLoadedEvent(IndexRange range, VirtualizingCacheBlock<T> block)
        {
            Range = range;
            Block = block;
        }

        public R Process<R>(IVirtualizingCacheEventProcessor<T, R> processor)
        {
            return processor.Process(this);
        }

        public void Process(IVirtualizingCacheEventProcessor<T> processor)
        {
            processor.Process(this);
        }
    }
}
