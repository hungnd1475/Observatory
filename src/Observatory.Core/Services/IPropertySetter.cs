using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Services
{
    public interface IPropertySetter<TEntity>
    {
        void Set<TProperty>(TEntity entity, TProperty value);
    }
}
