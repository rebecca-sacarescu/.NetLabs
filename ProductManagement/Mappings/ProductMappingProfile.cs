using AutoMapper;
using ProductManagement.Features.Products;

namespace ProductManagement.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<CreateProductProfileRequest, Product>()
            .ConstructUsing(src => new Product
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = src.Name,
                Brand = src.Brand,
                SKU = src.SKU,
                Category = src.Category,
                Price = src.Price,
                ReleaseDate = src.ReleaseDate,
                ImageUrl = src.ImageUrl,
                StockQuantity = src.StockQuantity
            });
    }
}