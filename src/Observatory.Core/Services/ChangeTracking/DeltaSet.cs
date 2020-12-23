using Observatory.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Observatory.Core.Services.ChangeTracking
{
    public class DeltaSet<T> : ICollection<DeltaEntity<T>>
    {
        private readonly List<DeltaEntity<T>> _changes = new List<DeltaEntity<T>>();

        public DeltaSet() { }

        public DeltaSet(IEnumerable<DeltaEntity<T>> changes)
        {
            _changes.AddRange(changes);
        }

        public int Count => _changes.Count;

        public void Add(DeltaEntity<T> item) => _changes.Add(item);

        public void AddRange(IEnumerable<DeltaEntity<T>> items)
        {
            _changes.AddRange(items);
        }

        public bool Remove(DeltaEntity<T> item) => _changes.Remove(item);

        public void Clear() => _changes.Clear();

        public bool Contains(DeltaEntity<T> item) => _changes.Contains(item);

        public void CopyTo(DeltaEntity<T>[] array, int arrayIndex) => _changes.CopyTo(array, arrayIndex);

        public IEnumerator<DeltaEntity<T>> GetEnumerator() => _changes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool ICollection<DeltaEntity<T>>.IsReadOnly => false;
    }
}
