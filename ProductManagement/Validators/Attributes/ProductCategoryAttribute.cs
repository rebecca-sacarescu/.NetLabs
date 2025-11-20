using System.ComponentModel.DataAnnotations;
using ProductManagement;

namespace ProductManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ProductCategoryAttribute : ValidationAttribute
{
    private readonly ProductCategory[] _allowed;

    public ProductCategoryAttribute(params ProductCategory[] allowed)
    {
        _allowed = allowed;
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;

        if (value is ProductCategory category)
        {
            return _allowed.Contains(category);
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        var allowedList = string.Join(", ", _allowed.Select(a => a.ToString()));
        return $"The {name} field must be one of the following categories: {allowedList}.";
    }
}