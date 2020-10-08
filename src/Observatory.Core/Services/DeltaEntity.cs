using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Services
{
    public struct DeltaEntity<T>
    {
        public DeltaState State { get; }
        public string Id { get; }
        public T Entity { get; }

        public DeltaEntity(DeltaState state, string id, T entity)
        {
            State = state;
            Id = id;
            Entity = entity;
        }
    }

    public struct DeltaEntity
    {
        public static DeltaEntity<T> Added<T>(string id, T entity)
        {
            return new DeltaEntity<T>(DeltaState.Add, id, entity);
        }

        public static DeltaEntity<T> Updated<T>(string id, T entity)
        {
            return new DeltaEntity<T>(DeltaState.Update, id, entity);
        }

        public static DeltaEntity<T> Removed<T>(string id)
        {
            return new DeltaEntity<T>(DeltaState.Remove, id, default);
        }
    }
}
