using Microsoft.AspNetCore.Identity;

namespace Identity.API.Entities;

/// <summary>
/// Application user extending ASP.NET Identity's IdentityUser.
/// </summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
