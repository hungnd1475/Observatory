using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Observatory.Core.Models;
using Observatory.Core.Persistence.Conversion;
using Observatory.Core.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence
{
    public class ProfileDataStore : DbContext, IProfileDataQuery
    {
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<MailFolder> Folders { get; set; }
        public DbSet<MessageSummary> MessageSummaries { get; set; }
        public DbSet<MessageDetail> MessageDetails { get; set; }

        ISpecificationQueryable<MailFolder> IProfileDataQuery.Folders => new EFSpecificationQueryable<MailFolder>(Folders);

        ISpecificationQueryable<MessageSummary> IProfileDataQuery.MessageSummaries => new EFSpecificationQueryable<MessageSummary>(MessageSummaries);

        ISpecificationQueryable<MessageDetail> IProfileDataQuery.MessageDetails => new EFSpecificationQueryable<MessageDetail>(MessageDetails);

        public ProfileDataStore(string path, bool trackChanges, ILoggerFactory loggerFactory)
            : base(BuildOptions(path, trackChanges, loggerFactory))
        {
            ChangeTracker.AutoDetectChangesEnabled = trackChanges;
        }

        private static DbContextOptions<ProfileDataStore> BuildOptions(string path, bool trackChanges, ILoggerFactory loggerFactory)
        {
            return new DbContextOptionsBuilder<ProfileDataStore>()
                  .UseSqlite($@"Filename={path}")
                  .UseLoggerFactory(loggerFactory)
                  .UseQueryTrackingBehavior(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
#if DEBUG
                  .EnableSensitiveDataLogging()
#endif
                  .Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var recipientsJsonConverter = new ObjectToJsonConverter<List<Recipient>>();

            modelBuilder.Entity<Profile>()
                .HasKey(p => p.EmailAddress);

            modelBuilder.Entity<MailFolder>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<MessageSummary>()
                .HasKey(m => m.Id);
            modelBuilder.Entity<MessageSummary>()
                .Property(m => m.ReceivedDateTime)
                .HasConversion(new DateTimeOffsetToBytesConverter());
            modelBuilder.Entity<MessageSummary>()
                .Property(m => m.Sender)
                .HasConversion(new ObjectToJsonConverter<Recipient>());
            modelBuilder.Entity<MessageSummary>()
                .Property(m => m.ToRecipients)
                .HasConversion(recipientsJsonConverter);
            modelBuilder.Entity<MessageSummary>()
                .Property(m => m.CcRecipients)
                .HasConversion(recipientsJsonConverter);
            modelBuilder.Entity<MessageSummary>()
                .HasIndex(m => m.ReceivedDateTime);
            modelBuilder.Entity<MessageSummary>()
                .HasOne(m => m.Detail)
                .WithOne()
                .HasForeignKey<MessageDetail>(m => m.Id)
                .IsRequired(true);

            modelBuilder.Entity<MessageDetail>()
                .HasKey(m => m.Id);
        }
    }
}
