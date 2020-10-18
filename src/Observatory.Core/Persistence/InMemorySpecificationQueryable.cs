using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence
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

        public Task<int> CountAsync(ISpecification<T> specification)
        {
            var query = specification.Apply(_source.AsQueryable());
            return Task.FromResult(query.Count());
        }

        public Task<T> FirstOrDefaultAsync(ISpecification<T> specification)
        {
            var query = specification.Apply(_source.AsQueryable());
            return Task.FromResult(query.FirstOrDefault());
        }

        public Task<IReadOnlyList<T>> ToListAsync(ISpecification<T> specification)
        {
            var query = specification.Apply(_source.AsQueryable());
            return Task.FromResult<IReadOnlyList<T>>(query.ToList().AsReadOnly());
        }
    }
}
