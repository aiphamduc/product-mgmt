using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.API.Products.Dtos;
using ProductManagement.Infrastructure.Persistence;
using Xunit;

namespace ProductManagement.IntegrationTests;

public sealed class ReleaseReadinessTests : IClassFixture<ProductApiFixture>
{
    private readonly HttpClient client;
    private readonly ProductApiFixture fixture;

    public ReleaseReadinessTests(ProductApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient();
    }

    [Fact]
    public async Task InvalidInputNotFoundAndAllowlistErrorsUseExpectedStatuses()
    {
        var invalid = await client.PostAsJsonAsync("/api/v1/products", Product($"invalid-{Guid.NewGuid():N}", description: new string('x', 10001), variantCount: 0));
        var missing = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        var invalidSort = await client.GetAsync("/api/v1/products?sort=price:desc");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidSort.StatusCode);
    }

    [Fact]
    public async Task IfNoneMatchReturnsNotModified()
    {
        var created = await client.PostAsJsonAsync("/api/v1/products", Product($"etag-{Guid.NewGuid():N}"));
        var location = created.Headers.Location!.ToString();
        var etag = created.Headers.ETag!.Tag;
        using var request = new HttpRequestMessage(HttpMethod.Get, location);
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateSkuReturnsConflict()
    {
        var sku = $"DUP-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/products", Product($"sku-a-{Guid.NewGuid():N}", sku))).StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/v1/products", Product($"sku-b-{Guid.NewGuid():N}", sku));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task ConcurrentDuplicateSlugHasOneWinner()
    {
        var slug = $"race-{Guid.NewGuid():N}";
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/products", Product(slug, $"RACE-A-{Guid.NewGuid():N}")),
            client.PostAsJsonAsync("/api/v1/products", Product(slug, $"RACE-B-{Guid.NewGuid():N}")));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task OutboxMessageIsEventuallyProcessed()
    {
        var slug = $"outbox-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/products", Product(slug));
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        for (var attempt = 0; attempt < 30; attempt++)
        {
            using var scope = fixture.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            if (await dbContext.OutboxMessages.AnyAsync(message => message.Type == "ProductChanged" && message.Payload.Contains(product!.Id.ToString()) && message.ProcessedUtc != null)) return;
            await Task.Delay(100);
        }

        Assert.Fail("The outbox worker did not process the product event within the verification window.");
    }

    [Fact]
    public async Task HealthSwaggerCorsAndRedisFallbackPathsAreAvailable()
    {
        var health = await client.GetAsync("/health");
        var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/products");
        preflight.Headers.TryAddWithoutValidation("Origin", "http://localhost:4200");
        preflight.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");
        var cors = await client.SendAsync(preflight);
        var list = await client.GetAsync("/api/v1/products?limit=1");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, cors.StatusCode);
        Assert.Equal("http://localhost:4200", cors.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task ConcurrentStockAdjustmentsDoNotLoseUpdates()
    {
        var created = await client.PostAsJsonAsync("/api/v1/products", Product($"concurrent-stock-{Guid.NewGuid():N}"));
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var variantId = product!.Variants.Single().Id;

        var results = await Task.WhenAll(
            AdjustInScope(variantId, "concurrent-a"),
            AdjustInScope(variantId, "concurrent-b"));

        Assert.Equal(2, results.Length);
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var variant = await dbContext.ProductVariants.SingleAsync(item => item.Id == variantId);
        Assert.Equal(4, variant.StockOnHand);
    }

    private async Task<Core.Interfaces.StockAdjustmentResult> AdjustInScope(Guid variantId, string reason)
    {
        using var scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<Core.Interfaces.IInventoryService>().AdjustStockAsync(
            variantId,
            new Core.Interfaces.StockAdjustmentCommand(1, reason, $"{reason}-{Guid.NewGuid():N}"));
    }

    private static ProductRequest Product(string slug, string? sku = null, string description = "Release test product", int variantCount = 1)
    {
        var variants = Enumerable.Range(0, variantCount).Select(index => new ProductVariantRequest(sku ?? $"SKU-{Guid.NewGuid():N}-{index}", 10, "USD", 2)).ToArray();
        return new ProductRequest("Release Test Product", slug, description, Guid.Parse("11111111-1111-1111-1111-111111111111"), Array.Empty<ProductAttributeRequest>(), variants);
    }
}
