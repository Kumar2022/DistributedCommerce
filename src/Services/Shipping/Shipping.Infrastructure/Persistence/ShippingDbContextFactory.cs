using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shipping.Infrastructure.Persistence;

public class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShippingDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=shipping_db;Username=postgres;Password=postgres");
        return new ShippingDbContext(optionsBuilder.Options);
    }
}
