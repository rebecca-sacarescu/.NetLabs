using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProductManagement.Exceptions;
using ProductManagement.Logging;
using ProductManagement.Metrics;
using ProductManagement.Persistence;

namespace ProductManagement.Features.Products;

public class CreateProductProfileHandler
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductProfileHandler> _logger;
    private readonly IValidator<CreateProductProfileRequest> _validator;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    private const string AllProductsCacheKey = "all_products";

    public CreateProductProfileHandler(
        ProductManagementContext context,
        ILogger<CreateProductProfileHandler> logger,
        IValidator<CreateProductProfileRequest> validator,
        IMapper mapper,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _validator = validator;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IResult> Handle(CreateProductProfileRequest request)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];

        var totalWatch = System.Diagnostics.Stopwatch.StartNew();
        var validationWatch = new System.Diagnostics.Stopwatch();
        var dbWatch = new System.Diagnostics.Stopwatch();

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId,
            ["ProductName"] = request.Name,
            ["SKU"] = request.SKU,
            ["Category"] = request.Category.ToString()
        });

        _logger.LogInformation(
            new EventId(LogEvents.ProductCreationStarted, nameof(LogEvents.ProductCreationStarted)),
            "Product creation started. Name: {Name}, Brand: {Brand}, SKU: {SKU}, Category: {Category}",
            request.Name, request.Brand, request.SKU, request.Category);

        try
        {

            validationWatch.Start();

            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    new EventId(LogEvents.ProductValidationFailed, nameof(LogEvents.ProductValidationFailed)),
                    "Validation failed for product SKU {SKU}. Errors: {Errors}",
                    request.SKU,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());

                var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                throw new Exceptions.ValidationException("Validation failed", validationErrors);
            }

            _logger.LogInformation(
                new EventId(LogEvents.SKUValidationPerformed, nameof(LogEvents.SKUValidationPerformed)),
                "Checking SKU uniqueness for {SKU}", request.SKU);

            if (await _context.Product.AnyAsync(p => p.SKU == request.SKU))
            {
                _logger.LogWarning(
                    new EventId(LogEvents.ProductValidationFailed, nameof(LogEvents.ProductValidationFailed)),
                    "SKU {SKU} already exists. Creation failed.",
                    request.SKU);

                throw new ProductSKUConflictException(request.SKU);
            }

            _logger.LogInformation(
                new EventId(LogEvents.StockValidationPerformed, nameof(LogEvents.StockValidationPerformed)),
                "Stock validation performed for SKU {SKU}, StockQuantity: {StockQuantity}",
                request.SKU, request.StockQuantity);

            validationWatch.Stop();

            dbWatch.Start();

            _logger.LogInformation(
                new EventId(LogEvents.DatabaseOperationStarted, nameof(LogEvents.DatabaseOperationStarted)),
                "Database operation started for SKU {SKU}", request.SKU);

            var product = _mapper.Map<Product>(request);

            _context.Product.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                new EventId(LogEvents.DatabaseOperationCompleted, nameof(LogEvents.DatabaseOperationCompleted)),
                "Database operation completed for ProductId {ProductId}", product.Id);

            dbWatch.Stop();
            _logger.LogInformation(
                new EventId(LogEvents.CacheOperationPerformed, nameof(LogEvents.CacheOperationPerformed)),
                "Cache invalidation performed for key {CacheKey}",
                AllProductsCacheKey);

            _cache.Remove(AllProductsCacheKey);
            
            totalWatch.Stop();

            var metrics = new ProductCreationMetrics(
                OperationId: operationId,
                ProductName: product.Name,
                SKU: product.SKU,
                Category: product.Category,
                ValidationDuration: validationWatch.Elapsed,
                DatabaseSaveDuration: dbWatch.Elapsed,
                TotalDuration: totalWatch.Elapsed,
                Success: true,
                ErrorReason: null);

            _logger.LogProductCreationMetrics(metrics);

            var dto = _mapper.Map<ProductProfileDto>(product);

            _logger.LogInformation(
                new EventId(LogEvents.ProductCreationCompleted, nameof(LogEvents.ProductCreationCompleted)),
                "Product creation completed successfully. ProductId: {ProductId}",
                product.Id);

            return Results.Created($"/products/{dto.Id}", dto);
        }
        catch (Exception ex)
        {
            totalWatch.Stop();
            validationWatch.Stop();
            dbWatch.Stop();

            var errorMetrics = new ProductCreationMetrics(
                OperationId: operationId,
                ProductName: request.Name,
                SKU: request.SKU,
                Category: request.Category,
                ValidationDuration: validationWatch.Elapsed,
                DatabaseSaveDuration: dbWatch.Elapsed,
                TotalDuration: totalWatch.Elapsed,
                Success: false,
                ErrorReason: ex.Message);

            _logger.LogProductCreationMetrics(errorMetrics);
            
            throw;
        }
    }
}
