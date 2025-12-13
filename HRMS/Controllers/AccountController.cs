using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Models;
using HRMS.Services;
using HRMS.Data;
using HRMS.Helpers;

namespace HRMS.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAccountService _authService;
    private readonly IEmployeeService _employeeService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService authService, 
        IEmployeeService employeeService,
        HrmsDbContext context,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _employeeService = employeeService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Validate user credentials
        var isValid = await _authService.ValidateUserAsync(model.Username, model.Password);
        
        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        // Get user information
        var user = await _authService.GetUserByUsernameAsync(model.Username);
        
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            return View(model);
        }

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.FullName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
        }

        var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            "CookieAuth",
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        // Redirect to return URL or home page
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        _logger.LogInformation("User logged out");
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        var model = new RegisterViewModel();
        
        // Only System Admins, HR Admins, and Line Managers can create personal accounts
        // Employee role is not available in registration
        model.AvailableRoles = new List<string>
        {
            AuthorizationHelper.SystemAdminRole,
            AuthorizationHelper.HRAdminRole,
            AuthorizationHelper.LineManagerRole
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Only System Admins, HR Admins, and Line Managers can create personal accounts
        model.AvailableRoles = new List<string>
        {
            AuthorizationHelper.SystemAdminRole,
            AuthorizationHelper.HRAdminRole,
            AuthorizationHelper.LineManagerRole
        };

        // Validate role selection
        if (string.IsNullOrEmpty(model.SelectedRole))
        {
            ModelState.AddModelError(nameof(model.SelectedRole), "Please select a role.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if email already exists
        var emailExists = await _employeeService.EmailExistsAsync(model.Email);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
            return View(model);
        }

        // Create new employee
        var employee = new Employee
        {
            first_name = model.FirstName,
            last_name = model.LastName,
            full_name = $"{model.FirstName} {model.LastName}".Trim(),
            email = model.Email,
            phone = model.Phone,
            password_hash = DatabaseAuthenticationService.HashPassword(model.Password),
            is_active = true,
            account_status = "Active"
        };

        try
        {
            await _employeeService.CreateAsync(employee);

            // Assign role - always assign the selected role or default to Employee
            var roleToAssign = !string.IsNullOrEmpty(model.SelectedRole) 
                ? model.SelectedRole 
                : AuthorizationHelper.EmployeeRole;
            
            await AuthorizationHelper.AssignRoleAsync(_context, employee.employee_id, roleToAssign);

            _logger.LogInformation("New user registered: {Email} with role {Role}", model.Email, roleToAssign);

            // Automatically log in the user after registration
            var user = await _authService.GetUserByUsernameAsync(model.Email);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                };

                if (!string.IsNullOrEmpty(user.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, user.Email));
                }

                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
                }

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    "CookieAuth",
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                TempData["SuccessMessage"] = "Registration successful! You have been logged in.";
                return RedirectToAction("Index", "Home");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Email}. Error: {Error}", model.Email, ex.Message);
            
            // Reload available roles for the view
            model.AvailableRoles = new List<string>
            {
                AuthorizationHelper.SystemAdminRole,
                AuthorizationHelper.HRAdminRole,
                AuthorizationHelper.LineManagerRole
            };
            
            ModelState.AddModelError(string.Empty, $"An error occurred during registration: {ex.Message}. Please check the logs for details.");
            return View(model);
        }

        // If we get here, registration succeeded but login failed
        TempData["SuccessMessage"] = "Registration successful! Please log in.";
        return RedirectToAction("Login", "Account");
    }

    /// <summary>
    /// System Admin can create accounts for new employees
    /// </summary>
    [HttpGet]
    [Authorize]
    [RequireRole(AuthorizationHelper.SystemAdminRole)]
    public async Task<IActionResult> CreateEmployeeAccount()
    {
        var model = new RegisterViewModel
        {
            AvailableRoles = new List<string>
            {
                AuthorizationHelper.EmployeeRole,
                AuthorizationHelper.HRAdminRole,
                AuthorizationHelper.LineManagerRole,
                AuthorizationHelper.SystemAdminRole
            }
        };
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [RequireRole(AuthorizationHelper.SystemAdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployeeAccount(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = new List<string>
            {
                AuthorizationHelper.EmployeeRole,
                AuthorizationHelper.HRAdminRole,
                AuthorizationHelper.LineManagerRole,
                AuthorizationHelper.SystemAdminRole
            };
            return View(model);
        }

        // Check if email already exists
        var emailExists = await _employeeService.EmailExistsAsync(model.Email);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
            model.AvailableRoles = new List<string>
            {
                AuthorizationHelper.EmployeeRole,
                AuthorizationHelper.HRAdminRole,
                AuthorizationHelper.LineManagerRole,
                AuthorizationHelper.SystemAdminRole
            };
            return View(model);
        }

        // Create new employee
        var employee = new Employee
        {
            first_name = model.FirstName,
            last_name = model.LastName,
            full_name = $"{model.FirstName} {model.LastName}".Trim(),
            email = model.Email,
            phone = model.Phone,
            password_hash = DatabaseAuthenticationService.HashPassword(model.Password),
            is_active = true,
            account_status = "Active",
            hire_date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        try
        {
            await _employeeService.CreateAsync(employee);

            // Assign role
            var roleToAssign = !string.IsNullOrEmpty(model.SelectedRole) 
                ? model.SelectedRole 
                : AuthorizationHelper.EmployeeRole;
            
            await AuthorizationHelper.AssignRoleAsync(_context, employee.employee_id, roleToAssign);

            _logger.LogInformation("System Admin created new employee account: {Email} with role {Role}", model.Email, roleToAssign);

            TempData["SuccessMessage"] = $"Employee account created successfully with role: {roleToAssign}";
            return RedirectToAction("Index", "Employee");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error creating employee account: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the account. Please try again.");
            model.AvailableRoles = new List<string>
            {
                AuthorizationHelper.EmployeeRole,
                AuthorizationHelper.HRAdminRole,
                AuthorizationHelper.LineManagerRole,
                AuthorizationHelper.SystemAdminRole
            };
            return View(model);
        }
    }
}

