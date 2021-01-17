using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public class VirtualizingCachePlaceholder
    {
        public int Index { get; }

        public VirtualizingCachePlaceholder(int index)
        {
            Index = index;
        }
    }
}
