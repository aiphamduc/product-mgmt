using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ProductManagement.API.Common.Policies;

namespace ProductManagement.API.Common.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Authentication:Authority"];
                options.Audience = configuration["Authentication:Audience"];
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(options.Authority),
                    ValidateAudience = !string.IsNullOrWhiteSpace(options.Audience)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AdminOnly, policy =>
                policy.RequireAuthenticatedUser().RequireRole("Admin"));
            options.AddPolicy("InventoryAdjust", policy =>
                policy.RequireAuthenticatedUser().RequireClaim("permission", "inventory:adjust"));
        });

        return services;
    }
}
