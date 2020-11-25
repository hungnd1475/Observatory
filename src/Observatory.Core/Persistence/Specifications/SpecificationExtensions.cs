using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Defines a set of extension methods for <see cref="ISpecification{T}"/>.
    /// </summary>
    public static class Specification
    {
        /// <summary>
        /// Creates a new instance of <see cref="ISpecification{TSource, TTarget}"/> by relaying to a given specificator function.
        /// </summary>
        /// <param name="specificator">The specificator function.</param>
        /// <returns></returns>
        public static ISpecification<TSource, TTarget> Relay<TSource, TTarget>(Func<IQueryable<TSource>, IQueryable<TTarget>> specificator)
        {
            return new RelaySpecification<TSource, TTarget>(specificator);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ISpecification{TSource, TTarget}"/> by relaying to a given specificator function.
        /// </summary>
        /// <param name="specificator">The specificator function.</param>
        /// <returns></returns>
        public static ISpecification<T, T> Relay<T>(Func<IQueryable<T>, IQueryable<T>> specificator) => Relay<T, T>(specificator);

        /// <summary>
        /// Creates a new instance of <see cref="ISpecification{TSource, TTarget}"/> that returns the exact source queryable.
        /// </summary>
        /// <returns></returns>
        public static ISpecification<T, T> Identity<T>()
        {
            return new IdentitySpecification<T>();
        }

        /// <summary>
        /// Chains with a given instance of <see cref="ISpecification{TSource, TTarget}"/> to produce a new 
        /// instance of <see cref="ISpecification{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="previous">The previous specification to be chained.</param>
        /// <param name="next">The next specification to chain.</param>
        /// <returns></returns>
        public static ISpecification<TSource, TTarget> Chain<TSource, TIntermediate, TTarget>(this ISpecification<TSource, TIntermediate> previous,
            ISpecification<TIntermediate, TTarget> next)
        {
            return new RelaySpecification<TSource, TTarget>(q => next.Apply(previous.Apply(q)));
        }

        /// <summary>
        /// Chains with a given specificator function to produce a new instance of <see cref="ISpecification{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="previous">The previous specification to be chained.</param>
        /// <param name="specificator">The specificator function.</param>
        /// <returns></returns>
        public static ISpecification<TSource, TTarget> Chain<TSource, TIntermediate, TTarget>(this ISpecification<TSource, TIntermediate> previous,
            Func<IQueryable<TIntermediate>, IQueryable<TTarget>> specificator)
        {
            return new RelaySpecification<TSource, TTarget>(q => specificator(previous.Apply(q)));
        }

        /// <summary>
        /// Returns a list of elements with an identity specification.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="queryable">The queryable.</param>
        /// <returns></returns>
        public static List<T> ToList<T>(this ISpecificationQueryable<T> queryable)
        {
            return queryable.ToList(Identity<T>());
        }

        /// <summary>
        /// Returns a list of elements filtered by a given predicate.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="queryable">The queryable.</param>
        /// <param name="predicate">The predicate to filter the elements.</param>
        /// <returns></returns>
        public static List<T> ToList<T>(this ISpecificationQueryable<T> queryable,
            Expression<Func<T, bool>> predicate)
        {
            return queryable.ToList(Relay<T, T>(q => q.Where(predicate)));
        }

        /// <summary>
        /// Returns the number of all elements.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="queryable">The queryable.</param>
        /// <returns></returns>
        public static int Count<T>(this ISpecificationQueryable<T> queryable)
        {
            return queryable.Count(Identity<T>());
        }

        /// <summary>
        /// Returns the number of elements satisfying a given predicate.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="queryable">The queryable.</param>
        /// <param name="predicate">The predicate to filter the elements.</param>
        /// <returns></returns>
        public static int Count<T>(this ISpecificationQueryable<T> queryable,
            Expression<Func<T, bool>> predicate)
        {
            return queryable.Count(Relay<T, T>(q => q.Where(predicate)));
        }

        /// <summary>
        /// Returns the first element.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="queryable">The queryable.</param>
        /// <returns></returns>
        public static T FirstOrDefault<T>(this ISpecificationQueryable<T> queryable)
        {
            return queryable.FirstOrDefault(Identity<T>());
        }

        /// <summary>
        /// Returns the first element satisfying a given predicate.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="queryable">The queryable.</param>
        /// <param name="predicate">The predicate to filter the elements.</param>
        /// <returns></returns>
        public static T FirstOrDefault<T>(this ISpecificationQueryable<T> queryable,
            Expression<Func<T, bool>> predicate)
        {
            return queryable.FirstOrDefault(Relay<T, T>(q => q.Where(predicate)));
        }
    }
}
