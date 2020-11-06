using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Virtualization
{
    public class FakeVirtualizingSource<T> : IVirtualizingSource<T>, IEnableLogger
    {
        private readonly T _placeholder;
        private readonly int _totalCount;

        public FakeVirtualizingSource(T placeholder, int totalCount)
        {
            _placeholder = placeholder;
            _totalCount = totalCount;
        }

        public T[] GetItems(int startIndex, int maxNumberOfItems)
        {
            Task.Delay(1000);
            this.Log().Debug($"{maxNumberOfItems} items retrieved starting from {startIndex}.");
            return Enumerable.Repeat(_placeholder, maxNumberOfItems).ToArray();
        }

        public int GetTotalCount()
        {
            return _totalCount;
        }

        public int IndexOf(T entity)
        {
            return 0;
        }
    }
}
