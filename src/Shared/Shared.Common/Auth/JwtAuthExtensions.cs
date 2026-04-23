using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Common.Auth;

/// <summary>
/// Extension methods that wire up JWT Bearer authentication and permission-based
/// authorization policies.  Every microservice calls a single method:
///   builder.Services.AddTicketingJwtAuth(builder.Configuration);
/// </summary>
public static class JwtAuthExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication (RS256 public-key validation) and all
    /// authorization policies defined in <see cref="AuthPolicies"/>.
    /// </summary>
    public static IServiceCollection AddTicketingJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? new JwtSettings();

        // Load the RSA public key for token validation
        var rsa = RSA.Create();
        var publicKeyPath = jwtSettings.PublicKeyPath;

        if (File.Exists(publicKeyPath))
        {
            var publicKeyPem = File.ReadAllText(publicKeyPath);
            rsa.ImportFromPem(publicKeyPem);
        }

        var rsaSecurityKey = new RsaSecurityKey(rsa);

        // ── Authentication ────────────────────────────────────────────────────
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

                // Map the "sub" claim to ClaimTypes.NameIdentifier
                NameClaimType = "sub",
                RoleClaimType = "role"
            };

            // Return ApiResponse-compatible JSON for 401/403 errors
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

        // ── Authorization Policies ────────────────────────────────────────────
        services.AddAuthorization(options =>
        {
            // Catalog
            options.AddPolicy(AuthPolicies.CatalogRead, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.CatalogRead));

            options.AddPolicy(AuthPolicies.CatalogWrite, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.CatalogWrite));

            // Basket
            options.AddPolicy(AuthPolicies.BasketAccess, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.BasketRead));

            options.AddPolicy(AuthPolicies.BasketCheckout, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.BasketCheckout));

            // Payment
            options.AddPolicy(AuthPolicies.PaymentRead, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.PaymentRead));

            options.AddPolicy(AuthPolicies.PaymentManage, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.PaymentManage));

            // User Management
            options.AddPolicy(AuthPolicies.UserManage, policy =>
                policy.RequireClaim(AppPermissions.PermissionClaimType, AppPermissions.UserManage));
        });

        return services;
    }
}
