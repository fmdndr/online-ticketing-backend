namespace Shared.Common.Auth;

public static class AuthPolicies
{
    public const string CatalogRead = "CatalogRead";
    public const string CatalogWrite = "CatalogWrite";

    public const string BasketAccess = "BasketAccess";
    public const string BasketCheckout = "BasketCheckout";

    public const string PaymentRead = "PaymentRead";
    public const string PaymentManage = "PaymentManage";

    public const string UserManage = "UserManage";
}
