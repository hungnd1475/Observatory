using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Represents a type of <see cref="ISpecification{T}"/> that relays the actual specification to a delegate function.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class RelaySpecification<T> : ISpecification<T>
    {
        private readonly Func<IQueryable<T>, IQueryable<T>> _specificator;

        /// <summary>
        /// Constructs an instance of <see cref="RelaySpecification{T}"/>.
        /// </summary>
        /// <param name="specificator">The delegate function that defines the actual specification.</param>
        public RelaySpecification(Func<IQueryable<T>, IQueryable<T>> specificator)
        {
            _specificator = specificator;
        }

        public IQueryable<T> Apply(IQueryable<T> source)
        {
            return _specificator(source);
        }
    }
}
