using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ProductCategoryAttribute : ValidationAttribute
{
    private readonly ProductCategory[] _allowedCategories;

    public ProductCategoryAttribute(params ProductCategory[] allowedCategories)
    {
        _allowedCategories = allowedCategories;
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;

        if (value is ProductCategory category)
        {
            return _allowedCategories.Contains(category);
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        var allowed = string.Join(", ", _allowedCategories.Select(c => c.ToString()));
        return $"The {name} field must be one of the following categories: {allowed}.";
    }
}