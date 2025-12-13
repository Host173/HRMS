using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;

namespace HRMS.ViewComponents;

public class ProfileImageViewComponent : ViewComponent
{
    private readonly HrmsDbContext _context;

    public ProfileImageViewComponent(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var profileImage = "";
        var initial = "U";
        
        if (User?.Identity?.IsAuthenticated == true)
        {
            var claimsPrincipal = User as ClaimsPrincipal ?? new ClaimsPrincipal(User.Identity);
            var employeeId = AuthorizationHelper.GetCurrentEmployeeId(claimsPrincipal);
            if (employeeId.HasValue)
            {
                var employee = await _context.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.employee_id == employeeId.Value);
                
                if (employee != null)
                {
                    profileImage = employee.profile_image ?? "";
                    initial = employee.first_name?.Substring(0, 1).ToUpper() ?? 
                             employee.email?.Substring(0, 1).ToUpper() ?? "U";
                }
            }
        }

        return View(new ProfileImageViewModel
        {
            ProfileImage = profileImage,
            Initial = initial
        });
    }
}

public class ProfileImageViewModel
{
    public string ProfileImage { get; set; } = "";
    public string Initial { get; set; } = "U";
}

