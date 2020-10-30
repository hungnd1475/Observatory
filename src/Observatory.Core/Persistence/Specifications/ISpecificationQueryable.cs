using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence.Specifications
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
        IReadOnlyList<T> ToList(ISpecification<T> specification);

        /// <summary>
        /// Returns an array of elements satisfying a given <see cref="ISpecification{T}"/>.
        /// </summary>
        /// <param name="specification">The specification.</param>
        /// <returns></returns>
        T[] ToArray(ISpecification<T> specification);

        /// <summary>
        /// Returns the first element satisfying a given <see cref=" ISpecification{T}"/>.
        /// </summary>
        /// <param name="specification">The specification.</param>
        /// <returns></returns>
        T FirstOrDefault(ISpecification<T> specification);

        /// <summary>
        /// Returns the number of elements satisfying a given <see cref="ISpecification{T}"/>.
        /// </summary>
        /// <param name="specification"></param>
        /// <returns></returns>
        int Count(ISpecification<T> specification);
    }
}
