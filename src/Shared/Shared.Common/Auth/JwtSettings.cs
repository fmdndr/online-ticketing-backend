namespace Shared.Common.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ticketing-identity";

    public string Audience { get; set; } = "ticketing-services";

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;

    public string PublicKeyPath { get; set; } = "keys/rsa-public.pem";

    public string PrivateKeyPath { get; set; } = "keys/rsa-private.pem";
}
