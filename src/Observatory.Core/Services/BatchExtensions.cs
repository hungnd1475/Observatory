using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Services
{
    public static class BatchExtensions
    {
        public static IEnumerable<IEnumerable<T>> Paginate<T>(this IEnumerable<T> source, int pageSize)
        {
            var pageCount = source.Count() / pageSize;
            return Enumerable.Range(0, pageCount).Select(pageNumber => source.Skip(pageNumber * pageSize).Take(pageSize));
        }
    }
}
