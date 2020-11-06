using Observatory.Core.Virtualization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.UI.Virtualizing
{
    public class VirtualizingListBlock<TSource, TTarget>
    {
        public IndexRange Range { get; }

        public List<TTarget> Items { get; }


    }
}
