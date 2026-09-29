using ProductManagement.API.Products.Dtos;
using ProductManagement.Core.Entities;

namespace ProductManagement.API.Products;

public static class ProductMappingProfile
{
    public static ProductResponse ToResponse(this Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.CategoryId,
            product.Status.ToString(),
            product.AttributeValues.Select(attribute => new ProductAttributeResponse(
                attribute.AttributeDefinitionId,
                attribute.TextValue
                    ?? attribute.IntegerValue?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ?? attribute.DecimalValue?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ?? attribute.BooleanValue?.ToString().ToLowerInvariant())).ToArray(),
            product.Variants.Select(variant => new ProductVariantResponse(
                variant.Id,
                variant.Sku,
                variant.Price,
                variant.Currency,
                variant.StockOnHand)).ToArray(),
            ProductConcurrency.ForProduct(product.RowVersion));
    }
}
