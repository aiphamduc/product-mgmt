using Hellang.Middleware.ProblemDetails;

namespace ProductManagement.API.Common.Middleware;

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseProblemDetails();
    }
}
