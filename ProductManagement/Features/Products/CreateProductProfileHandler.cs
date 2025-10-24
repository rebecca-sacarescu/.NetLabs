using Microsoft.EntityFrameworkCore;
using ProductManagement.Exceptions;
using ProductManagement.Persistence;
using ProductManagement.Validators;
namespace ProductManagement.Features.Products;

public class CreateProductProfileHandler(ProductManagementContext context, ILogger<CreateProductProfileHandler> logger)
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductProfileHandler> _logger = logger;
    
    public async Task<IResult> Handle(CreateProductProfileRequest request)
    {
        _logger.LogInformation(
            "Attempting to create product. Name: {Name}, Brand: {Brand}, SKU: {SKU}, Category: {Category}",
            request.Name, request.Brand, request.SKU, request.Category);
        
        var validator = new CreateProductProfileValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for product SKU {SKU}.", request.SKU);
            
            var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            throw new Exceptions.ValidationException("Validation failed", validationErrors);
        }
        
        if(await _context.Product.AnyAsync(p => p.SKU == request.SKU))
        {
            _logger.LogWarning("SKU {SKU} already exists. Creation failed.", request.SKU);
            
            throw new ProductSKUConflictException(request.SKU);
        }
        
        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Brand = request.Brand,
            SKU = request.SKU,
            Category = request.Category,
            Price = request.Price,
            ReleaseDate = request.ReleaseDate,
            ImageUrl = request.ImageUrl,
            StockQuantity = request.StockQuantity,
            CreatedAt = DateTime.UtcNow
        };
        _context.Product.Add(newProduct);
        await _context.SaveChangesAsync();
        
        var ageSpan = DateTime.UtcNow - newProduct.ReleaseDate;
        string productAge;
        if (ageSpan.TotalDays < 1)
            productAge = "Less than a day";
        else if (ageSpan.TotalDays < 365.25)
            productAge = $"{(int)ageSpan.TotalDays} days";
        else
        {
            var years = (int)(ageSpan.TotalDays / 365.25);
            productAge = years == 1 ? "1 year" : $"{years} years";
        }

        string brandInitials = string.Empty;
        if (!string.IsNullOrWhiteSpace(newProduct.Brand))
        {
            brandInitials = string.Concat(newProduct.Brand
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s[0].ToString().ToUpperInvariant()));
        }
        var dto = new ProductProfileDto
        {
            Id = newProduct.Id,
            Name = newProduct.Name,
            Brand = newProduct.Brand,
            SKU = newProduct.SKU,
            CategoryDisplayName = newProduct.Category.ToString(),
            Price = newProduct.Price,
            ReleaseDate = newProduct.ReleaseDate,
            CreatedAt = newProduct.CreatedAt,
            ImageUrl = newProduct.ImageUrl,
            IsAvailable = newProduct.IsAvailable,
            StockQuantity = newProduct.StockQuantity,
            ProductAge = productAge,
            BrandInitials = brandInitials
        };

        return Results.Created($"/products/{dto.Id}", dto);
        
    }
}