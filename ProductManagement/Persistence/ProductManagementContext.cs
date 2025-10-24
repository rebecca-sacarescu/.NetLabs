using Microsoft.EntityFrameworkCore;

namespace ProductManagement.Persistence;

public class ProductManagementContext(DbContextOptions<ProductManagementContext> options) : DbContext(options)
{
    public DbSet<Product> Product { get; set; }
}