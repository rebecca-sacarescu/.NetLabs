using FluentValidation;

namespace ProductManagement.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    public CreateProductProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotNull().NotEmpty()
            .MinimumLength(3)
            .WithMessage("Name must be at least 3 characters long.");

        RuleFor(x => x.Brand)
            .NotNull().NotEmpty()
            .WithMessage("Brand is required.");

        RuleFor(x => x.SKU)
            .NotNull().NotEmpty()
            .MinimumLength(4)
            .WithMessage("SKU must be at least 4 characters long.");
            
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0.");
            
        RuleFor(x => x.ReleaseDate)
            .NotEmpty()
            .WithMessage("Release Date is required.");
    }
}