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
    {
        private readonly List<(LambdaExpression, object)> _setExpressions = new List<(LambdaExpression, object)>();
        private readonly Func<List<(LambdaExpression, object)>, Task> _executeCallback;

        public RelayEntityUpdater(Func<List<(LambdaExpression, object)>, Task> executeCallback)
        {
            _executeCallback = executeCallback;
        }

        public async Task ExecuteAsync()
        {
            await _executeCallback(_setExpressions);
        }

        public IEntityUpdater<TEntity> Set<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, TProperty value)
        {
            _setExpressions.Add((propertyExpression, value));
            return this;
        }
    }
}
