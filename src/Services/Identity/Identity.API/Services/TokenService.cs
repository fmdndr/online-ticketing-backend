using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Identity.API.Data;
using Identity.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Common.Auth;

namespace Identity.API.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IdentityDbContext _dbContext;
    private readonly RsaSecurityKey _privateKey;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        IdentityDbContext dbContext,
        ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _dbContext = dbContext;
        _logger = logger;

        // Load the RSA private key for signing tokens
        var rsa = RSA.Create();
        var privateKeyPem = File.ReadAllText(_jwtSettings.PrivateKeyPath);
        rsa.ImportFromPem(privateKeyPem);
        _privateKey = new RsaSecurityKey(rsa);
    }

    public string GenerateAccessToken(AppUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        // Add permission claims based on roles
        var permissions = new HashSet<string>();
        foreach (var role in roles)
        {
            if (AppPermissions.RolePermissions.TryGetValue(role, out var rolePerms))
            {
                foreach (var perm in rolePerms)
                    permissions.Add(perm);
            }
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(AppPermissions.PermissionClaimType, permission));
        }

        var credentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation(
            "Access token generated for user {UserId} with roles [{Roles}], expires at {ExpiresAt}",
            user.Id, string.Join(", ", roles), expires);

        return tokenString;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Refresh token generated for user {UserId}, expires at {ExpiresAt}",
            userId, refreshToken.ExpiresAt);

        return refreshToken;
    }

    public async Task<RefreshToken?> RotateRefreshTokenAsync(string token)
    {
        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (existingToken == null)
        {
            _logger.LogWarning("Refresh token not found: {Token}", token[..Math.Min(8, token.Length)] + "...");
            return null;
        }

        if (!existingToken.IsActive)
        {
            _logger.LogWarning("Refresh token is no longer active for user {UserId} (revoked={Revoked}, expired={Expired})",
                existingToken.UserId, existingToken.IsRevoked, existingToken.IsExpired);
            return null;
        }

        // Revoke the old token
        existingToken.RevokedAt = DateTime.UtcNow;

        // Create a new refresh token
        var newRefreshToken = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = existingToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Refresh token rotated for user {UserId}", existingToken.UserId);

        return newRefreshToken;
    }

    /// <summary>
    /// Generates a cryptographically secure random token string.
    /// </summary>
    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
