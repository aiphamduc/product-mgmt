using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.API.Products.Dtos;
using ProductManagement.API.Products.Validators;
using ProductManagement.Core.Entities;
using ProductManagement.Core.Interfaces;
using System.Text.Json;

namespace ProductManagement.API.Products;

[ApiController]
[AllowAnonymous]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductRepository productRepository;
    private readonly IValidator<ProductRequest> productRequestValidator;
    private readonly IValidator<ProductQuery> productQueryValidator;
    private readonly IProductCache productCache;

    public ProductsController(IProductRepository productRepository, IValidator<ProductRequest> productRequestValidator, IValidator<ProductQuery> productQueryValidator, IProductCache productCache)
    {
        this.productRepository = productRepository;
        this.productRequestValidator = productRequestValidator;
        this.productQueryValidator = productQueryValidator;
        this.productCache = productCache;
    }

    [HttpGet]
    public async Task<ActionResult<ProductListResponse>> GetAll([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        var validation = await productQueryValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        if (!TryParseCursor(query.Cursor, out var cursor))
        {
            ModelState.AddModelError(nameof(query.Cursor), "Cursor is invalid.");
            return ValidationProblem(ModelState);
        }

        var sort = query.Sort?.ToLowerInvariant() ?? "updatedutc:desc";
        var descending = !sort.EndsWith(":asc", StringComparison.Ordinal);
        var sortField = sort.Split(':')[0];
        if (cursor is not null && !string.Equals(cursor.Sort, sortField, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(query.Cursor), "Cursor does not match the requested sort.");
            return ValidationProblem(ModelState);
        }
        var version = await productCache.GetVersionAsync(cancellationToken);
        var cacheKey = $"catalog:{version}:{query.CategoryId}:{query.Status}:{query.Q}:{sort}:{query.Limit}:{query.Cursor}";
        var cached = await productCache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            var cachedResponse = JsonSerializer.Deserialize<ProductListResponse>(cached);
            if (cachedResponse is not null) return Ok(cachedResponse);
        }

        var list = await productRepository.GetListAsync(new ProductListQuery(
            string.IsNullOrWhiteSpace(query.CategoryId) ? null : Guid.Parse(query.CategoryId),
            string.IsNullOrWhiteSpace(query.Status) ? null : Enum.Parse<ProductStatus>(query.Status, true),
            query.Q, sortField, descending, query.Limit, cursor), cancellationToken);
        var response = new ProductListResponse(list.Items.Select(product => product.ToResponse()).ToArray(), list.NextCursor is null ? null : EncodeCursor(list.NextCursor), query.Limit);
        await productCache.SetAsync(cacheKey, JsonSerializer.Serialize(response), TimeSpan.FromSeconds(60), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null) return NotFound();
        var etag = ProductConcurrency.ForProduct(product.RowVersion);
        if (ProductConcurrency.Matches(Request.Headers.IfNoneMatch, etag)) return StatusCode(StatusCodes.Status304NotModified);
        Response.Headers.ETag = etag;
        return Ok(product.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request, CancellationToken cancellationToken)
    {
        var validation = await productRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        var category = await productRepository.GetCategoryWithDefinitionsAsync(request.CategoryId, cancellationToken);
        if (category is null) return NotFound("Category was not found.");
        var attributeInputs = request.Attributes.Select(attribute => new AttributeInput(attribute.DefinitionId, attribute.Value)).ToArray();
        var attributeErrors = ProductDomainRules.ValidateAttributes(category, attributeInputs);
        if (attributeErrors.Count > 0) return AttributeProblem(attributeErrors);

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Slug = request.Slug.Trim(), Description = request.Description?.Trim(),
            CategoryId = request.CategoryId, Category = category, Status = ProductStatus.Draft, CreatedUtc = now, UpdatedUtc = now,
            Variants = request.Variants.Select(variant => new ProductVariant
            {
                Id = Guid.NewGuid(), Sku = variant.Sku.Trim().ToUpperInvariant(), Price = variant.Price,
                Currency = variant.Currency.Trim().ToUpperInvariant(), StockOnHand = variant.Stock
            }).ToList(),
            AttributeValues = request.Attributes.Select(attribute =>
            {
                var definition = category.AttributeDefinitions.Single(item => item.Id == attribute.DefinitionId);
                return ProductDomainRules.ToAttributeValue(new AttributeDefinitionSnapshot(definition.Id, definition.ValueType), Guid.Empty, attribute.Value);
            }).ToList()
        };
        foreach (var attribute in product.AttributeValues) attribute.ProductId = product.Id;

        var created = await productRepository.CreateAsync(product, cancellationToken);
        Response.Headers.ETag = ProductConcurrency.ForProduct(created.RowVersion);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, [FromBody] ProductRequest request, CancellationToken cancellationToken)
    {
        if (!ProductConcurrency.TryParse(Request.Headers.IfMatch, "product-", out var expectedVersion)) return StatusCode(StatusCodes.Status428PreconditionRequired);
        var validation = await productRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null) return NotFound();
        var category = await productRepository.GetCategoryWithDefinitionsAsync(request.CategoryId, cancellationToken);
        if (category is null) return NotFound("Category was not found.");
        var attributeInputs = request.Attributes.Select(attribute => new AttributeInput(attribute.DefinitionId, attribute.Value)).ToArray();
        var attributeErrors = ProductDomainRules.ValidateAttributes(category, attributeInputs);
        if (attributeErrors.Count > 0) return AttributeProblem(attributeErrors);

        product.Name = request.Name.Trim(); product.Slug = request.Slug.Trim(); product.Description = request.Description?.Trim();
        product.CategoryId = request.CategoryId; product.UpdatedUtc = DateTime.UtcNow;
        product.Variants = request.Variants.Select(variant => new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = id, Sku = variant.Sku.Trim().ToUpperInvariant(), Price = variant.Price,
            Currency = variant.Currency.Trim().ToUpperInvariant(), StockOnHand = variant.Stock
        }).ToList();
        product.AttributeValues = request.Attributes.Select(attribute =>
        {
            var definition = category.AttributeDefinitions.Single(item => item.Id == attribute.DefinitionId);
            return ProductDomainRules.ToAttributeValue(new AttributeDefinitionSnapshot(definition.Id, definition.ValueType), id, attribute.Value);
        }).ToList();

        var updated = await productRepository.UpdateAsync(product, expectedVersion, cancellationToken);
        Response.Headers.ETag = ProductConcurrency.ForProduct(updated.RowVersion);
        return Ok(updated.ToResponse());
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ProductResponse>> ChangeStatus(Guid id, [FromBody] ProductStatusRequest request, CancellationToken cancellationToken)
    {
        if (!ProductConcurrency.TryParse(Request.Headers.IfMatch, "product-", out var expectedVersion)) return StatusCode(StatusCodes.Status428PreconditionRequired);
        if (!Enum.TryParse<ProductStatus>(request.Status, true, out var requestedStatus)) return ValidationProblem("Status is invalid.");
        var current = await productRepository.GetByIdAsync(id, cancellationToken);
        if (current is null) return NotFound();
        if (!ProductDomainRules.CanTransition(current.Status, requestedStatus)) return ValidationProblem("Status transition is invalid.");
        if (requestedStatus == ProductStatus.Published)
        {
            current.Category = await productRepository.GetCategoryWithDefinitionsAsync(current.CategoryId, cancellationToken) ?? new Category();
            var errors = ProductDomainRules.ValidateForPublish(current);
            if (errors.Count > 0) return AttributeProblem(errors);
        }
        var updated = await productRepository.ChangeStatusAsync(id, requestedStatus, expectedVersion, cancellationToken);
        if (updated is null) return NotFound();
        Response.Headers.ETag = ProductConcurrency.ForProduct(updated.RowVersion);
        return Ok(updated.ToResponse());
    }

    [HttpPost("{id:guid}/variants")]
    public async Task<ActionResult<ProductVariantResponse>> AddVariant(Guid id, [FromBody] ProductVariantRequest request, CancellationToken cancellationToken)
    {
        var errors = ProductDomainRules.ValidateVariant(request.Price, request.Currency, request.Stock).ToList();
        if (string.IsNullOrWhiteSpace(request.Sku)) errors.Add("SKU is required.");
        if (errors.Count > 0) return AttributeProblem(errors);
        if (await productRepository.GetByIdAsync(id, cancellationToken) is null) return NotFound();
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = id, Sku = request.Sku.Trim().ToUpperInvariant(), Price = request.Price, Currency = request.Currency.Trim().ToUpperInvariant(), StockOnHand = request.Stock };
        await productRepository.AddVariantAsync(id, variant, cancellationToken);
        Response.Headers.ETag = ProductConcurrency.ForVariant(variant.RowVersion);
        return CreatedAtAction(nameof(Get), new { id }, new ProductVariantResponse(variant.Id, variant.Sku, variant.Price, variant.Currency, variant.StockOnHand));
    }

    [HttpPut("/api/v1/variants/{id:guid}")]
    public async Task<ActionResult<ProductVariantResponse>> UpdateVariant(Guid id, [FromBody] ProductVariantRequest request, CancellationToken cancellationToken)
    {
        if (!ProductConcurrency.TryParse(Request.Headers.IfMatch, "variant-", out var expectedVersion)) return StatusCode(StatusCodes.Status428PreconditionRequired);
        var errors = ProductDomainRules.ValidateVariant(request.Price, request.Currency, request.Stock).ToList();
        if (string.IsNullOrWhiteSpace(request.Sku)) errors.Add("SKU is required.");
        if (errors.Count > 0) return AttributeProblem(errors);
        var current = await productRepository.GetVariantByIdAsync(id, cancellationToken);
        if (current is null) return NotFound();
        var updated = await productRepository.UpdateVariantAsync(id, new ProductVariant { Id = id, ProductId = current.ProductId, Sku = request.Sku.Trim().ToUpperInvariant(), Price = request.Price, Currency = request.Currency.Trim().ToUpperInvariant(), StockOnHand = request.Stock }, expectedVersion, cancellationToken);
        if (updated is null) return NotFound();
        Response.Headers.ETag = ProductConcurrency.ForVariant(updated.RowVersion);
        return Ok(new ProductVariantResponse(updated.Id, updated.Sku, updated.Price, updated.Currency, updated.StockOnHand));
    }

    private ActionResult AttributeProblem(IEnumerable<string> errors) => ValidationProblem(new ValidationProblemDetails(errors.Select((error, index) => new KeyValuePair<string, string[]>(index.ToString(), new[] { error })).ToDictionary(item => item.Key, item => item.Value)));

    private static string EncodeCursor(ProductCursor cursor) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{cursor.Sort}|{cursor.SortKey}|{cursor.UpdatedUtc:O}|{cursor.Id}"));

    private static bool TryParseCursor(string? value, out ProductCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        try
        {
            var parts = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value)).Split('|');
            if (parts.Length != 4 || !DateTime.TryParse(parts[2], null, System.Globalization.DateTimeStyles.RoundtripKind, out var date) || !Guid.TryParse(parts[3], out var id)) return false;
            cursor = new ProductCursor(parts[0], parts[1], date, id);
            return true;
        }
        catch (FormatException) { return false; }
    }
}
