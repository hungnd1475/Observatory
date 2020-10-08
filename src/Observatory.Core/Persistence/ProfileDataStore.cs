using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Observatory.Core.Models;
using Observatory.Core.Persistence.Conversion;
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

        IQueryable<MailFolder> IProfileDataQuery.Folders => Folders.AsQueryable();

        IQueryable<MessageSummary> IProfileDataQuery.MessageSummaries => MessageSummaries.AsQueryable();

        IQueryable<MessageDetail> IProfileDataQuery.MessageDetails => MessageDetails.AsQueryable();

        public ProfileDataStore(string path, ILoggerFactory loggerFactory)
            : base(new DbContextOptionsBuilder<ProfileDataStore>()
                  .UseSqlite($@"Filename={path}")
                  .UseLoggerFactory(loggerFactory)
                  .Options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
                .HasConversion(new ObjectToJsonConverter<List<Recipient>>());
            modelBuilder.Entity<MessageSummary>()
                .Property(m => m.CcRecipients)
                .HasConversion(new ObjectToJsonConverter<List<Recipient>>());
            modelBuilder.Entity<MessageSummary>()
                .HasIndex(m => m.ReceivedDateTime);

            modelBuilder.Entity<MessageDetail>()
                .HasKey(m => m.Id);
        }
    }
}
