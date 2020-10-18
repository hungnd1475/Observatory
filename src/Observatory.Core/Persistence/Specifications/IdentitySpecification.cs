using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Represents a type of <see cref="ISpecification{T}"/> that simply returns the source queryable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct IdentitySpecification<T> : ISpecification<T>
    {
        public IQueryable<T> Apply(IQueryable<T> source)
        {
            return source;
        }
    }
}
