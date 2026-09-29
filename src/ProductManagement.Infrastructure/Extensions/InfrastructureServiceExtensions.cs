using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Core.Interfaces;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ProductManagement.Infrastructure.Caching;
using ProductManagement.Infrastructure.Outbox;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProductDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ProductDatabase")));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddMemoryCache();
        var redisConnection = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConnection)) services.AddDistributedMemoryCache();
        else services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        services.AddSingleton<IProductCache, HybridProductCache>();
        services.AddHostedService<OutboxProcessor>();
        return services;
    }
}
