using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductManagement.API.Products.Dtos;
using ProductManagement.Core.Interfaces;
using ProductManagement.Infrastructure.Persistence;
using Xunit;

namespace ProductManagement.IntegrationTests;

public sealed class ProductApiTests : IClassFixture<ProductApiFixture>
{
    private readonly HttpClient client;
    private readonly ProductApiFixture fixture;

    public ProductApiTests(ProductApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient();
    }

    [Fact]
    public async Task CreateGetAndUpdateRequireAndReturnEtags()
    {
        var slug = $"phase-three-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/products", Product(slug));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var location = response.Headers.Location!.ToString();
        var get = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(response.Headers.ETag, get.Headers.ETag);

        var body = Product(slug);
        var withoutPrecondition = await client.PutAsJsonAsync(location, body);
        Assert.Equal(HttpStatusCode.PreconditionRequired, withoutPrecondition.StatusCode);

        using var staleRequest = new HttpRequestMessage(HttpMethod.Put, location)
        {
            Content = JsonContent.Create(body)
        };
        staleRequest.Headers.TryAddWithoutValidation("If-Match", "\"product-AQ==\"");
        var staleResponse = await client.SendAsync(staleRequest);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleResponse.StatusCode);
    }

    [Fact]
    public async Task DuplicateSlugReturnsConflict()
    {
        var slug = $"duplicate-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/products", Product(slug))).StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/v1/products", Product(slug));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task ListHonorsLimitAndCursor()
    {
        await client.PostAsJsonAsync("/api/v1/products", Product($"list-a-{Guid.NewGuid():N}"));
        await client.PostAsJsonAsync("/api/v1/products", Product($"list-b-{Guid.NewGuid():N}"));

        var first = await client.GetFromJsonAsync<JsonElement>("/api/v1/products?limit=1");
        Assert.Equal(1, first.GetProperty("items").GetArrayLength());
        var cursor = first.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(cursor));

        var second = await client.GetFromJsonAsync<JsonElement>($"/api/v1/products?limit=1&cursor={Uri.EscapeDataString(cursor!)}");
        Assert.Equal(1, second.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task InvalidCursorReturnsBadRequest()
    {
        var response = await client.GetAsync("/api/v1/products?cursor=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProductCanBePublishedWithCurrentEtag()
    {
        var created = await client.PostAsJsonAsync("/api/v1/products", Product($"publish-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location!.ToString();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{location}/status")
        {
            Content = JsonContent.Create(new { status = "Published" })
        };
        request.Headers.TryAddWithoutValidation("If-Match", created.Headers.ETag!.Tag);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Published", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task StockAdjustmentIsIdempotentAndCannotGoNegative()
    {
        var created = await client.PostAsJsonAsync("/api/v1/products", Product($"stock-{Guid.NewGuid():N}"));
        var createdBody = await created.Content.ReadFromJsonAsync<ProductResponse>();
        var variantId = createdBody!.Variants.Single().Id;
        using var scope = fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var key = $"stock-key-{Guid.NewGuid():N}";
        var first = await service.AdjustStockAsync(variantId, new StockAdjustmentCommand(-1, "sale", key));
        var replay = await service.AdjustStockAsync(variantId, new StockAdjustmentCommand(-1, "sale", key));

        Assert.False(first.WasReplay);
        Assert.True(replay.WasReplay);
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        Assert.True(await dbContext.OutboxMessages.AnyAsync(message => message.Type == "StockAdjusted"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdjustStockAsync(variantId, new StockAdjustmentCommand(-2, "oversell", $"negative-{Guid.NewGuid():N}")));
    }

    [Fact]
    public async Task StockAdjustmentRequiresAuthentication()
    {
        var response = await client.PostAsJsonAsync($"/api/v1/variants/{Guid.NewGuid()}/stock-adjustments", new StockAdjustmentRequest(1, "receipt"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static ProductRequest Product(string slug) => new(
        "Phase Three Product",
        slug,
        "Integration test product",
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Array.Empty<ProductAttributeRequest>(),
        new[] { new ProductVariantRequest($"SKU-{Guid.NewGuid():N}", 10, "USD", 2) });
}

public sealed class ProductApiFixture : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ProductDbContext>().Database.Migrate();
        return host;
    }
}
