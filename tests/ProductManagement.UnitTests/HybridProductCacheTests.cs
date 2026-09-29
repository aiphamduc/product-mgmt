using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProductManagement.Infrastructure.Caching;
using Xunit;

namespace ProductManagement.UnitTests;

public sealed class HybridProductCacheTests
{
    [Fact]
    public async Task UsesMemoryFallbackAndAdvancesVersion()
    {
        var cache = new HybridProductCache(
            new MemoryCache(Options.Create(new MemoryCacheOptions())),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

        await cache.SetAsync("catalog:test", "value", TimeSpan.FromMinutes(1));

        Assert.Equal("value", await cache.GetAsync("catalog:test"));
        var initial = await cache.GetVersionAsync();
        var next = await cache.AdvanceVersionAsync();

        Assert.Equal(initial + 1, next);
    }
}
