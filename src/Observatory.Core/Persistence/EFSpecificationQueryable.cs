using Microsoft.EntityFrameworkCore;
using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence
{
    /// <summary>
    /// Represents a type of <see cref="ISpecificationQueryable{T}"/> that works with Entity Framework Core.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EFSpecificationQueryable<T> : ISpecificationQueryable<T>
        where T : class
    {
        private readonly DbSet<T> _source;

        /// <summary>
        /// Constructs an instance of <see cref="EFSpecificationQueryable{T}"/> that works on a <see cref="DbSet{TEntity}"/>.
        /// </summary>
        /// <param name="source"></param>
        public EFSpecificationQueryable(DbSet<T> source)
        {
            _source = source;
        }

        public async Task<int> CountAsync(ISpecification<T> specification)
        {
            var query = specification.Apply(_source);
            return await query.CountAsync();
        }

        public async Task<T> FirstOrDefaultAsync(ISpecification<T> specification)
        {
            var query = specification.Apply(_source);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<T>> ToListAsync(ISpecification<T> specification)
        {
            var query = specification.Apply(_source);
            return await query.ToListAsync();
        }
    }
}
