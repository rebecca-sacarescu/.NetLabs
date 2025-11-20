namespace ProductManagement;

public class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public ProductCategory Category { get; set; }

    public decimal Price { get; set; }

    public DateTime ReleaseDate { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public int StockQuantity { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}