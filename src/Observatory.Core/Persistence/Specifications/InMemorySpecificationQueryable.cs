using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Represents a type of <see cref="ISpecificationQueryable{T}"/> that works with an in-memory data source.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InMemorySpecificationQueryable<T> : ISpecificationQueryable<T>
    {
        private readonly IEnumerable<T> _source;

        /// <summary>
        /// Constructs an instance of <see cref="InMemorySpecificationQueryable{T}"/> with a given in-memory data source.
        /// </summary>
        /// <param name="source">The in-memory data source.</param>
        public InMemorySpecificationQueryable(IEnumerable<T> source)
        {
            _source = source;
        }

        public int Count<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source.AsQueryable());
            return query.Count();
        }

        public TResult FirstOrDefault<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source.AsQueryable());
            return query.FirstOrDefault();
        }

        public TResult[] ToArray<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source.AsQueryable());
            return query.ToArray();
        }

        public List<TResult> ToList<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source.AsQueryable());
            return query.ToList();
        }
    }
}
