using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public interface IVirtualizableSource<TKey>
    {
        /// <summary>
        /// Gets the key.
        /// </summary>
        TKey Id { get; }
    }
}
