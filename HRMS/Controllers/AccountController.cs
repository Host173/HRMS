using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Models;
using HRMS.Services;

namespace HRMS.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAccountService _authService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService authService, 
        IEmployeeService employeeService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _employeeService = employeeService;
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
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
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

            _logger.LogInformation("New user registered: {Email}", model.Email);

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
            _logger.LogError(ex, "Error registering user: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
            return View(model);
        }

        // If we get here, registration succeeded but login failed
        TempData["SuccessMessage"] = "Registration successful! Please log in.";
        return RedirectToAction("Login", "Account");
    }
}

