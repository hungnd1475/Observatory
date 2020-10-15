using DynamicData.Kernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence.Delta
{
    /// <summary>
    /// Represents a removal of an entity in a collection.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    public struct Removal<T> : IChange<T>
    {
        /// <summary>
        /// The id of the removed entity.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="Removal{T}"/>.
        /// </summary>
        /// <param name="id">The id of the removed entity.</param>
        public Removal(string id)
        {
            Id = id;
        }
    }
}
