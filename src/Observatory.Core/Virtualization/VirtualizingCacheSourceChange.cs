using Observatory.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a single change is the source that got its index figured out.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VirtualizingCacheSourceChange<T>
    {
        /// <summary>
        /// Gets the single change happened to the source.
        /// </summary>
        public DeltaEntity<T> Change { get; }

        /// <summary>
        /// Gets the current index of item affected by the change.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the previous index of item affected by the change, only valid if 
        /// the state is <see cref="DeltaState.Update"/>.
        /// </summary>
        public int? PreviousIndex { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheSourceChange{T}"/>.
        /// </summary>
        /// <param name="change">The original change.</param>
        /// <param name="index">The current index of the change.</param>
        /// <param name="previousIndex">The previous index of the item affected by the change, if any.</param>
        public VirtualizingCacheSourceChange(DeltaEntity<T> change, int index, int? previousIndex = null)
        {
            Change = change;
            Index = index;
            PreviousIndex = previousIndex;
        }
    }
}
