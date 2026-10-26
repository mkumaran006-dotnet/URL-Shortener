using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;

namespace UrlShortener.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ShortUrl>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ShortCode).IsUnique();
            e.Property(x => x.ShortCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.OriginalUrl).HasMaxLength(2048).IsRequired();
            e.Property(x => x.CustomAlias).HasMaxLength(50);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.CreatedBy).HasMaxLength(100);
            e.HasMany(x => x.ClickEvents)
             .WithOne(x => x.ShortUrl)
             .HasForeignKey(x => x.ShortUrlId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ClickEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShortUrlId, x.ClickedAt });
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.Referrer).HasMaxLength(2048);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
        });
    }
}
