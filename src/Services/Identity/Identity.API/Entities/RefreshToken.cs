using System.ComponentModel.DataAnnotations;

namespace Identity.API.Entities;

/// <summary>
/// Represents a refresh token stored in the database for token rotation.
/// </summary>
public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The opaque refresh token string.</summary>
    public string Token { get; set; } = null!;

    /// <summary>The user this token belongs to.</summary>
    public string UserId { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    /// <summary>True if this token has been explicitly revoked.</summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>True if the token has passed its expiry date.</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>True if the token is still valid (not revoked and not expired).</summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
