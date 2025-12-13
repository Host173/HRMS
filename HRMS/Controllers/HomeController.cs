using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;

namespace HRMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HrmsDbContext _context;

    public HomeController(ILogger<HomeController> logger, HrmsDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var employeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (employeeId.HasValue)
        {
            var roles = await AuthorizationHelper.GetEmployeeRolesAsync(_context, employeeId.Value);
            ViewBag.UserRoles = roles;
            ViewBag.IsSystemAdmin = await AuthorizationHelper.IsSystemAdminAsync(_context, employeeId.Value);
            ViewBag.IsHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, employeeId.Value);
            ViewBag.IsLineManager = await AuthorizationHelper.IsLineManagerAsync(_context, employeeId.Value);
            
            // Get current employee for display
            var employee = await _context.Employee
                .Include(e => e.department)
                .Include(e => e.position)
                .FirstOrDefaultAsync(e => e.employee_id == employeeId.Value);
            ViewBag.CurrentEmployee = employee;
        }
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}