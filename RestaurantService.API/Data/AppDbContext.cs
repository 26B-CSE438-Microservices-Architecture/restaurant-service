using Microsoft.EntityFrameworkCore;
using RestaurantService.API.Entities;

namespace RestaurantService.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Restaurant ──
        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.Property(r => r.AddressText).HasMaxLength(500);
            entity.Property(r => r.LogoUrl).HasMaxLength(500);

            entity.Property(r => r.MinOrderAmount).HasPrecision(10, 2);
            entity.Property(r => r.DeliveryFee).HasPrecision(10, 2);

            entity.Property(r => r.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Indexes for search performance
            entity.HasIndex(r => r.Name);
            entity.HasIndex(r => new { r.Latitude, r.Longitude });
            entity.HasIndex(r => r.IsActive);

            entity.HasMany(r => r.MenuCategories)
                  .WithOne(mc => mc.Restaurant)
                  .HasForeignKey(mc => mc.RestaurantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MenuCategory ──
        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.HasKey(mc => mc.Id);
            entity.Property(mc => mc.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(mc => mc.Name).IsRequired().HasMaxLength(200);

            entity.HasIndex(mc => new { mc.RestaurantId, mc.DisplayOrder });

            entity.HasMany(mc => mc.Products)
                  .WithOne(p => p.Category)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Product ──
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.Price).HasPrecision(10, 2);

            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.IsAvailable);
        });
    }
}
