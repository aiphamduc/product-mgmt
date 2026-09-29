using ProductManagement.API.Products.Dtos;
using ProductManagement.API.Products.Validators;
using Xunit;

namespace ProductManagement.UnitTests;

public sealed class ProductRequestValidatorTests
{
    [Fact]
    public async Task RejectsInvalidSlug()
    {
        var validator = new ProductRequestValidator();
        var request = new ProductRequest(
            "T-Shirt",
            "Not A Slug",
            null,
            Guid.NewGuid(),
            Array.Empty<ProductAttributeRequest>(),
            new[]
            {
                new ProductVariantRequest("TSHIRT-BLK-M", 24.99m, "USD", 10)
            });

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ProductRequest.Slug));
    }

    [Fact]
    public async Task RejectsNegativeVariantPrice()
    {
        var validator = new ProductRequestValidator();
        var request = new ProductRequest(
            "Valid product",
            "valid-product",
            "desc",
            Guid.NewGuid(),
            Array.Empty<ProductAttributeRequest>(),
            new[]
            {
                new ProductVariantRequest("TSHIRT-BLK-M", -1m, "USD", 10)
            });

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains("Price", StringComparison.OrdinalIgnoreCase));
    }
}
