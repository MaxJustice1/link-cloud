﻿using LantanaGroup.Link.Submission.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LantanaGroup.Link.Submission.Domain
{
    public class TenantSubmissionDbContext : DbContext
    {
        public DbSet<TenantSubmissionConfigEntity> TenantSubmissionConfigs { get; set; }

        public TenantSubmissionDbContext(DbContextOptions<TenantSubmissionDbContext> options) : base(options)
        {
        }

        public DbSet<TenantSubmissionConfigEntity> TenantSubmissionConfigEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantSubmissionDbContext).Assembly);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TenantSubmissionConfigEntity>().HasKey(b => b.Id).IsClustered(false);

            modelBuilder.Entity<TenantSubmissionConfigEntity>()
                .Property(b => b.Id)
                .HasConversion(
                    v => new Guid(v),
                    v => v.ToString()
                );

            modelBuilder.Entity<TenantSubmissionConfigEntity>().OwnsMany(b => b.Methods, navBuilder =>
            {
                navBuilder.ToJson();
            });

        }
    }
}
