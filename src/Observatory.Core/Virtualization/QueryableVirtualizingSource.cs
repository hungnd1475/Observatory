using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Virtualization
{
    public class QueryableVirtualizingSource<T> : IVirtualizingSource<T>
    {
        private readonly IQueryable<T> _source;

        public int PageSize { get; }

        public QueryableVirtualizingSource(IQueryable<T> source, int pageSize)
        {
            _source = source;
            this.PageSize = pageSize;
        }

        public async Task<IReadOnlyList<T>> GetPageAsync(int pageNumber)
        {
            var start = this.PageSize * (pageNumber - 1);
            var result = await _source
                .Skip(start)
                .Take(this.PageSize)
                .ToListAsync();
            return result.AsReadOnly();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _source.CountAsync();
        }
    }
}
