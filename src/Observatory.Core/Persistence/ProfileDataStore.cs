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
        public DbSet<Message> Messages { get; set; }

        ISpecificationQueryable<MailFolder> IProfileDataQuery.Folders => new EFSpecificationQueryable<MailFolder>(Folders);

        ISpecificationQueryable<MessageSummary> IProfileDataQuery.MessageSummaries => new EFSpecificationQueryable<MessageSummary>(Set<MessageSummary>());

        ISpecificationQueryable<MessageDetail> IProfileDataQuery.MessageDetails => new EFSpecificationQueryable<MessageDetail>(Set<MessageDetail>());

        public ProfileDataStore(string path, bool trackChanges, ILoggerFactory loggerFactory)
            : base(BuildOptions(path, trackChanges, loggerFactory))
        {
            ChangeTracker.AutoDetectChangesEnabled = trackChanges;
        }

        private static DbContextOptions<ProfileDataStore> BuildOptions(string path, bool trackChanges, ILoggerFactory loggerFactory)
        {
            return new DbContextOptionsBuilder<ProfileDataStore>()
                  .UseSqlite($@"Filename={path}")
                  //.UseLoggerFactory(loggerFactory)
                  .UseQueryTrackingBehavior(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
#if DEBUG
                  //.EnableSensitiveDataLogging()
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

            modelBuilder.Entity<Message>(entity => 
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.ReceivedDateTime)
                    .HasConversion(new DateTimeOffsetToBytesConverter());
                entity.Property(m => m.Sender)
                    .HasConversion(new ObjectToJsonConverter<Recipient>());
                entity.Property(m => m.ToRecipients)
                    .HasConversion(recipientsJsonConverter);
                entity.Property(m => m.CcRecipients)
                    .HasConversion(recipientsJsonConverter);
                entity.HasIndex(m => m.ReceivedDateTime);
                entity.HasIndex(m => m.FolderId);
            });

            modelBuilder.Entity<MessageSummary>(entity =>
            {
                entity.HasNoKey();
                entity.Property(m => m.ReceivedDateTime)
                    .HasConversion(new DateTimeOffsetToBytesConverter());
                entity.Property(m => m.Sender)
                    .HasConversion(new ObjectToJsonConverter<Recipient>());
                entity.Property(m => m.ToRecipients)
                    .HasConversion(recipientsJsonConverter);
                entity.Property(m => m.CcRecipients)
                    .HasConversion(recipientsJsonConverter);
                entity.ToQuery(() => Messages.Select(m => new MessageSummary()
                {
                    Id = m.Id,
                    BodyPreview = m.BodyPreview,
                    CcRecipients = m.CcRecipients,
                    FolderId = m.FolderId,
                    HasAttachments = m.HasAttachments,
                    Importance = m.Importance,
                    IsDraft = m.IsDraft,
                    IsFlagged = m.IsFlagged,
                    IsRead = m.IsRead,
                    ReceivedDateTime = m.ReceivedDateTime,
                    Sender = m.Sender,
                    Subject = m.Subject,
                    ThreadId = m.ThreadId,
                    ThreadPosition = m.ThreadPosition,
                    ToRecipients = m.ToRecipients
                }));
            });

            modelBuilder.Entity<MessageDetail>(entity =>
            {
                entity.HasNoKey();
                entity.ToQuery(() => Messages.Select(m => new MessageDetail()
                {
                    Id = m.Id,
                    Body = m.Body,
                    BodyType = m.BodyType
                }));
            });                
        }
    }
}
