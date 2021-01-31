using Observatory.Core.Services.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Virtualization
{
    public class VirtualizingCacheUpdater<T>
    {
        private readonly DeltaSet<T> _deltaSet = new DeltaSet<T>();

        public IReadOnlyList<DeltaEntity<T>> Changes => _deltaSet;

        public void Add(T entity)
        {
            _deltaSet.Add(DeltaEntity.Added(entity));
        }

        public void Update(T entity)
        {
            _deltaSet.Add(DeltaEntity.Updated(entity));
        }

        public void Remove(T entity)
        {
            _deltaSet.Add(DeltaEntity.Removed(entity));
        }
    }
}
