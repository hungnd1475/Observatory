using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public static class VirtualizationExtensions
    {
        public static IVirtualizingSource<T> Virtualize<T>(this IQueryable<T> query, int pageSize)
        {
            return new QueryableVirtualizingSource<T>(query, pageSize);
        }
    }
}
