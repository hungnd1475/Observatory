using DynamicData.Kernel;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Observatory.Core.Persistence.Delta
{
    /// <summary>
    /// Represents a modification of an entity in a collection.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    public struct Modification<T> : IChange<T>
    {
        private readonly Dictionary<string, object> _updatedProperties;

        /// <summary>
        /// The id of the modified entity.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="Modification{T}"/>.
        /// </summary>
        /// <param name="id">The id of the modified entity.</param>
        /// <param name="updatedProperties">The updated properties.</param>
        public Modification(string id, Dictionary<string, object> updatedProperties)
        {
            _updatedProperties = updatedProperties;
            Id = id;
        }

        /// <summary>
        /// Applies the modification to the entity.
        /// </summary>
        /// <param name="entity"></param>
        public void Apply(T entity)
        {
            var entityClass = entity.GetType();
            foreach (var p in _updatedProperties)
            {
                entityClass.GetProperty(p.Key).SetValue(entity, p.Value);
            }
        }
    }
}
