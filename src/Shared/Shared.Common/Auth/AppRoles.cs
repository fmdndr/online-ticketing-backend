namespace Shared.Common.Auth;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Seller = "Seller";

    public static readonly string[] All = { Admin, User, Seller };
}
