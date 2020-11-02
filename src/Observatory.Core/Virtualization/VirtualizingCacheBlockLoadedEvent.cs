using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public struct VirtualizingCacheBlockLoadedEvent<TSource, TTarget> : IVirtualizingCacheChangedEvent
    {
        public IndexRange Range { get; }
        public VirtualizingCacheBlock<TSource, TTarget> Block { get; }

        public VirtualizingCacheBlockLoadedEvent(IndexRange range, VirtualizingCacheBlock<TSource, TTarget> block)
        {
            Range = range;
            Block = block;
        }
    }
}
