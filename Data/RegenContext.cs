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
    public DbSet<CreditPurchase> CreditPurchases => Set<CreditPurchase>();
    public DbSet<CreditPackage> CreditPackages => Set<CreditPackage>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents { get; set; }
    public DbSet<Notification> Notifications { get; set; }

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

        modelBuilder.Entity<FreeCreditClaim>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.GoogleIdHash)
                .IsUnique();
        });

        modelBuilder.Entity<CreditPurchase>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.StripeSessionId)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<CreditPackage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Credits)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(x => x.StripePriceId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => x.StripePriceId)
                .IsUnique();

            entity.HasIndex(x => x.IsActive);

            entity.HasData(
                new CreditPackage
                {
                    Id = 1,
                    Name = "Starter",
                    Credits = 30,
                    Amount = 3.00m,
                    Currency = "MYR",
                    StripePriceId = "price_1UeFhQHaga1NVVh0rM4EpQGi",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc)
                },
                new CreditPackage
                {
                    Id = 2,
                    Name = "Basic",
                    Credits = 100,
                    Amount = 10.00m,
                    Currency = "MYR",
                    StripePriceId = "price_1UeFizHaga1NVVh0OhHp23fW",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc)
                },
                new CreditPackage
                {
                    Id = 3,
                    Name = "Pro",
                    Credits = 500,
                    Amount = 45.00m,
                    Currency = "MYR",
                    StripePriceId = "price_1UeFjZHaga1NVVh0uiihyUB4",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc)
                },
                new CreditPackage
                {
                    Id = 4,
                    Name = "Premium",
                    Credits = 1000,
                    Amount = 80.00m,
                    Currency = "MYR",
                    StripePriceId = "price_1UeFk3Haga1NVVh0T0bCVxQC",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
    }
}