using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProductManagement.API.Common;

[ApiController]
[AllowAnonymous]
[Route("api/v1/status")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "product-management-api", status = "ok" });
}
