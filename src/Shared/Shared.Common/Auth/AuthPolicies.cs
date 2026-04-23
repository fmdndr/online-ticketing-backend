namespace Shared.Common.Auth;

/// <summary>
/// Authorization policy names used with [Authorize(Policy = "...")] attributes.
/// Each policy requires the caller's JWT to contain the corresponding permission claim.
/// </summary>
public static class AuthPolicies
{
    // ── Catalog ────────────────────────────────────────
    public const string CatalogRead = "CatalogRead";
    public const string CatalogWrite = "CatalogWrite";

    // ── Basket ─────────────────────────────────────────
    public const string BasketAccess = "BasketAccess";
    public const string BasketCheckout = "BasketCheckout";

    // ── Payment ────────────────────────────────────────
    public const string PaymentRead = "PaymentRead";
    public const string PaymentManage = "PaymentManage";

    // ── User Management ────────────────────────────────
    public const string UserManage = "UserManage";
}
