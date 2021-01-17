using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Represents a type of <see cref="ISpecification{TSource, TTarget}"/> that simply returns the source queryable.
    /// </summary>
    public struct IdentitySpecification<T> : ISpecification<T, T>
    {
        public IQueryable<T> Apply(IQueryable<T> source)
        {
            return source;
        }
    }
}
