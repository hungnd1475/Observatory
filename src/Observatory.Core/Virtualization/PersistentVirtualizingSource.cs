using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a specialized <see cref="IVirtualizingSource{T}"/> that queries messages in a mail folder.
    /// </summary>
    public class PersistentVirtualizingSource<TEntity, TKey> : IVirtualizingSource<TEntity, TKey>
        where TEntity : IVirtualizableSource<TKey>
    {
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly ISpecification<TEntity, TEntity> _itemSpecification;
        private readonly Func<TEntity, ISpecification<TEntity, TEntity>> _indexSpecification;

        /// <summary>
        /// Constructs in an instance of <see cref="PersistentVirtualizingSource{TEntity, TKey}"/>.
        /// </summary>
        /// <param name="queryFactory">The query factory.</param>
        /// <param name="itemSpecification">The specification specifies how to retrieve the items.</param>
        /// <param name="indexSpecification">The specification specifies how to get the index of a given item.</param>
        public PersistentVirtualizingSource(
            IProfileDataQueryFactory queryFactory,
            ISpecification<TEntity, TEntity> itemSpecification,
            Func<TEntity, ISpecification<TEntity, TEntity>> indexSpecification)
        {
            _queryFactory = queryFactory;
            _itemSpecification = itemSpecification;
            _indexSpecification = indexSpecification;
        }

        public List<TKey> GetAllKeys()
        {
            using var query = _queryFactory.Connect();
            return query.ForType<TEntity>().ToList(
                _itemSpecification.Chain(q => q.Select(e => e.Id)));
        }

        public TEntity[] GetItems(int startIndex, int maxNumberOfItems)
        {
            using var query = _queryFactory.Connect();
            return query.ForType<TEntity>().ToArray(
                _itemSpecification.Chain(q => q.Skip(startIndex)
                    .Take(maxNumberOfItems)));
        }

        public int GetTotalCount()
        {
            using var query = _queryFactory.Connect();
            return query.ForType<TEntity>().Count(_itemSpecification);
        }

        public int IndexOf(TEntity entity)
        {
            using var query = _queryFactory.Connect();
            return query.ForType<TEntity>().Count(_indexSpecification(entity));
        }
    }
}
