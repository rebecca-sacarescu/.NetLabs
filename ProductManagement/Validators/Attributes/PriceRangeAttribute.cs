using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _min;
    private readonly decimal _max;

    public PriceRangeAttribute(double min, double max)
    {
        _min = (decimal)min;
        _max = (decimal)max;
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;

        if (value is decimal dec)
        {
            return dec >= _min && dec <= _max;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be between {_min:C2} and {_max:C2}.";
    }
}