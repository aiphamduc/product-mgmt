namespace ProductManagement.API.Products.Dtos;

public sealed record ProductListResponse(
    IReadOnlyCollection<ProductResponse> Items,
    string? NextCursor,
    int Limit);

public sealed record ProductStatusRequest(string Status);
