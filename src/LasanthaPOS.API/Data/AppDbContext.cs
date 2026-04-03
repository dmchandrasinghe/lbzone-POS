using Microsoft.EntityFrameworkCore;
using LasanthaPOS.API.Models;

namespace LasanthaPOS.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ItemCode)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.LoyaltyCardId)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Sale>()
            .HasIndex(s => s.ReceiptNumber)
            .IsUnique();

        // Ignore computed properties (not mapped to columns)
        modelBuilder.Entity<Product>()
            .Ignore(p => p.TotalCost)
            .Ignore(p => p.ProfitMargin);
    }
}
