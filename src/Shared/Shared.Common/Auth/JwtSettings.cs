namespace Shared.Common.Auth;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// Bound from the "Jwt" section in appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Token issuer (Identity.API service URL).</summary>
    public string Issuer { get; set; } = "ticketing-identity";

    /// <summary>Token audience (all ticketing services).</summary>
    public string Audience { get; set; } = "ticketing-services";

    /// <summary>Access token lifetime in minutes. Default: 15 minutes.</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days. Default: 7 days.</summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>Path to the RSA public key PEM file (used by all services for validation).</summary>
    public string PublicKeyPath { get; set; } = "keys/rsa-public.pem";

    /// <summary>Path to the RSA private key PEM file (used only by Identity.API for signing).</summary>
    public string PrivateKeyPath { get; set; } = "keys/rsa-private.pem";
}
