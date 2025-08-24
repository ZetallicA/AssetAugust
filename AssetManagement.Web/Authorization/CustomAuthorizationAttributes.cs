using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace AssetManagement.Web.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(params string[] roles) : base()
    {
        Roles = string.Join(",", roles);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireDepartmentAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string[] _allowedDepartments;

    public RequireDepartmentAttribute(params string[] departments)
    {
        _allowedDepartments = departments;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userDepartment = user.FindFirstValue("Department");
        
        if (string.IsNullOrEmpty(userDepartment) || !_allowedDepartments.Contains(userDepartment, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ForbidResult();
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user has the required permission
        var hasPermission = user.HasClaim(c => c.Type == "Permission" && c.Value == _permission);
        
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireActiveUserAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user is active (you might want to check this from the database)
        var isActive = user.FindFirstValue("IsActive");
        
        if (string.IsNullOrEmpty(isActive) || isActive.ToLower() != "true")
        {
            context.Result = new ForbidResult();
        }
    }
}
