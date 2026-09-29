using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using ProductManagement.Core.Interfaces;

namespace ProductManagement.Infrastructure.Caching;

public sealed class HybridProductCache : IProductCache
{
    private const string VersionKey = "product-management:catalog:version";
    private readonly IMemoryCache memoryCache;
    private readonly IDistributedCache distributedCache;
    private long localVersion = 1;

    public HybridProductCache(IMemoryCache memoryCache, IDistributedCache distributedCache)
    {
        this.memoryCache = memoryCache;
        this.distributedCache = distributedCache;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(key, out string? memoryValue)) return memoryValue;
        try
        {
            var bytes = await WithTimeout(token => distributedCache.GetAsync(key, token), cancellationToken);
            if (bytes is null) return null;
            var value = Encoding.UTF8.GetString(bytes);
            memoryCache.Set(key, value, TimeSpan.FromSeconds(60));
            return value;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
        catch (Exception) { return null; }
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        memoryCache.Set(key, value, ttl);
        try
        {
            await WithTimeout(token => distributedCache.SetAsync(key, Encoding.UTF8.GetBytes(value), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, token), cancellationToken);
        }
        catch (Exception) { }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        try { await WithTimeout(token => distributedCache.RemoveAsync(key, token), cancellationToken); }
        catch (Exception) { }
    }

    public async Task<long> AdvanceVersionAsync(CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(cancellationToken) + 1;
        Interlocked.Exchange(ref localVersion, version);
        await SetAsync(VersionKey, version.ToString(System.Globalization.CultureInfo.InvariantCulture), TimeSpan.FromHours(24), cancellationToken);
        return version;
    }

    public async Task<long> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(VersionKey, cancellationToken);
        return long.TryParse(value, out var version) ? version : Interlocked.Read(ref localVersion);
    }

    private static async Task<T> WithTimeout<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(200));
        return await operation(timeout.Token);
    }

    private static async Task WithTimeout(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(200));
        await operation(timeout.Token);
    }
}
