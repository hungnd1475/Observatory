using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Observatory.Core.Persistence;
using Observatory.Providers.Exchange.Models;

namespace Observatory.Providers.Exchange.Persistence
{
    public class ExchangeProfileDataStore : ProfileDataStore
    {
        public delegate ExchangeProfileDataStore Factory(string path);

        public DbSet<FolderSynchronizationState> FolderSynchronizationStates { get; set; }
        public DbSet<MessageSynchronizationState> MessageSynchronizationStates { get; set; }

        public ExchangeProfileDataStore(string path, ILoggerFactory loggerFactory)
            : base(path, loggerFactory)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FolderSynchronizationState>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<FolderSynchronizationState>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<FolderSynchronizationState>()
                .Property(e => e.TimeLastSync)
                .HasConversion(new DateTimeOffsetToBytesConverter())
                .IsRequired(false);

            modelBuilder.Entity<MessageSynchronizationState>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<MessageSynchronizationState>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<MessageSynchronizationState>()
                .Property(e => e.TimeLastSync)
                .HasConversion(new DateTimeOffsetToBytesConverter())
                .IsRequired(false);
        }
    }
}
