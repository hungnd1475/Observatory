using DynamicData.Kernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence.Delta
{
    /// <summary>
    /// Represents an addition of an entity to a collection.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    public struct Addition<T> : IChange<T>
    {
        /// <summary>
        /// The added entity.
        /// </summary>
        public T Entity { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="Addition{T}"/>.
        /// </summary>
        /// <param name="entity">The added entity.</param>
        public Addition(T entity)
        {
            Entity = entity;
        }
    }
}
