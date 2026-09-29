using Hellang.Middleware.ProblemDetails;
using ProductManagement.Core.Interfaces;

namespace ProductManagement.API.Common.ProblemDetails;

public static class ApiProblemDetailsExtensions
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.Map<DuplicateResourceException>((context, exception) => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Resource conflict",
                Detail = exception.Message,
                Extensions = { ["code"] = "duplicate_resource" }
            });
            options.Map<ConcurrencyConflictException>((context, exception) => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status412PreconditionFailed,
                Title = "Concurrency conflict",
                Detail = exception.Message,
                Extensions = { ["code"] = "stale_version" }
            });
            options.Map<KeyNotFoundException>((context, exception) => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = exception.Message,
                Extensions = { ["code"] = "not_found" }
            });
            options.Map<ArgumentException>((context, exception) => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = exception.Message,
                Extensions = { ["code"] = "invalid_request" }
            });
            options.Map<InvalidOperationException>((context, exception) => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid operation",
                Detail = exception.Message,
                Extensions = { ["code"] = "invalid_operation" }
            });
        });
        return services;
    }
}
