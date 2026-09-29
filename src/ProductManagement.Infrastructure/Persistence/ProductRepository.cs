using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ProductManagement.Core.Entities;
using ProductManagement.Core.Interfaces;

namespace ProductManagement.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private readonly ProductDbContext dbContext;

    public ProductRepository(ProductDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Products
            .AsNoTracking()
            .Include(product => product.Variants)
            .Include(product => product.AttributeValues)
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<ProductListResult> GetListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var products = dbContext.Products
            .AsNoTracking()
            .Include(product => product.Variants)
            .Include(product => product.AttributeValues)
            .AsQueryable();

        if (query.CategoryId.HasValue) products = products.Where(product => product.CategoryId == query.CategoryId.Value);
        if (query.Status.HasValue) products = products.Where(product => product.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search)) products = products.Where(product => product.Name.Contains(query.Search) || product.Slug.Contains(query.Search));

        if (query.Cursor is not null)
        {
            products = query.Sort == "name"
                ? query.Descending
                    ? products.Where(product => string.Compare(product.Name, query.Cursor.SortKey) < 0 || (product.Name == query.Cursor.SortKey && product.Id.CompareTo(query.Cursor.Id) < 0))
                    : products.Where(product => string.Compare(product.Name, query.Cursor.SortKey) > 0 || (product.Name == query.Cursor.SortKey && product.Id.CompareTo(query.Cursor.Id) > 0))
                : query.Descending
                    ? products.Where(product => product.UpdatedUtc < query.Cursor.UpdatedUtc || (product.UpdatedUtc == query.Cursor.UpdatedUtc && product.Id.CompareTo(query.Cursor.Id) < 0))
                    : products.Where(product => product.UpdatedUtc > query.Cursor.UpdatedUtc || (product.UpdatedUtc == query.Cursor.UpdatedUtc && product.Id.CompareTo(query.Cursor.Id) > 0));
        }

        var ordered = query.Sort == "name"
            ? query.Descending ? products.OrderByDescending(product => product.Name).ThenByDescending(product => product.Id) : products.OrderBy(product => product.Name).ThenBy(product => product.Id)
            : query.Descending ? products.OrderByDescending(product => product.UpdatedUtc).ThenByDescending(product => product.Id) : products.OrderBy(product => product.UpdatedUtc).ThenBy(product => product.Id);
        var items = await ordered.Take(query.Limit + 1).ToListAsync(cancellationToken);
        var nextCursor = items.Count > query.Limit
            ? new ProductCursor(query.Sort, query.Sort == "name" ? items[query.Limit - 1].Name : string.Empty, items[query.Limit - 1].UpdatedUtc, items[query.Limit - 1].Id)
            : null;
        return new ProductListResult(items.Take(query.Limit).ToArray(), nextCursor);
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        dbContext.Products.Add(product);
        AddOutboxMessage("ProductChanged", product.Id);
        await SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> UpdateAsync(Product product, byte[] expectedRowVersion, CancellationToken cancellationToken = default)
    {
        var tracked = await dbContext.Products
            .Include(entry => entry.Variants)
            .Include(entry => entry.AttributeValues)
            .SingleOrDefaultAsync(entry => entry.Id == product.Id, cancellationToken);

        if (tracked is null)
        {
            throw new InvalidOperationException($"Product {product.Id} was not found.");
        }

        dbContext.Entry(tracked).Property(entry => entry.RowVersion).OriginalValue = expectedRowVersion;

        tracked.Name = product.Name;
        tracked.Slug = product.Slug;
        tracked.Description = product.Description;
        tracked.CategoryId = product.CategoryId;
        tracked.Status = product.Status;
        tracked.UpdatedUtc = product.UpdatedUtc;

        tracked.Variants.Clear();
        foreach (var variant in product.Variants)
        {
            tracked.Variants.Add(variant);
        }

        tracked.AttributeValues.Clear();
        foreach (var attribute in product.AttributeValues)
        {
            tracked.AttributeValues.Add(attribute);
        }

        AddOutboxMessage("ProductChanged", product.Id);
        await SaveChangesAsync(cancellationToken);
        return tracked;
    }

    public async Task<Product?> AddVariantAsync(Guid productId, ProductVariant variant, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(entry => entry.Variants)
            .SingleOrDefaultAsync(entry => entry.Id == productId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.Variants.Add(variant);
        product.UpdatedUtc = DateTime.UtcNow;
        AddOutboxMessage("ProductChanged", productId);
        await SaveChangesAsync(cancellationToken);
        return product;
    }

    public Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        dbContext.ProductVariants
            .AsNoTracking()
            .SingleOrDefaultAsync(variant => variant.Id == variantId, cancellationToken);

    public async Task<ProductVariant?> UpdateVariantAsync(Guid variantId, ProductVariant variant, byte[] expectedRowVersion, CancellationToken cancellationToken = default)
    {
        var current = await dbContext.ProductVariants
            .SingleOrDefaultAsync(entry => entry.Id == variantId, cancellationToken);

        if (current is null)
        {
            return null;
        }

        dbContext.Entry(current).Property(entry => entry.RowVersion).OriginalValue = expectedRowVersion;

        current.Sku = variant.Sku;
        current.Price = variant.Price;
        current.Currency = variant.Currency;
        current.StockOnHand = variant.StockOnHand;
        AddOutboxMessage("ProductChanged", current.ProductId);
        await SaveChangesAsync(cancellationToken);
        return current;
    }

    public Task<Category?> GetCategoryWithDefinitionsAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        dbContext.Categories
            .Include(category => category.AttributeDefinitions)
            .SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);

    public async Task<Product?> ChangeStatusAsync(Guid productId, ProductStatus status, byte[] expectedRowVersion, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(entry => entry.Id == productId, cancellationToken);
        if (product is null) return null;
        dbContext.Entry(product).Property(entry => entry.RowVersion).OriginalValue = expectedRowVersion;
        product.Status = status;
        product.UpdatedUtc = DateTime.UtcNow;
        AddOutboxMessage("ProductChanged", productId);
        await SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(productId, cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The resource was modified by another request.");
        }
        catch (DbUpdateException)
        {
            throw new DuplicateResourceException("A product slug or variant SKU already exists.");
        }
    }

    private void AddOutboxMessage(string type, Guid productId)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(new { productId }),
            CreatedUtc = DateTime.UtcNow
        });
    }
}
