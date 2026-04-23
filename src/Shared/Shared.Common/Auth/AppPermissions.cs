namespace Shared.Common.Auth;

public static class AppPermissions
{
    public const string CatalogRead = "catalog:read";
    public const string CatalogWrite = "catalog:write";
    public const string CatalogDelete = "catalog:delete";

    public const string BasketRead = "basket:read";
    public const string BasketWrite = "basket:write";
    public const string BasketCheckout = "basket:checkout";

    public const string PaymentRead = "payment:read";
    public const string PaymentManage = "payment:manage";

    public const string UserRead = "user:read";
    public const string UserManage = "user:manage";

    public const string PermissionClaimType = "permissions";

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
