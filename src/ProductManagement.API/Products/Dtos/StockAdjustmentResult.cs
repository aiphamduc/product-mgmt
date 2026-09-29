namespace ProductManagement.API.Products.Dtos;

public sealed record StockAdjustmentResult(Guid AdjustmentId, Guid VariantId, int PreviousStock, int NewStock, bool WasReplay);
