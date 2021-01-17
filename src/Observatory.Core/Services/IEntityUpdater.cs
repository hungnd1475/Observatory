using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Services
{
    public interface IEntityUpdater<TEntity>
    {
        IEntityUpdater<TEntity> Set<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, TProperty value);
        Task ExecuteAsync();
    }

    public class RelayEntityUpdater<TEntity> : IEntityUpdater<TEntity>
        where TEntity : class, new()
    {
        private readonly TEntity _entity;
        private readonly Func<TEntity, Task> _executeCallback;

        public RelayEntityUpdater(Func<TEntity, Task> executeCallback)
        {
            _entity = new TEntity();
            _executeCallback = executeCallback;
        }

        public async Task ExecuteAsync()
        {
            await _executeCallback(_entity);
        }

        public IEntityUpdater<TEntity> Set<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, TProperty value)
        {
            propertyExpression.GetPropertyAccess().SetValue(_entity, value);
            return this;
        }
    }
}
