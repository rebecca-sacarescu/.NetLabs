using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProductManagement.Validators.Attributes;

public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    public ValidSKUAttribute()
    {
        ErrorMessage = "SKU must be 5-20 characters long, alphanumeric with hyphens.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true; 

        var sku = value.ToString()!.Replace(" ", string.Empty);

        if (string.IsNullOrWhiteSpace(sku))
            return true;

        if (sku.Length < 5 || sku.Length > 20)
            return false;

        return Regex.IsMatch(sku, @"^[A-Za-z0-9\-]+$");
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-validsku", ErrorMessage ?? "Invalid SKU format.");
    }

    private static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (attributes.ContainsKey(key))
            return false;

        attributes.Add(key, value);
        return true;
    }
}