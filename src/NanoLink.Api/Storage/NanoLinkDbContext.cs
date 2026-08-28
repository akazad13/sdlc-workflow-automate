using Microsoft.EntityFrameworkCore;
using NanoLink.Api.Models;

namespace NanoLink.Api.Storage;

public class NanoLinkDbContext : DbContext
{
    public NanoLinkDbContext(DbContextOptions<NanoLinkDbContext> options) : base(options)
    {
    }

    public DbSet<UrlRecord> Urls => Set<UrlRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UrlRecord>(entity =>
        {
            entity.HasKey(e => e.ShortCode);
            entity.Property(e => e.ShortCode).HasMaxLength(32).IsRequired();
            entity.Property(e => e.TargetUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasIndex(e => e.ExpiresAtUtc);
            entity.HasIndex(e => e.CreatedAtUtc);
        });
    }
}
