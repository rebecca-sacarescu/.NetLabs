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

    private readonly string[] _inappropriateWords =
    {
        "bad", "nasty", "illegal", "toxic", "weapon", "xxx", "nsfw", "drug"
    };

    private readonly string[] _homeRestrictedWords =
    {
        "toxic", "chemical", "explosive", "unsafe"
    };

    private readonly string[] _technologyKeywords =
    {
        "smart", "4k", "hd", "wifi", "bluetooth", "intel", "amd", "nvidia",
        "laptop", "tablet", "phone", "device", "ultra", "pro"
    };

    public CreateProductProfileValidator(
        ProductManagementContext context,
        ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(1).WithMessage("Name must be at least 1 character long.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
            .Must(BeValidName).WithMessage("Product name contains inappropriate content.")
            .MustAsync(BeUniqueName).WithMessage("Product name must be unique per brand.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MinimumLength(2).WithMessage("Brand must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Brand must not exceed 100 characters.")
            .Must(BeValidBrandName).WithMessage("Brand contains invalid characters.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .Must(BeValidSKU).WithMessage("SKU must be alphanumeric with hyphens, 5-20 characters.")
            .MustAsync(BeUniqueSKU).WithMessage("SKU must be unique.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category must be a valid value.");

        RuleFor(x => x.Price)
            .GreaterThan(0m).WithMessage("Price must be greater than 0.")
            .LessThan(10000m).WithMessage("Price must be less than 10,000.");

        RuleFor(x => x.ReleaseDate)
            .Must(d => d <= DateTime.UtcNow)
            .WithMessage("Release date cannot be in the future.")
            .Must(d => d >= new DateTime(1900, 1, 1))
            .WithMessage("Release date cannot be before year 1900.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000.");

        RuleFor(x => x.ImageUrl)
            .Must(BeValidImageUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("ImageUrl must be a valid HTTP/HTTPS image URL.");

        RuleFor(x => x)
            .MustAsync(PassBusinessRules)
            .WithMessage("Product does not satisfy business rules.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(50m)
            .When(x => x.Category == ProductCategory.Electronics)
            .WithMessage("Electronics products must have a minimum price of $50.00.");

        RuleFor(x => x)
            .Must(ContainTechnologyKeywords)
            .When(x => x.Category == ProductCategory.Electronics)
            .WithMessage("Electronics products must contain at least one technology keyword in the name.");

        RuleFor(x => x.ReleaseDate)
            .Must(d => d >= DateTime.UtcNow.AddYears(-5) && d <= DateTime.UtcNow)
            .When(x => x.Category == ProductCategory.Electronics)
            .WithMessage("Electronics products must be released within the last 5 years.");

        RuleFor(x => x.Price)
            .LessThanOrEqualTo(200m)
            .When(x => x.Category == ProductCategory.Home)
            .WithMessage("Home products must not have a price greater than $200.00.");

        RuleFor(x => x)
            .Must(BeAppropriateForHome)
            .When(x => x.Category == ProductCategory.Home)
            .WithMessage("Home product name contains restricted content.");

        RuleFor(x => x.Brand)
            .MinimumLength(3)
            .When(x => x.Category == ProductCategory.Clothing)
            .WithMessage("Clothing products must have a brand name of at least 3 characters.");

        RuleFor(x => x)
            .Must(x => x.Price <= 100m || x.StockQuantity <= 20)
            .WithMessage("Expensive products (over $100) must have limited stock (20 units or less).");
    }

    private bool BeValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return !_inappropriateWords.Any(w =>
            name.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeValidBrandName(string brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return false;

        var regex = new Regex(@"^[A-Za-z0-9\s\-'\.]+$");
        return regex.IsMatch(brand);
    }

    private bool BeValidSKU(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return false;

        sku = sku.Replace(" ", string.Empty);
        var regex = new Regex(@"^[A-Za-z0-9\-]{5,20}$");
        return regex.IsMatch(sku);
    }

    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return allowedExtensions.Any(ext =>
            uri.AbsolutePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> BeUniqueName(CreateProductProfileRequest model, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(model.Brand))
            return true;

        var exists = await _context.Product
            .AnyAsync(p => p.Name == name && p.Brand == model.Brand, ct);

        if (exists)
        {
            _logger.LogWarning("Name+Brand uniqueness check failed for Name {Name}, Brand {Brand}.", name, model.Brand);
        }

        return !exists;
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return true;

        var exists = await _context.Product
            .AnyAsync(p => p.SKU == sku, ct);

        if (exists)
        {
            _logger.LogWarning("SKU uniqueness check failed for {SKU}", sku);
        }

        return !exists;
    }

    private async Task<bool> PassBusinessRules(CreateProductProfileRequest request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var dailyCount = await _context.Product
            .CountAsync(p => p.CreatedAt.Date == today, ct);

        if (dailyCount >= 500)
        {
            _logger.LogWarning("Daily product addition limit exceeded. TodayCount: {Count}", dailyCount);
            return false;
        }

        if (request.Category == ProductCategory.Electronics && request.Price < 50m)
        {
            _logger.LogWarning("Electronics minimum price rule violated for SKU {SKU}", request.SKU);
            return false;
        }

        if (request.Category == ProductCategory.Home &&
            _homeRestrictedWords.Any(w =>
                request.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Home product content restriction violated for Name {Name}", request.Name);
            return false;
        }

        if (request.Price > 500m && request.StockQuantity > 10)
        {
            _logger.LogWarning("High-value product stock limit violated for SKU {SKU}", request.SKU);
            return false;
        }

        return true;
    }

    private bool ContainTechnologyKeywords(CreateProductProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return false;

        return _technologyKeywords.Any(k =>
            request.Name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeAppropriateForHome(CreateProductProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return true;

        return !_homeRestrictedWords.Any(w =>
            request.Name.Contains(w, StringComparison.OrdinalIgnoreCase));
    }
}
