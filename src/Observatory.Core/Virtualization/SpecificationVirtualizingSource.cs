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

        public int PageSize { get; }

        public SpecificationVirtualizingSource(IProfileDataQueryFactory queryFactory,
            Func<IProfileDataQuery, ISpecificationQueryable<T>> sourceSelector,
            ISpecification<T> sourceSpecification, int pageSize)
        {
            _queryFactory = queryFactory;
            _sourceSelector = sourceSelector;
            _sourceSpecification = sourceSpecification;
            this.PageSize = pageSize;
        }

        public async Task<IReadOnlyList<T>> GetPageAsync(int pageNumber)
        {
            using var query = _queryFactory.Connect();
            var source = _sourceSelector(query);
            var start = this.PageSize * (pageNumber - 1);
            var pagingSpecification = _sourceSpecification.Chain(q => q.Skip(start).Take(PageSize));
            return await source.ToListAsync(pagingSpecification);
        }

        public async Task<int> GetTotalCountAsync()
        {
            using var query = _queryFactory.Connect();
            var source = _sourceSelector(query);
            return await source.CountAsync(_sourceSpecification);
        }
    }
}
