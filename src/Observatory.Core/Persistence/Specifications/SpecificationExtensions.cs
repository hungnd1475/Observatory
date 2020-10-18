using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Defines a set of extension methods for <see cref="ISpecification{T}"/>.
    /// </summary>
    public static class Specification
    {
        /// <summary>
        /// Creates a new instance of <see cref="ISpecification{T}"/> by relaying a given specificator function.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="specificator">The specificator function.</param>
        /// <returns></returns>
        public static ISpecification<T> Relay<T>(Func<IQueryable<T>, IQueryable<T>> specificator)
        {
            return new RelaySpecification<T>(specificator);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ISpecification{T}"/> that returns the exact source queryable.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <returns></returns>
        public static ISpecification<T> Identity<T>()
        {
            return new IdentitySpecification<T>();
        }

        /// <summary>
        /// Chains with a given instance of <see cref="ISpecification{T}"/> to produce a new instance of <see cref="ISpecification{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="previous">The previous <see cref="ISpecification{T}"/>.</param>
        /// <param name="next">The next <see cref="ISpecification{T}"/>.</param>
        /// <returns></returns>
        public static ISpecification<T> Chain<T>(this ISpecification<T> previous, ISpecification<T> next)
        {
            return new RelaySpecification<T>(q => next.Apply(previous.Apply(q)));
        }

        /// <summary>
        /// Chains with a given specificator function to produce a new instance of <see cref="ISpecification{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="previous">The previous <see cref="ISpecification{T}"/>.</param>
        /// <param name="specificator">The specificator function.</param>
        /// <returns></returns>
        public static ISpecification<T> Chain<T>(this ISpecification<T> previous, Func<IQueryable<T>, IQueryable<T>> specificator)
        {
            return new RelaySpecification<T>(q => specificator(previous.Apply(q)));
        }
    }
}
