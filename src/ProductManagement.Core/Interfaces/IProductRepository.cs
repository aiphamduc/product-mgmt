using ProductManagement.Core.Entities;

namespace ProductManagement.Core.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductListResult> GetListAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Product product, byte[] expectedRowVersion, CancellationToken cancellationToken = default);
    Task<Product?> AddVariantAsync(Guid productId, ProductVariant variant, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<ProductVariant?> UpdateVariantAsync(Guid variantId, ProductVariant variant, byte[] expectedRowVersion, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryWithDefinitionsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Product?> ChangeStatusAsync(Guid productId, ProductStatus status, byte[] expectedRowVersion, CancellationToken cancellationToken = default);
}

public sealed record ProductListQuery(
    Guid? CategoryId,
    ProductStatus? Status,
    string? Search,
    string Sort,
    bool Descending,
    int Limit,
    ProductCursor? Cursor);

public sealed record ProductCursor(string Sort, string SortKey, DateTime UpdatedUtc, Guid Id);

public sealed record ProductListResult(IReadOnlyCollection<Product> Items, ProductCursor? NextCursor);

public sealed class DuplicateResourceException : Exception
{
    public DuplicateResourceException(string message) : base(message) { }
}

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }
}

public interface IInventoryService
{
    Task<StockAdjustmentResult> AdjustStockAsync(Guid variantId, StockAdjustmentCommand command, CancellationToken cancellationToken = default);
}

public sealed record StockAdjustmentCommand(int Quantity, string Reason, string IdempotencyKey, Guid? ActorId = null);

public sealed record StockAdjustmentResult(Guid AdjustmentId, Guid VariantId, int previousStock, int NewStock, bool WasReplay);

public interface IProductCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<long> AdvanceVersionAsync(CancellationToken cancellationToken = default);
    Task<long> GetVersionAsync(CancellationToken cancellationToken = default);
}
