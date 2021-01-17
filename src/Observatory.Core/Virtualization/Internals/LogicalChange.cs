using Observatory.Core.Services.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Observatory.Core.Virtualization.Internals
{
    /// <summary>
    /// Represents a "logical" change to a collection of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class LogicalChange<T>
        where T : class
    {
        /// <summary>
        /// Gets the type of the change.
        /// </summary>
        public readonly DeltaState State;

        /// <summary>
        /// Gets the entity resulted after the change.
        /// </summary>
        public readonly T CurrentEntity;

        /// <summary>
        /// Gets the entity before the change.
        /// </summary>
        public readonly T PreviousEntity;

        /// <summary>
        /// Gets the index of the entity after the change.
        /// </summary>
        public readonly int? CurrentIndex;

        /// <summary>
        /// Gets the index of the entity after the change.
        /// </summary>
        public readonly int? PreviousIndex;

        /// <summary>
        /// Constructs an instance of <see cref="LogicalChange{T}"/>.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="currentEntity"></param>
        /// <param name="previousEntity"></param>
        /// <param name="currentIndex"></param>
        /// <param name="previousIndex"></param>
        public LogicalChange(DeltaState state, T currentEntity, T previousEntity, int? currentIndex, int? previousIndex)
        {
            State = state;
            CurrentEntity = currentEntity;
            PreviousEntity = previousEntity;
            CurrentIndex = currentIndex;
            PreviousIndex = previousIndex;
        }

        public override string ToString()
        {
            return State switch
            {
                DeltaState.Add => $"{nameof(LogicalChange<T>)} {{ {State} at {CurrentIndex.Value} }}",
                DeltaState.Remove => $"{nameof(LogicalChange<T>)} {{ {State} at {PreviousIndex.Value} }}",
                DeltaState.Update => $"{nameof(LogicalChange<T>)} {{ {State} from {PreviousIndex.Value} to {CurrentIndex.Value} }}",
                _ => throw new NotSupportedException(),
            };
        }
    }

    public static class LogicalChange
    {
        /// <summary>
        /// Creates a logical change that is an addition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentEntity"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        public static LogicalChange<T> Addition<T>(T currentEntity, int currentIndex)
            where T : class
        {
            return new LogicalChange<T>(DeltaState.Add, currentEntity, default, currentIndex, default);
        }

        /// <summary>
        /// Creates a logical change that is a removal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="previousEntity"></param>
        /// <param name="previousIndex"></param>
        /// <returns></returns>
        public static LogicalChange<T> Removal<T>(T previousEntity, int previousIndex)
            where T : class
        {
            return new LogicalChange<T>(DeltaState.Remove, default, previousEntity, default, previousIndex);
        }

        /// <summary>
        /// Creates a logical change that is an update.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentEntity"></param>
        /// <param name="currentIndex"></param>
        /// <param name="previousEntity"></param>
        /// <param name="previousIndex"></param>
        /// <returns></returns>
        public static LogicalChange<T> Update<T>(T currentEntity, int currentIndex, T previousEntity, int previousIndex)
            where T : class
        {
            return new LogicalChange<T>(DeltaState.Update, currentEntity, previousEntity, currentIndex, previousIndex);
        }

        /// <summary>
        /// Creates a <see cref="PhysicalChangeSet{T}"/> from an array of <see cref="LogicalChange{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logicalChanges"></param>
        /// <returns></returns>
        public static PhysicalChangeSet<T> ToPhysicalChangeSet<T>(this IEnumerable<LogicalChange<T>> logicalChanges)
            where T : class
        {
            return new PhysicalChangeSet<T>(logicalChanges);
        }

        /// <summary>
        /// Serializes the logical changes in an order that can be applied one-by-one by the UI.
        /// </summary>
        /// <param name="logicalChanges">The logical changes.</param>
        /// <returns></returns>
        public static IReadOnlyList<VirtualizingCacheSourceChange<T>> Serialize<T>(this IEnumerable<LogicalChange<T>> logicalChanges)
            where T : class
        {
            var serializedChanges = new List<VirtualizingCacheSourceChange<T>>();
            var additions = logicalChanges.Where(c => c.State == DeltaState.Add)
                .OrderBy(c => c.CurrentIndex.Value)
                .ToArray();
            var removalsAndUpdates = logicalChanges.Where(c => c.State != DeltaState.Add)
                .OrderBy(c => c.PreviousIndex.Value)
                .ToArray();

            var additionPointer = 0;
            var removalAndUpdatePointer = 0;
            var shift = 0;

            while (true)
            {
                LogicalChange<T> currentChange = null;
                if (additionPointer < additions.Length && removalAndUpdatePointer < removalsAndUpdates.Length)
                {
                    if (additions[additionPointer].CurrentIndex.Value < removalsAndUpdates[removalAndUpdatePointer].PreviousIndex.Value + shift)
                    {
                        currentChange = additions[additionPointer];
                        additionPointer += 1;
                    }
                    else
                    {
                        currentChange = removalsAndUpdates[removalAndUpdatePointer];
                        removalAndUpdatePointer += 1;
                    }
                }
                else if (additionPointer < additions.Length)
                {
                    currentChange = additions[additionPointer];
                    additionPointer += 1;
                }
                else if (removalAndUpdatePointer < removalsAndUpdates.Length)
                {
                    currentChange = removalsAndUpdates[removalAndUpdatePointer];
                    removalAndUpdatePointer += 1;
                }

                if (currentChange != null)
                {
                    switch (currentChange.State)
                    {
                        case DeltaState.Add:
                            serializedChanges.Add(VirtualizingCacheSourceChange<T>.Addition(
                                currentChange.CurrentEntity,
                                currentChange.CurrentIndex.Value));
                            shift += 1;
                            break;
                        case DeltaState.Remove:
                            serializedChanges.Add(VirtualizingCacheSourceChange<T>.Removal(
                                currentChange.PreviousEntity,
                                currentChange.PreviousIndex.Value + shift));
                            shift -= 1;
                            break;
                        case DeltaState.Update:
                            serializedChanges.Add(VirtualizingCacheSourceChange<T>.Update(
                                currentChange.CurrentEntity,
                                currentChange.CurrentIndex.Value,
                                currentChange.PreviousEntity,
                                currentChange.PreviousIndex.Value + shift));
                            break;
                    }
                }
                else
                    break;
            }

            return serializedChanges.AsReadOnly();
        }
    }
}
