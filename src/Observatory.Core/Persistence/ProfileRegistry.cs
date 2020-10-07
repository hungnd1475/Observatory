using Microsoft.EntityFrameworkCore;
using Observatory.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Persistence
{
    public class ProfileRegistry : DbContext
    {
        public DbSet<ProfileRegister> Profiles { get; set; }

        public ProfileRegistry(string path)
            : base(new DbContextOptionsBuilder<ProfileRegistry>()
                  .UseSqlite($@"Filename={path}")
                  .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                  .Options)
        {
            this.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProfileRegister>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<ProfileRegister>()
                .HasIndex(p => p.EmailAddress)
                .IsUnique(true);
        }
    }
}
