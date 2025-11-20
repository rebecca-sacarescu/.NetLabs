using Microsoft.EntityFrameworkCore;

namespace ProductManagement.Persistence;

public class ProductManagementContext : DbContext
{
    public ProductManagementContext(DbContextOptions<ProductManagementContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Product { get; set; } = null!;
}