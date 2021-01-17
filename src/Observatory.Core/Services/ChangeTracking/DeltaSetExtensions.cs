using Microsoft.EntityFrameworkCore;
using Observatory.Core.Models;
using Observatory.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Services.ChangeTracking
{
    public static class DeltaSetExtensions
    {
        /// <summary>
        /// Gets all message changes for a given folder.
        /// </summary>
        /// <param name="changes">The delta set.</param>
        /// <param name="folderId">The folder's id to get changes for.</param>
        /// <returns>An instance of <see cref="IEnumerable{T}"/> containing all message changes for the given folder, lazily evaluated.</returns>
        public static IEnumerable<DeltaEntity<Message>> ForFolder(
            this DeltaSet<Message> changes,
            string folderId)
        {
            return changes.Where(c => c.Entity.FolderId == folderId);
        }

        public static bool AffectsFolder(this DeltaSet<Message> changes, string folderId)
        {
            return changes.Any(c => c.Entity.FolderId == folderId);
        }

        /// <summary>
        /// Converts changes for given type from a <see cref="DbContext"/> to a <see cref="DeltaSet{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity type to get changes for.</typeparam>
        /// <param name="store">The <see cref="DbContext"/> where changes are tracked.</param>
        /// <returns>An instance of <see cref="DeltaSet{T}"/> containing changes for the given type.</returns>
        public static DeltaSet<T> GetChanges<T>(this DbContext store)
            where T : class
        {
            return new DeltaSet<T>(store.ChangeTracker.Entries<T>()
                .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached)
                .Select(entry => entry.State switch
                {
                    EntityState.Modified => DeltaEntity.Updated(entry.Entity),
                    EntityState.Added => DeltaEntity.Added(entry.Entity),
                    EntityState.Deleted => DeltaEntity.Removed(entry.Entity),
                    _ => throw new NotSupportedException(),
                }));
        }
    }
}
