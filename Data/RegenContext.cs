using Microsoft.EntityFrameworkCore;
using Regen.Models;

namespace Regen.Data;

public class RegenContext : DbContext
{
    public RegenContext(DbContextOptions<RegenContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ScheduledBatch> ScheduledBatches => Set<ScheduledBatch>();
    public DbSet<ScheduledEntry> ScheduledEntries => Set<ScheduledEntry>();
    public DbSet<SavedConfiguration> SavedConfigurations { get; set; }
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<FreeCreditClaim> FreeCreditClaims => Set<FreeCreditClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.HasIndex(u => u.GoogleId).IsUnique();

            entity.HasIndex(u => u.StripeCustomerId).IsUnique();

            entity.Property(u => u.GoogleId)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(u => u.Email)
                  .HasMaxLength(320)
                  .IsRequired();

            entity.Property(u => u.Name)
                  .HasMaxLength(255)
                  .IsRequired();
        });

        modelBuilder.Entity<ScheduledEntry>(entity =>
        {
            entity.HasOne(e => e.Batch)
                .WithMany(b => b.Entries)
                .HasForeignKey(e => e.BatchId);

            entity.HasIndex(e => new
            {
                e.Status,
                e.ScheduledAt
            });
        });

        modelBuilder.Entity<SavedConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Url)
                .IsRequired();

            entity.Property(e => e.Config)
                .HasColumnType("jsonb")
                .IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.OperationKey)
                .IsUnique();

            entity.HasIndex(t => new
            {
                t.UserId,
                t.CreatedAt
            });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}