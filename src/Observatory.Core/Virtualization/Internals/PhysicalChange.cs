using Observatory.Core.Services.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Virtualization.Internals
{
    /// <summary>
    /// Represents a "physical" change to a collection of type <typeparamref name="T"/>. The only difference between this and <see cref="LogicalChange{T}"/>
    /// is that an update is treated as a removal (of the old item) followed by an addition (of the new item with updated information).
    /// This treatment allows updated items to move to different positions and makes it easier to apply the changes to the array of
    /// cache blocks.
    /// </summary>
    public readonly struct PhysicalChange<T>
        where T : class
    {
        /// <summary>
        /// Gets the action of the change.
        /// </summary>
        public readonly PhysicalChangeAction Action;

        /// <summary>
        /// Gets the entity affected by the change.
        /// </summary>
        public readonly T Entity;

        /// <summary>
        /// Gets the index of the change.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Gets the logical change the physical change originates from.
        /// </summary>
        public readonly LogicalChange<T> LogicalChange;

        /// <summary>
        /// Constructs an instance of <see cref="PhysicalChange{T}"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="entity"></param>
        /// <param name="index"></param>
        public PhysicalChange(PhysicalChangeAction action, T entity, int index, LogicalChange<T> logicalChange)
        {
            Action = action;
            Entity = entity;
            Index = index;
            LogicalChange = logicalChange;
        }
    }

    /// <summary>
    /// Represents an action of a physical change.
    /// </summary>
    public enum PhysicalChangeAction
    {
        /// <summary>
        /// The physical change is an addition.
        /// </summary>
        Add,
        /// <summary>
        /// The physical change is a removal.
        /// </summary>
        Remove,
    }

    /// <summary>
    /// Represents a collection of <see cref="PhysicalChange{T}"/>.
    /// </summary>
    public readonly struct PhysicalChangeSet<T>
        where T : class
    {
        /// <summary>
        /// Gets the changes that are additions, sorted by index in ascending order.
        /// </summary>
        public readonly PhysicalChange<T>[] Additions;

        /// <summary>
        /// Gets the changes that are removals, sorted by index in ascending order.
        /// </summary>
        public readonly PhysicalChange<T>[] Removals;

        /// <summary>
        /// Constructs an instance of <see cref="PhysicalChangeSet{T}"/> from an array of <see cref="LogicalChange{T}"/>.
        /// </summary>
        /// <param name="logicalChanges">The logical changes.</param>
        public PhysicalChangeSet(IEnumerable<LogicalChange<T>> logicalChanges)
        {
            Additions = logicalChanges.Where(c => c.State != DeltaState.Remove)
                .Select(c => new PhysicalChange<T>(PhysicalChangeAction.Add, c.CurrentEntity, c.CurrentIndex.Value, c))
                .OrderBy(c => c.Index)
                .ToArray();
            Removals = logicalChanges.Where(c => c.State != DeltaState.Add)
                .Select(c => new PhysicalChange<T>(PhysicalChangeAction.Remove, c.PreviousEntity, c.PreviousIndex.Value, c))
                .OrderBy(c => c.Index)
                .ToArray();
        }

        public override string ToString()
        {
            return $"{nameof(PhysicalChangeSet<T>)} {{ Additions: [{string.Join(", ", Additions.Select(c => c.Index))}], Removals: [{string.Join(", ", Removals.Select(c => c.Index))}] }}";
        }
    }
}
