namespace Shared.Common.Auth;

/// <summary>
/// Fine-grained permission constants and role-to-permission mapping.
/// Permissions are embedded as claims in the JWT and enforced via authorization policies.
/// </summary>
public static class AppPermissions
{
    // ── Catalog ───────────────────────────────────────────────
    public const string CatalogRead = "catalog:read";
    public const string CatalogWrite = "catalog:write";
    public const string CatalogDelete = "catalog:delete";

    // ── Basket ────────────────────────────────────────────────
    public const string BasketRead = "basket:read";
    public const string BasketWrite = "basket:write";
    public const string BasketCheckout = "basket:checkout";

    // ── Payment ───────────────────────────────────────────────
    public const string PaymentRead = "payment:read";
    public const string PaymentManage = "payment:manage";

    // ── User Management ───────────────────────────────────────
    public const string UserRead = "user:read";
    public const string UserManage = "user:manage";

    /// <summary>Custom claim type used to embed permissions in the JWT.</summary>
    public const string PermissionClaimType = "permissions";

    /// <summary>Maps each role to the permissions it grants.</summary>
    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        [AppRoles.Admin] = new[]
        {
            CatalogRead, CatalogWrite, CatalogDelete,
            BasketRead, BasketWrite, BasketCheckout,
            PaymentRead, PaymentManage,
            UserRead, UserManage
        },
        [AppRoles.Seller] = new[]
        {
            CatalogRead, CatalogWrite, CatalogDelete,
            PaymentRead
        },
        [AppRoles.User] = new[]
        {
            CatalogRead,
            BasketRead, BasketWrite, BasketCheckout,
            PaymentRead
        }
    };
}
