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
    }
}