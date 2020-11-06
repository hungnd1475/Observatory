using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Virtualization
{
    public interface IVirtualizingSource<T>
    {
        int GetTotalCount();
        int IndexOf(T entity);
        T[] GetItems(int startIndex, int maxNumberOfItems);
    }
}
