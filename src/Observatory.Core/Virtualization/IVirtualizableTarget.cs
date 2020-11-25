using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Defines an interface implemented by any class that servces as the target for a virtualizing list.
    /// </summary>
    /// <typeparam name="TSource">The type of source.</typeparam>
    public interface IVirtualizableTarget<TSource>
    {
        /// <summary>
        /// Refreshes whenever the source changed.
        /// </summary>
        /// <param name="source">The source that has changed.</param>
        void Refresh(TSource source);
    }
}
