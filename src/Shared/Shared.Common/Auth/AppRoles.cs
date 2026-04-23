namespace Shared.Common.Auth;

/// <summary>
/// Role constants used across all microservices.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Seller = "Seller";

    /// <summary>All defined roles for seeding.</summary>
    public static readonly string[] All = { Admin, User, Seller };
}
