using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using FluentValidation;
using FluentValidation.AspNetCore;
using ProductManagement.Persistence;
using ProductManagement.Middleware;
using ProductManagement.Mappings;
using ProductManagement.Features.Products;
using ProductManagement.Validators;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Management API",
        Version = "v1",
        Description = "API for managing products."
    });
});

builder.Services.AddDbContext<ProductManagementContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMemoryCache();
builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile));
builder.Services.AddScoped<IValidator<CreateProductProfileRequest>, CreateProductProfileValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<CreateProductProfileHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductManagementContext>();
    context.Database.Migrate();
}

app.UseCorrelationIdMiddleware();
app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevCors");
app.UseHttpsRedirection();

app.MapPost("/products", async (CreateProductProfileRequest req, CreateProductProfileHandler handler) =>
    await handler.Handle(req))
    .WithName("CreateProduct")
    .WithOpenApi(op =>
    {
        op.Description = "Creates a new product with full validation, mapping and logging.";
        return op;
    });

app.Run();

public partial class Program { }
