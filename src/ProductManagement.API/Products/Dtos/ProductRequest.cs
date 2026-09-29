namespace ProductManagement.API.Products.Dtos;

public sealed record ProductRequest(
    string Name,
    string Slug,
    string? Description,
    Guid CategoryId,
    IReadOnlyCollection<ProductAttributeRequest> Attributes,
    IReadOnlyCollection<ProductVariantRequest> Variants);

public sealed record ProductAttributeRequest(Guid DefinitionId, string Value);

public sealed record ProductVariantRequest(string Sku, decimal Price, string Currency, int Stock);
