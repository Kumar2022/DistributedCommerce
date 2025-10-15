using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        
        // Use a connection string for design-time only
        optionsBuilder.UseNpgsql("Host=localhost;Database=inventory_db;Username=postgres;Password=postgres");
        
        return new InventoryDbContext(optionsBuilder.Options);
    }
}
