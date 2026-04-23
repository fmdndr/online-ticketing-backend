using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Common.Auth;

public static class JwtAuthExtensions
{
    public static IServiceCollection AddTicketingJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? new JwtSettings();

        var rsa = RSA.Create();
        var publicKeyPath = jwtSettings.PublicKeyPath;

        if (File.Exists(publicKeyPath))
        {
            var publicKeyPem = File.ReadAllText(publicKeyPath);
            rsa.ImportFromPem(publicKeyPem);
        }

        var rsaSecurityKey = new RsaSecurityKey(rsa);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = rsaSecurityKey,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30), // tight clock skew

                NameClaimType = "sub",
                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        isSuccess = false,
                        message = "Authentication required. Please provide a valid JWT token.",
                        data = (object?)null
                    });
                    return context.Response.WriteAsync(json);
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        isSuccess = false,
                        message = "Access denied. You do not have the required permissions.",
                        data = (object?)null
                    });
                    return context.Response.WriteAsync(json);
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.CatalogRead, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.CatalogRead));

            options.AddPolicy(AuthPolicies.CatalogWrite, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.CatalogWrite));

            options.AddPolicy(AuthPolicies.BasketAccess, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.BasketRead));

            options.AddPolicy(AuthPolicies.BasketCheckout, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.BasketCheckout));

            options.AddPolicy(AuthPolicies.PaymentRead, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.PaymentRead));

            options.AddPolicy(AuthPolicies.PaymentManage, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.PaymentManage));

            options.AddPolicy(AuthPolicies.UserManage, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.UserManage));
        });

        return services;
    }
}
