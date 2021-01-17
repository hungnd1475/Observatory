using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Defines a specification applied on entities of type <typeparamref name="TSource"/> and producing results of type <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public interface ISpecification<TSource, TTarget>
    {
        /// <summary>
        /// Applies the specification on a source queryable.
        /// </summary>
        /// <param name="source">The source queryable to apply the specification on.</param>
        /// <returns>A <see cref="IQueryable{T}"/> that satisfies the specification.</returns>
        IQueryable<TTarget> Apply(IQueryable<TSource> source);
    }
}
