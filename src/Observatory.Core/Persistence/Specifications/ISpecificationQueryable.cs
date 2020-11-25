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
        /// Returns a list of elements satisfying a given <see cref="ISpecification{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="specification">The specification.</param>
        /// <returns></returns>
        List<TResult> ToList<TResult>(ISpecification<T, TResult> specification);

        /// <summary>
        /// Returns an array of elements satisfying a given <see cref="ISpecification{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="specification">The specification.</param>
        /// <returns></returns>
        TResult[] ToArray<TResult>(ISpecification<T, TResult> specification);

        /// <summary>
        /// Returns the first element satisfying a given <see cref="ISpecification{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="specification">The specification.</param>
        /// <returns></returns>
        TResult FirstOrDefault<TResult>(ISpecification<T, TResult> specification);

        /// <summary>
        /// Returns the number of elements satisfying a given <see cref="ISpecification{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="specification"></param>
        /// <returns></returns>
        int Count<TResult>(ISpecification<T, TResult> specification);
    }
}
