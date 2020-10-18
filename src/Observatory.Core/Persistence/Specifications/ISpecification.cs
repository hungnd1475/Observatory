using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Defines a specification applied on entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface ISpecification<T>
    {
        /// <summary>
        /// Applies the specification on a source queryable.
        /// </summary>
        /// <param name="source">The source queryable to apply the specification on.</param>
        /// <returns>A <see cref="IQueryable{T}"/> that satisfies the specification.</returns>
        IQueryable<T> Apply(IQueryable<T> source);
    }
}
