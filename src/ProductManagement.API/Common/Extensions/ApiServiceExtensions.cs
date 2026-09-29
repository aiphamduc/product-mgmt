using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.API.Products.Validators;

namespace ProductManagement.API.Common.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    public static IServiceCollection AddApiValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ProductRequestValidator>();
        return services;
    }
}
