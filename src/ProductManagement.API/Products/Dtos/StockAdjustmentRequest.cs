namespace ProductManagement.API.Products.Dtos;

public sealed record StockAdjustmentRequest(int Quantity, string Reason);
