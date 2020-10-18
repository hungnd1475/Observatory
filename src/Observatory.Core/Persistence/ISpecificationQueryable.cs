using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence
{
    /// <summary>
    /// Defines a query execution engine that turns a <see cref="ISpecification{T}"/> to an actual result.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface ISpecificationQueryable<T>
    {
        /// <summary>
        /// Returns a list of elements satisfying a given <see cref="ISpecification{T}"/>.
        /// </summary>
        /// <param name="specification">The specification.</param>
        /// <returns></returns>
        Task<IReadOnlyList<T>> ToListAsync(ISpecification<T> specification);

        /// <summary>
        /// Returns the first element satisfying a given <see cref=" ISpecification{T}"/>.
        /// </summary>
        /// <param name="specification">The specification.</param>
        /// <returns></returns>
        Task<T> FirstOrDefaultAsync(ISpecification<T> specification);

        /// <summary>
        /// Returns the number of elements satisfying a given <see cref="ISpecification{T}"/>.
        /// </summary>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<int> CountAsync(ISpecification<T> specification);
    }
}
