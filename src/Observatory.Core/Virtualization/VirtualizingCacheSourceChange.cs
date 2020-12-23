using Observatory.Core.Services;
using Observatory.Core.Services.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a single change in the source that got its index figured out.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct VirtualizingCacheSourceChange<T>
        where T : class
    {
        /// <summary>
        /// Gets the state of the change.
        /// </summary>
        public DeltaState State { get; }

        /// <summary>
        /// Gets the current item resulted after the change, only valid 
        /// for <see cref="DeltaState.Add"/> or <see cref="DeltaState.Update"/>.
        /// </summary>
        public T CurrentItem { get; }

        /// <summary>
        /// Gets the previous item affected by the change, only valid
        /// for <see cref="DeltaState.Update"/> of <see cref="DeltaState.Remove"/> and
        /// can be null if it is not tracked by the cache.
        /// </summary>
        public T PreviousItem { get; }

        /// <summary>
        /// Gets the current index of item affected by the change, only valid
        /// for <see cref="DeltaState.Add"/> or <see cref="DeltaState.Update"/>.
        /// </summary>
        public int? CurrentIndex { get; }

        /// <summary>
        /// Gets the previous index of item affected by the change, only valid for 
        /// <see cref="DeltaState.Update"/> or <see cref="DeltaState.Remove"/>.
        /// </summary>
        public int? PreviousIndex { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheSourceChange{T}"/>.
        /// </summary>
        /// <param name="change">The original change.</param>
        /// <param name="currentIndex">The current index of the change.</param>
        /// <param name="previousIndex">The previous index of the item affected by the change, if any.</param>
        private VirtualizingCacheSourceChange(DeltaState state, T currentItem, int? currentIndex, T previousItem, int? previousIndex)
        {
            State = state;
            CurrentItem = currentItem;
            PreviousItem = previousItem;
            CurrentIndex = currentIndex;
            PreviousIndex = previousIndex;
        }

        public override string ToString()
        {
            return State switch
            {
                DeltaState.Add => $"{State} at {CurrentIndex.Value}",
                DeltaState.Remove => $"{State} at {PreviousIndex.Value}",
                DeltaState.Update => $"{State} from {PreviousIndex.Value} to {CurrentIndex.Value}",
                _ => base.ToString(),
            };
        }

        public static VirtualizingCacheSourceChange<T> Addition(T item, int currentIndex)
        {
            return new VirtualizingCacheSourceChange<T>(DeltaState.Add, item, currentIndex, null, null);
        }

        public static VirtualizingCacheSourceChange<T> Update(T currentItem, int currentIndex, T previousItem, int previousIndex)
        {
            return new VirtualizingCacheSourceChange<T>(DeltaState.Update, currentItem, currentIndex, previousItem, previousIndex);
        }

        public static VirtualizingCacheSourceChange<T> Removal(T previousItem, int previousIndex)
        {
            return new VirtualizingCacheSourceChange<T>(DeltaState.Remove, null, null, previousItem, previousIndex);
        }
    }
}
