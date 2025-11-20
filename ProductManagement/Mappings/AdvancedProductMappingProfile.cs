using AutoMapper;
using ProductManagement.Features.Products;

namespace ProductManagement.Mappings;

public class AdvancedProductMappingProfile : Profile
{
    public AdvancedProductMappingProfile()
    {
        CreateMap<CreateProductProfileRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src =>
                DateTime.SpecifyKind(src.ReleaseDate, DateTimeKind.Utc)))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        
        CreateMap<Product, ProductProfileDto>()
            .ForMember(dest => dest.ImageUrl, opt =>
            {
                opt.Condition(src =>
                    src.Category == ProductCategory.Electronics ||
                    src.Category == ProductCategory.Clothing ||
                    src.Category == ProductCategory.Books);
                opt.MapFrom(src => src.ImageUrl);
            })

            .ForMember(dest => dest.Price,
                opt => opt.MapFrom(src =>
                    src.Category == ProductCategory.Home
                        ? src.Price * 0.9m
                        : src.Price))
            .ForMember(dest => dest.CategoryDisplayName,
                opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.FormattedPrice,
                opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.ProductAge,
                opt => opt.MapFrom<ProductAgeResolver>())
            .ForMember(dest => dest.BrandInitials,
                opt => opt.MapFrom<BrandInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus,
                opt => opt.MapFrom<AvailabilityStatusResolver>());
    }
}


public static class ProductMappingHelpers
{
    public static decimal GetEffectivePrice(Product product)
        => product.Category == ProductCategory.Home
            ? product.Price * 0.9m
            : product.Price;
}

public class CategoryDisplayResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            ProductCategory.Electronics => "Electronics & Technology",
            ProductCategory.Clothing    => "Clothing & Fashion",
            ProductCategory.Books       => "Books & Media",
            ProductCategory.Home        => "Home & Garden",
            _                           => "Uncategorized"
        };
    }
}

public class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var effectivePrice = ProductMappingHelpers.GetEffectivePrice(source);
        return effectivePrice.ToString("C2");
    }
}

public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var now = DateTime.UtcNow;
        var age = now.Date - source.ReleaseDate.Date;
        var days = age.TotalDays;

        if (days < 30)
        {
            return "New Release";
        }

        if (days < 365)
        {
            var months = (int)(days / 30);
            if (months <= 0) months = 1;
            return $"{months} months old";
        }

        if (days < 1825) // < 5 years
        {
            var years = (int)(days / 365);
            if (years <= 0) years = 1;
            return $"{years} years old";
        }

        if (Math.Abs(days - 1825) < 0.1)
        {
            return "Classic";
        }

        return "Classic";
    }
}

public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Brand))
        {
            return "?";
        }

        var parts = source.Brand
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            return parts[0][0].ToString().ToUpperInvariant();
        }

        var first = parts.First()[0];
        var last = parts.Last()[0];

        return $"{char.ToUpperInvariant(first)}{char.ToUpperInvariant(last)}";
    }
}

public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
        {
            return "Out of Stock";
        }

        if (source.StockQuantity <= 0)
        {
            return "Unavailable";
        }

        if (source.StockQuantity == 1)
        {
            return "Last Item";
        }

        if (source.StockQuantity <= 5)
        {
            return "Limited Stock";
        }

        return "In Stock";
    }
}
