using Identity.API.DTOs;
using Identity.API.Entities;

namespace Identity.API.Services;

public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT access token for the given user and roles.
    /// </summary>
    string GenerateAccessToken(AppUser user, IList<string> roles);

    /// <summary>
    /// Creates and persists a new refresh token for the given user.
    /// </summary>
    Task<RefreshToken> GenerateRefreshTokenAsync(string userId);

    /// <summary>
    /// Validates a refresh token and rotates it (revokes old, creates new).
    /// Returns the new refresh token, or null if the provided token is invalid.
    /// </summary>
    Task<RefreshToken?> RotateRefreshTokenAsync(string token);
}
