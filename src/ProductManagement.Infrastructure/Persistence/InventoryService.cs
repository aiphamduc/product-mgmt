using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Core.Entities;
using ProductManagement.Core.Interfaces;

namespace ProductManagement.Infrastructure.Persistence;

public sealed class InventoryService : IInventoryService
{
    private readonly ProductDbContext dbContext;

    public InventoryService(ProductDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<StockAdjustmentResult> AdjustStockAsync(Guid variantId, StockAdjustmentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Quantity == 0) throw new ArgumentException("Stock adjustment quantity cannot be zero.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Reason)) throw new ArgumentException("Adjustment reason is required.", nameof(command));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var replay = await dbContext.StockAdjustments.SingleOrDefaultAsync(item => item.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (replay is not null)
        {
            if (replay.ProductVariantId != variantId || replay.Quantity != command.Quantity || !string.Equals(replay.Reason, command.Reason, StringComparison.Ordinal))
            {
                throw new DuplicateResourceException("The idempotency key was already used for a different stock adjustment.");
            }

            await transaction.CommitAsync(cancellationToken);
            return new StockAdjustmentResult(replay.Id, replay.ProductVariantId, replay.PreviousStock, replay.NewStock, true);
        }

        var variant = await dbContext.ProductVariants.SingleOrDefaultAsync(item => item.Id == variantId, cancellationToken);
        if (variant is null) throw new KeyNotFoundException("Variant was not found.");
        int newStock;
        try
        {
            newStock = checked(variant.StockOnHand + command.Quantity);
        }
        catch (OverflowException)
        {
            throw new InvalidOperationException("Stock adjustment exceeds the supported range.");
        }

        if (newStock < 0) throw new InvalidOperationException("Stock cannot become negative.");
        var adjustment = new StockAdjustment
        {
            Id = Guid.NewGuid(), ProductVariantId = variantId, Quantity = command.Quantity, Reason = command.Reason.Trim(),
            IdempotencyKey = command.IdempotencyKey.Trim(), ActorId = command.ActorId, PreviousStock = variant.StockOnHand,
            NewStock = newStock, CreatedUtc = DateTime.UtcNow
        };
        variant.StockOnHand = newStock;
        dbContext.StockAdjustments.Add(adjustment);
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(), Type = "StockAdjusted", CreatedUtc = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(new { variantId, adjustmentId = adjustment.Id })
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new StockAdjustmentResult(adjustment.Id, variantId, adjustment.PreviousStock, adjustment.NewStock, false);
    }
}
