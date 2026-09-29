using FluentValidation;
using ProductManagement.API.Products.Dtos;

namespace ProductManagement.API.Products.Validators;

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .Must(name => string.IsNullOrWhiteSpace(name) == false)
            .WithMessage("Name is required.");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be a URL-safe lowercase identifier.");

        RuleFor(x => x.Description)
            .MaximumLength(10000)
            .When(x => x.Description is not null);

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Attributes)
            .NotNull();

        RuleFor(x => x.Variants)
            .NotNull()
            .Must(variants => variants is not null && variants.Count > 0)
            .WithMessage("At least one variant is required.")
            .WithName("Variants");

        RuleForEach(x => x.Variants)
            .ChildRules(variant =>
            {
                variant.RuleFor(x => x.Sku)
                    .NotEmpty()
                    .MaximumLength(100)
                    .Must(sku => string.IsNullOrWhiteSpace(sku) == false)
                    .WithMessage("Variant SKU is required.");

                variant.RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(0m)
                    .WithMessage("Price cannot be negative.");

                variant.RuleFor(x => x.Currency)
                    .NotEmpty()
                    .Must(currency => string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("Currency must be USD.");

                variant.RuleFor(x => x.Stock)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Stock cannot be negative.");
            });
    }
}
