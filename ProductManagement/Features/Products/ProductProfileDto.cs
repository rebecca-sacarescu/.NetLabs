namespace ProductManagement;

public class ProductProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public string SKU { get; set; }
    public string CategoryDisplayName { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string FormattedPrice => Price.ToString("C");
    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public string ProductAge { get; set; }
    public string BrandInitials { get; set; }
    public string AvailabilityStatus => IsAvailable ? "In Stock" : "Out of Stock";
}