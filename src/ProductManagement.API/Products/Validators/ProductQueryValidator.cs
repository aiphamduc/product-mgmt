using FluentValidation;
using ProductManagement.API.Products.Dtos;

namespace ProductManagement.API.Products.Validators;

public sealed record ProductQuery(string? CategoryId, string? Status, string? Q, string? Sort, int Limit = 20, string? Cursor = null);

public sealed class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    public ProductQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort).Must(sort => string.IsNullOrWhiteSpace(sort) || sort is "updatedUtc" or "updatedUtc:asc" or "updatedUtc:desc" or "name" or "name:asc" or "name:desc")
            .WithMessage("Sort must be updatedUtc or name with an optional asc/desc direction.");
        RuleFor(x => x.Status).Must(status => string.IsNullOrWhiteSpace(status) || Enum.TryParse<ProductManagement.Core.Entities.ProductStatus>(status, true, out _))
            .WithMessage("Status is invalid.");
        RuleFor(x => x.CategoryId).Must(id => string.IsNullOrWhiteSpace(id) || Guid.TryParse(id, out _))
            .WithMessage("CategoryId must be a GUID.");
    }
}
