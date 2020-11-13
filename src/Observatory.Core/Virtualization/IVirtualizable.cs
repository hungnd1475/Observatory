using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public interface IVirtualizable<TSource>
    {
        void Refresh(TSource source);
    }
}
