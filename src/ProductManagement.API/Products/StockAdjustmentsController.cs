using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.API.Common.Policies;
using ProductManagement.API.Products.Dtos;
using ProductManagement.Core.Interfaces;

namespace ProductManagement.API.Products;

[ApiController]
[Authorize(Policy = "InventoryAdjust")]
[Route("api/v1/variants/{variantId:guid}/stock-adjustments")]
public sealed class StockAdjustmentsController : ControllerBase
{
    private readonly IInventoryService inventoryService;

    public StockAdjustmentsController(IInventoryService inventoryService)
    {
        this.inventoryService = inventoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Dtos.StockAdjustmentResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dtos.StockAdjustmentResult>> Adjust(Guid variantId, [FromBody] StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
        {
            var problem = new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Idempotency-Key is required." };
            problem.Extensions["code"] = "missing_idempotency_key";
            return BadRequest(problem);
        }

        var result = await inventoryService.AdjustStockAsync(variantId, new StockAdjustmentCommand(request.Quantity, request.Reason, key.ToString()), cancellationToken);
        return Ok(new Dtos.StockAdjustmentResult(result.AdjustmentId, result.VariantId, result.previousStock, result.NewStock, result.WasReplay));
    }
}
