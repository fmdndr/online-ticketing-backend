using System.ComponentModel.DataAnnotations;

namespace Identity.API.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [Required]
    public string FullName { get; set; } = null!;

    /// <summary>
    /// Optional role. If not specified, defaults to "User".
    /// Admin-only: can set "Seller" or "Admin" roles.
    /// </summary>
    public string? Role { get; set; }
}
