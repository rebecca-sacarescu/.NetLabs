using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using ProductManagement;
using ProductManagement.Exceptions;
using ProductManagement.Features.Products;
using ProductManagement.Mappings;
using ProductManagement.Persistence;
using ProductManagement.Validators;
using Xunit;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ProductManagementContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CreateProductProfileHandler>> _mockLogger;

    public CreateProductHandlerIntegrationTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ProductManagementContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProductManagementContext(dbOptions);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<AdvancedProductMappingProfile>();
        });

        _mapper = mapperConfig.CreateMapper();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<CreateProductProfileHandler>>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        var validator = new CreateProductProfileValidator(_context, 
            Mock.Of<ILogger<CreateProductProfileValidator>>());

        var handler = new CreateProductProfileHandler(
            _context, _mockLogger.Object, validator, _mapper, _cache);

        var request = new CreateProductProfileRequest(
            Name: "Smart Ultra 4K TV",
            Brand: "Tech Brand",
            SKU: "ELEC-9001",
            Category: ProductCategory.Electronics,
            Price: 999,
            ReleaseDate: DateTime.UtcNow.AddMonths(-6),
            ImageUrl: "https://example.com/tv.jpg",
            StockQuantity: 5
        );

        var result = await handler.Handle(request);

        result.Should().NotBeNull();
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>>();

        var created = (result as Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>)!.Value;

        created.CategoryDisplayName.Should().Be("Electronics & Technology");
        created.BrandInitials.Should().Be("TB");
        created.FormattedPrice.Should().StartWith("$");
        created.IsAvailable.Should().BeTrue();
        created.AvailabilityStatus.Should().Be("Limited Stock");
        created.ProductAge.Should().Contain("months");

        _mockLogger.VerifyLogWithEventId(LogLevel.Information, 2001);

    }

    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {

        await _context.Product.AddAsync(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Product",
            Brand = "BrandX",
            SKU = "DUPL-1111",
            Category = ProductCategory.Electronics,
            Price = 150,
            ReleaseDate = DateTime.UtcNow.AddMonths(-2),
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var validator = new CreateProductProfileValidator(_context,
            Mock.Of<ILogger<CreateProductProfileValidator>>());

        var handler = new CreateProductProfileHandler(
            _context, _mockLogger.Object, validator, _mapper, _cache);

        var request = new CreateProductProfileRequest(
            Name: "New Product",
            Brand: "BrandY",
            SKU: "DUPL-1111", 
            Category: ProductCategory.Electronics,
            Price: 300,
            ReleaseDate: DateTime.UtcNow.AddMonths(-1),
            ImageUrl: null,
            StockQuantity: 5
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(request));
        ex.Message.Should().Contain("Validation failed");
        ex.Errors.Should().Contain(e => e.Contains("unique"));

        _mockLogger.VerifyLogWithEventId(LogLevel.Warning, 2002);

    }

    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {

        var validator = new CreateProductProfileValidator(_context,
            Mock.Of<ILogger<CreateProductProfileValidator>>());

        var handler = new CreateProductProfileHandler(
            _context, _mockLogger.Object, validator, _mapper, _cache);

        var request = new CreateProductProfileRequest(
            Name: "Wooden Chair",
            Brand: "HomeBrand",
            SKU: "HOME-3001",
            Category: ProductCategory.Home,
            Price: 100,
            ReleaseDate: DateTime.UtcNow.AddMonths(-3),
            ImageUrl: "https://example.com/chair.jpg",
            StockQuantity: 20
        );

        var result = await handler.Handle(request);
        var dto = (result as Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>)!.Value;

        dto.CategoryDisplayName.Should().Be("Home & Garden");
        dto.Price.Should().Be(90);     
        dto.ImageUrl.Should().BeNull();
    }
}
