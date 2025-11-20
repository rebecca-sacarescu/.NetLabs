using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManagement.Features.Products;
using ProductManagement.Persistence;

namespace ProductManagement.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    private static readonly string[] InappropriateWords =
    [
        "ai-generated", "fake", "scam", "fraud", "illegal", "banned",
    ];

    private static readonly string[] HomeRestrictedWords =
    [
        "weapon", "explosive", "toxic"
    ];

    private static readonly string[] TechnologyKeywords =
    [
        "tv", "smart", "laptop", "phone", "tablet", "camera",
        "headphones", "speaker", "console", "monitor", "pc"
    ];

    public CreateProductProfileValidator(
        ProductManagementContext context,
        ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        // Name
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(1).MaximumLength(200)
            .WithMessage("Product name must be between 1 and 200 characters.")
            .Must(BeValidName)
            .WithMessage("Product name contains inappropriate content.")
            .MustAsync(BeUniqueName)
            .WithMessage("A product with the same name already exists for this brand.");

        // Brand
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MinimumLength(2).MaximumLength(100)
            .WithMessage("Brand must be between 2 and 100 characters.")
            .Must(BeValidBrandName)
            .WithMessage("Brand contains invalid characters.");

        // SKU
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .Must(BeValidSKU)
            .WithMessage("SKU must be 5-20 characters long and contain only letters, numbers, and hyphens.")
            .MustAsync(BeUniqueSKU)
            .WithMessage("SKU must be unique.");

        // Category
        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Category must be a valid value.");

        // Price
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThan(10_000).WithMessage("Price must be less than 10,000.");

        // Release date
        RuleFor(x => x.ReleaseDate)
            .Must(d => d >= new DateTime(1900, 1, 1))
            .WithMessage("Release date cannot be before year 1900.")
            .Must(d => d <= DateTime.UtcNow.Date)
            .WithMessage("Release date cannot be in the future.");

        // Stock
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(100_000).WithMessage("Stock quantity cannot exceed 100,000.");

        // Image URL
        When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl!)
                .Must(BeValidImageUrl)
                .WithMessage("ImageUrl must be a valid HTTP/HTTPS image URL.");
        });
        
        RuleFor(x => x)
            .MustAsync(PassBusinessRules)
            .WithMessage("Product does not satisfy business rules.");


        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(50.00m)
                .WithMessage("Electronics products must have a minimum price of $50.00.");

            RuleFor(x => x.Name)
                .Must(ContainTechnologyKeywords)
                .WithMessage("Electronics products must contain at least one technology keyword in the name.");

            RuleFor(x => x.ReleaseDate)
                .Must(d => d >= DateTime.UtcNow.AddYears(-5))
                .WithMessage("Electronics products must be released within the last 5 years.");
        });

        // Home
        When(x => x.Category == ProductCategory.Home, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(200.00m)
                .WithMessage("Home products must not exceed $200.00.");

            RuleFor(x => x.Name)
                .Must(BeAppropriateForHome)
                .WithMessage("Home product name contains inappropriate content.");
        });
        
        When(x => x.Category == ProductCategory.Clothing, () =>
        {
            RuleFor(x => x.Brand)
                .MinimumLength(3)
                .WithMessage("Clothing brand name must be at least 3 characters.");
        });
        
        RuleFor(x => x.StockQuantity)
            .Must((request, stock) => request.Price <= 100m || stock <= 20)
            .WithMessage("Expensive products (price > 100) must have stock quantity ≤ 20 units.");
        
        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.ReleaseDate)
                .Must(d => d >= DateTime.UtcNow.AddYears(-5))
                .WithMessage("Electronics must be released within the last 5 years.");
        });
    }
    

    private bool BeValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        var lowered = name.ToLowerInvariant();
        var invalid = InappropriateWords.Any(w => lowered.Contains(w));

        if (invalid)
        {
            _logger.LogWarning("Product name failed inappropriate content check: {Name}", name);
        }

        return !invalid;
    }

    private async Task<bool> BeUniqueName(CreateProductProfileRequest request, string name, CancellationToken cancellationToken)
    {
        var exists = await _context.Product.AnyAsync(
            p => p.Name == name && p.Brand == request.Brand,
            cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Name uniqueness check failed for Name {Name} and Brand {Brand}", name, request.Brand);
        }

        return !exists;
    }

    private bool BeValidBrandName(string brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return true;

        var regex = new Regex(@"^[\p{L}\p{N}\s\-\.'’]+$");
        var isValid = regex.IsMatch(brand);

        if (!isValid)
        {
            _logger.LogWarning("Brand name validation failed for value {Brand}", brand);
        }

        return isValid;
    }

    private bool BeValidSKU(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return true;

        sku = sku.Replace(" ", string.Empty);

        if (sku.Length < 5 || sku.Length > 20)
            return false;

        return Regex.IsMatch(sku, @"^[A-Za-z0-9\-]+$");
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken cancellationToken)
    {
        var exists = await _context.Product.AnyAsync(p => p.SKU == sku, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("SKU uniqueness check failed for {SKU}", sku);
        }

        return !exists;
    }

    private bool BeValidImageUrl(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var lower = uri.AbsolutePath.ToLowerInvariant();
        return lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".png")
               || lower.EndsWith(".gif") || lower.EndsWith(".webp");
    }

    private async Task<bool> PassBusinessRules(CreateProductProfileRequest request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var todayCount = await _context.Product.CountAsync(
            p => p.CreatedAt.Date == today,
            cancellationToken);

        if (todayCount >= 500)
        {
            _logger.LogWarning("Daily product addition limit reached for date {Date}", today);
            return false;
        }
        
        if (request.Category == ProductCategory.Electronics && request.Price < 50.0m)
        {
            _logger.LogWarning("Electronics minimum price rule failed. SKU {SKU}, Price {Price}", request.SKU, request.Price);
            return false;
        }
        
        if (request.Category == ProductCategory.Home && !BeAppropriateForHome(request.Name))
        {
            _logger.LogWarning("Home product content rule failed for Name {Name}", request.Name);
            return false;
        }
        
        if (request.Price > 500m && request.StockQuantity > 10)
        {
            _logger.LogWarning("High value stock limit rule failed. Price {Price}, Stock {Stock}",
                request.Price, request.StockQuantity);
            return false;
        }

        return true;
    }

    private bool ContainTechnologyKeywords(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var lowered = name.ToLowerInvariant();
        return TechnologyKeywords.Any(k => lowered.Contains(k));
    }

    private bool BeAppropriateForHome(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        var lowered = name.ToLowerInvariant();
        return !HomeRestrictedWords.Any(w => lowered.Contains(w));
    }
}
