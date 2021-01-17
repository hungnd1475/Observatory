using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Services.ChangeTracking
{
    public struct DeltaEntity<T>
    {
        public DeltaState State { get; }
        public T Entity { get; }

        public DeltaEntity(DeltaState state, T entity)
        {
            State = state;
            Entity = entity;
        }
    }

    public struct DeltaEntity
    {
        public static DeltaEntity<T> Added<T>(T entity)
        {
            return new DeltaEntity<T>(DeltaState.Add, entity);
        }

        public static DeltaEntity<T> Updated<T>(T entity)
        {
            return new DeltaEntity<T>(DeltaState.Update, entity);
        }

        public static DeltaEntity<T> Removed<T>(T entity)
        {
            return new DeltaEntity<T>(DeltaState.Remove, entity);
        }
    }
}
