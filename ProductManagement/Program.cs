using Microsoft.EntityFrameworkCore;


using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi;
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

// PostgreSQL
builder.Services.AddDbContext<ProductManagementContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Caching
builder.Services.AddMemoryCache();

// AutoMapper
builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Handlers
builder.Services.AddScoped<CreateProductProfileHandler>();

// CORS (optional)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Ensure DB
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductManagementContext>();
    context.Database.Migrate();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationIdMiddleware();
app.UseGlobalExceptionMiddleware();
app.UseCors("DevCors");
app.UseHttpsRedirection();

// ENDPOINT — Product Creation
app.MapPost("/products", async (CreateProductProfileRequest req, CreateProductProfileHandler handler) =>
    await handler.Handle(req));

app.Run();

public partial class Program { }
