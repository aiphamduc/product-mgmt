using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductManagement.Core.Interfaces;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;

    public OutboxProcessor(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                var cache = scope.ServiceProvider.GetRequiredService<IProductCache>();
                var messages = await dbContext.OutboxMessages
                    .Where(message => message.ProcessedUtc == null)
                    .OrderBy(message => message.CreatedUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);
                foreach (var message in messages)
                {
                    await cache.AdvanceVersionAsync(stoppingToken);
                    message.ProcessedUtc = DateTime.UtcNow;
                }
                if (messages.Count > 0) await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception) { }

            try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }
    }
}
