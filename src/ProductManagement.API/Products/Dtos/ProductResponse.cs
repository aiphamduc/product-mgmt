namespace ProductManagement.API.Products.Dtos;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid CategoryId,
    string Status,
    IReadOnlyCollection<ProductAttributeResponse> Attributes,
    IReadOnlyCollection<ProductVariantResponse> Variants,
    string ETag);

public sealed record ProductAttributeResponse(Guid DefinitionId, string? Value);

public sealed record ProductVariantResponse(
    Guid Id,
    string Sku,
    decimal Price,
    string Currency,
    int StockOnHand);
