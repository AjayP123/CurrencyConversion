using Microsoft.AspNetCore.Authorization;

namespace CurrencyConversionApi.Authorization;

/// <summary>
/// Authorization attribute for role-based access control
/// </summary>
public class RoleAuthorizeAttribute : AuthorizeAttribute
{
    public RoleAuthorizeAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

/// <summary>
/// Admin only authorization attribute
/// </summary>
public class AdminOnlyAttribute : RoleAuthorizeAttribute
{
    public AdminOnlyAttribute() : base(Models.UserRoles.Admin) { }
}

/// <summary>
/// Premium or Admin authorization attribute
/// </summary>
public class PremiumOrAdminAttribute : RoleAuthorizeAttribute
{
    public PremiumOrAdminAttribute() : base(Models.UserRoles.Premium, Models.UserRoles.Admin) { }
}

/// <summary>
/// Any authenticated user (Basic, Premium, or Admin) authorization attribute
/// </summary>
public class AuthenticatedUserAttribute : RoleAuthorizeAttribute
{
    public AuthenticatedUserAttribute() : base(Models.UserRoles.Basic, Models.UserRoles.Premium, Models.UserRoles.Admin) { }
}
