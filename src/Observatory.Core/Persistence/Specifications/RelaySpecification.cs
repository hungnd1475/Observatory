using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Represents a type of <see cref="ISpecification{TSource, TTarget}"/> that relays the actual specification to a delegate function.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public class RelaySpecification<TSource, TTarget> : ISpecification<TSource, TTarget>
    {
        private readonly Func<IQueryable<TSource>, IQueryable<TTarget>> _specificator;

        /// <summary>
        /// Constructs an instance of <see cref="RelaySpecification{T}"/>.
        /// </summary>
        /// <param name="specificator">The delegate function that defines the actual specification.</param>
        public RelaySpecification(Func<IQueryable<TSource>, IQueryable<TTarget>> specificator)
        {
            _specificator = specificator;
        }

        public IQueryable<TTarget> Apply(IQueryable<TSource> source)
        {
            return _specificator(source);
        }
    }
}
