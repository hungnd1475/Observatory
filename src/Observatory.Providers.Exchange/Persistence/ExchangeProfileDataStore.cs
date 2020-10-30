using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Observatory.Core.Persistence;
using Observatory.Core.Services;
using Observatory.Providers.Exchange.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;

namespace Observatory.Providers.Exchange.Persistence
{
    public class ExchangeProfileDataStore : ProfileDataStore
    {
        public delegate ExchangeProfileDataStore Factory(string path, bool trackChanges);

        public DbSet<FolderSynchronizationState> FolderSynchronizationStates { get; set; }
        public DbSet<MessageSynchronizationState> MessageSynchronizationStates { get; set; }

        public ExchangeProfileDataStore(string path, bool trackChanges, ILoggerFactory loggerFactory)
            : base(path, trackChanges, loggerFactory)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FolderSynchronizationState>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TimeLastSync)
                    .HasConversion(new DateTimeOffsetToBytesConverter())
                    .IsRequired(false);
            });

            modelBuilder.Entity<MessageSynchronizationState>(entity =>
            {
                entity.HasKey(e => e.FolderId);
                entity.Property(e => e.TimeLastSync)
                    .HasConversion(new DateTimeOffsetToBytesConverter())
                    .IsRequired(false);
            });                
        }

        public IReadOnlyCollection<DeltaEntity<T>> GetChanges<T>()
            where T: class
        {
            var changes = new List<DeltaEntity<T>>();
            foreach (var entry in ChangeTracker.Entries<T>()
                .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached))
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        changes.Add(DeltaEntity.Updated(entry.Entity));
                        break;
                    case EntityState.Added:
                        changes.Add(DeltaEntity.Added(entry.Entity));
                        break;
                    case EntityState.Deleted:
                        changes.Add(DeltaEntity.Removed(entry.Entity));
                        break;
                }
            }
            return changes.AsReadOnly();
        }
    }
}
