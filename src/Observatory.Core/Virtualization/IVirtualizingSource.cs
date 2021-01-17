using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Defines a contract for querying a data source by a <see cref="VirtualizingCache{TEntity, TKey}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of items retrieved by the source.</typeparam>
    /// <typeparam name="TKey">THe type of item's keys.</typeparam>
    public interface IVirtualizingSource<TEntity, TKey>
    {
        /// <summary>
        /// Returns the total number of items in the source.
        /// </summary>
        /// <returns></returns>
        int GetTotalCount();

        /// <summary>
        /// Returns the zero-based position of a given item in the source.
        /// </summary>
        /// <param name="entity">The item to get the position.</param>
        /// <returns></returns>
        int IndexOf(TEntity entity);

        /// <summary>
        /// Returns a range of items starting at a given position.
        /// </summary>
        /// <param name="startIndex">The starting position.</param>
        /// <param name="maxNumberOfItems">The maximum number of items to retrieved.</param>
        /// <returns></returns>
        TEntity[] GetItems(int startIndex, int maxNumberOfItems);

        /// <summary>
        /// Gets all keys from source.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<TKey> GetAllKeys();
    }
}
