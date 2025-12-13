using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HRMS.Data;
using HRMS.Helpers;

namespace HRMS.Helpers;

/// <summary>
/// Custom authorization attribute that checks if user has specific role
/// </summary>
public class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _requiredRole;

    public RequireRoleAttribute(string requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(user);

        if (employeeId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<HrmsDbContext>();
        var hasRole = await AuthorizationHelper.HasRoleAsync(dbContext, employeeId.Value, _requiredRole);

        if (!hasRole)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

