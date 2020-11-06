using Observatory.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public struct VirtualizingCacheSourceChange<T>
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

        public VirtualizingCacheSourceChange(DeltaEntity<T> change, int index, int? previousIndex = null)
        {
            Change = change;
            Index = index;
            PreviousIndex = previousIndex;
        }
    }
}
