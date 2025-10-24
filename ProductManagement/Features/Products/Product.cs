namespace ProductManagement;

public class Product
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public string SKU { get; set; }
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int StockQuantity { get; set; } = 0;
    public bool IsAvailable => StockQuantity > 0;
}