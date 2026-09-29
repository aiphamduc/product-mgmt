namespace ProductManagement.Core.Entities;

public enum ProductStatus
{
    Draft,
    Published,
    Archived
}

public enum AttributeValueType
{
    Text,
    Integer,
    Decimal,
    Boolean
}

public sealed class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<ProductAttributeDefinition> AttributeDefinitions { get; set; } = new List<ProductAttributeDefinition>();
}

public sealed class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public ProductStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
}

public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public int StockOnHand { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public sealed class ProductAttributeDefinition
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public AttributeValueType ValueType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
}

public sealed class ProductAttributeValue
{
    public Guid ProductId { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public string? TextValue { get; set; }
    public int? IntegerValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public bool? BooleanValue { get; set; }
    public Product Product { get; set; } = null!;
    public ProductAttributeDefinition AttributeDefinition { get; set; } = null!;
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
}

public sealed class StockAdjustment
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public Guid? ActorId { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
}
