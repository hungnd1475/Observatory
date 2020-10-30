using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Virtualization
{
    public class SpecificationVirtualizingSource<T> : IVirtualizingSource<T>
    {
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly Func<IProfileDataQuery, ISpecificationQueryable<T>> _sourceSelector;
        private readonly ISpecification<T> _sourceSpecification;

        public SpecificationVirtualizingSource(IProfileDataQueryFactory queryFactory,
            Func<IProfileDataQuery, ISpecificationQueryable<T>> sourceSelector,
            ISpecification<T> sourceSpecification)
        {
            _queryFactory = queryFactory;
            _sourceSelector = sourceSelector;
            _sourceSpecification = sourceSpecification;
        }

        public T[] GetItems(int startIndex, int maxNumberOfItems)
        {
            using var query = _queryFactory.Connect();
            var source = _sourceSelector.Invoke(query);
            var pagingSpecification = _sourceSpecification.Chain(q => q.Skip(startIndex).Take(maxNumberOfItems));
            return source.ToArray(pagingSpecification);
        }
        
        public int GetTotalCount()
        {
            using var query = _queryFactory.Connect();
            var source = _sourceSelector.Invoke(query);
            return source.Count(_sourceSpecification);
        }
    }
}
