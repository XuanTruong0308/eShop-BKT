using Microsoft.EntityFrameworkCore;

namespace Discount.API;

public class LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : DbContext(options)
{
    public DbSet<CustomerLoyalty> CustomerLoyalties => Set<CustomerLoyalty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerLoyalty>().HasKey(x => x.Id);
    }
}
