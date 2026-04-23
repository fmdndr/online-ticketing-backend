using Identity.API.DTOs;
using Identity.API.Entities;

namespace Identity.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user, IList<string> roles);

    Task<RefreshToken> GenerateRefreshTokenAsync(string userId);

    Task<RefreshToken?> RotateRefreshTokenAsync(string token);
}
