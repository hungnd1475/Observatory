using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Virtualization
{
    public interface IVirtualizingSource<T>
    {
        int PageSize { get; }
        Task<int> GetTotalCountAsync();
        Task<IReadOnlyList<T>> GetPageAsync(int pageNumber);
    }
}
