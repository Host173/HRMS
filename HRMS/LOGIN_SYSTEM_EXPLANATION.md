# Complete Login System Explanation

This document explains every step of the login system implementation so you can understand and modify it manually.

---

## 📋 Table of Contents
1. [Program.cs - Application Configuration](#1-programcs---application-configuration)
2. [LoginViewModel - Data Model](#2-loginviewmodel---data-model)
3. [IUserAuthenticationService - Service Interface](#3-iuserauthenticationservice---service-interface)
4. [AccountController - Login Logic](#4-accountcontroller---login-logic)
5. [Login View - User Interface](#5-login-view---user-interface)
6. [Home Controller Protection](#6-home-controller-protection)
7. [Layout Updates - Navigation Bar](#7-layout-updates---navigation-bar)

---

## 1. Program.cs - Application Configuration

### Location: `HRMS/Program.cs`

### What We Added:

```csharp
// Add authentication services
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
```

### Why Each Part:

- **`AddAuthentication("CookieAuth")`**: 
  - Registers authentication middleware
  - `"CookieAuth"` is the **scheme name** - a unique identifier for this authentication method
  - You can have multiple schemes (e.g., JWT, Cookies, OAuth)
  - **IMPORTANT**: This name must match everywhere you use it (in controllers)

- **`AddCookie("CookieAuth", options => ...)`**:
  - Configures cookie-based authentication
  - The first parameter must match the scheme name above
  - Cookies store authentication info in the user's browser

- **`options.LoginPath = "/Account/Login"`**:
  - Where unauthenticated users are redirected
  - When you try to access a protected page, you go here

- **`options.LogoutPath = "/Account/Logout"`**:
  - The URL that handles logout requests
  - Not used directly, but good for consistency

- **`options.AccessDeniedPath = "/Account/AccessDenied"`**:
  - Where users go if they're logged in but don't have permission
  - Useful for role-based authorization later

- **`options.ExpireTimeSpan = TimeSpan.FromHours(8)`**:
  - How long the login cookie lasts (8 hours)
  - After this time, user must log in again
  - **To change**: Modify the hours (e.g., `TimeSpan.FromDays(1)` for 24 hours)

- **`options.SlidingExpiration = true`**:
  - If user is active, cookie expiration resets
  - Example: Cookie expires in 8 hours, but if user visits after 7 hours, it resets to 8 hours again

### Service Registration:

```csharp
builder.Services.AddScoped<IUserAuthenticationService, MockAuthenticationService>();
```

- **`AddScoped`**: Creates one instance per HTTP request
- **`IUserAuthenticationService`**: The interface (contract)
- **`MockAuthenticationService`**: The implementation (temporary, for testing)
- **To change**: Replace `MockAuthenticationService` with `DatabaseAuthenticationService` when you connect your database

### Middleware Order (CRITICAL):

```csharp
app.UseRouting();           // Must be first
app.UseAuthentication();     // Must come before Authorization
app.UseAuthorization();      // Must come after Authentication
```

**Why this order matters:**
- Routing determines which controller to use
- Authentication checks if user is logged in
- Authorization checks if user has permission
- They must be in this exact order!

---

## 2. LoginViewModel - Data Model

### Location: `HRMS/Models/LoginViewModel.cs`

### What It Does:

```csharp
public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
```

### Why Each Part:

- **`[Required]`**: 
  - Validation attribute - field cannot be empty
  - Shows error message if user submits empty field
  - **To customize**: Change the `ErrorMessage` property

- **`[Display(Name = "Username")]`**:
  - What label shows in the form
  - If omitted, uses property name ("Username")

- **`[DataType(DataType.Password)]`**:
  - Makes password field hide characters (shows dots)
  - Browser security feature

- **`RememberMe`**:
  - Boolean checkbox
  - If checked, cookie lasts 30 days instead of 8 hours
  - See `AccountController` line 76 for how it's used

### How to Modify:

- **Add more fields**: Add properties with validation attributes
- **Change validation**: Add `[StringLength(50)]`, `[EmailAddress]`, etc.
- **Change error messages**: Modify `ErrorMessage` in attributes

---

## 3. IUserAuthenticationService - Service Interface

### Location: `HRMS/Services/IAuthenticationService.cs`

### What It Does:

```csharp
public interface IUserAuthenticationService
{
    Task<bool> ValidateUserAsync(string username, string password);
    Task<User?> GetUserByUsernameAsync(string username);
}
```

### Why We Use an Interface:

- **Separation of concerns**: Controller doesn't care HOW authentication works
- **Easy to swap**: Can switch from mock to database without changing controller
- **Testable**: Can create fake implementations for testing

### The Two Methods:

1. **`ValidateUserAsync`**: 
   - Checks if username/password combination is correct
   - Returns `true` if valid, `false` if not
   - **When you add database**: Query your Users table here

2. **`GetUserByUsernameAsync`**:
   - Gets full user information after validation
   - Returns `User` object with Id, Username, Email, FullName
   - Used to create claims (user identity info)

### User Class:

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
}
```

- **`?` after `string`**: Makes it nullable (optional field)
- **To add fields**: Add properties here, then update database service

---

## 4. AccountController - Login Logic

### Location: `HRMS/Controllers/AccountController.cs`

### Constructor (Dependency Injection):

```csharp
private readonly IUserAuthenticationService _authService;
private readonly ILogger<AccountController> _logger;

public AccountController(IUserAuthenticationService authService, ILogger<AccountController> logger)
{
    _authService = authService;
    _logger = logger;
}
```

**Why:**
- ASP.NET Core automatically provides these services
- `_authService`: Handles user validation
- `_logger`: For logging events (useful for debugging)

### GET Login Action (Show Form):

```csharp
[HttpGet]
public IActionResult Login(string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;
    return View();
}
```

**What it does:**
- `[HttpGet]`: Handles GET requests (when user visits the page)
- `returnUrl`: If user was redirected here, save where they came from
- `ViewData["ReturnUrl"]`: Passes data to the view
- Returns the Login.cshtml view

### POST Login Action (Process Form):

#### Step 1: Validate Model

```csharp
if (!ModelState.IsValid)
{
    return View(model);
}
```

- Checks if form validation passed (Required fields, etc.)
- If invalid, shows form again with error messages

#### Step 2: Validate Credentials

```csharp
var isValid = await _authService.ValidateUserAsync(model.Username, model.Password);

if (!isValid)
{
    ModelState.AddModelError(string.Empty, "Invalid username or password.");
    return View(model);
}
```

- Calls service to check username/password
- If invalid, adds error and shows form again
- `string.Empty` means error applies to entire form, not specific field

#### Step 3: Get User Information

```csharp
var user = await _authService.GetUserByUsernameAsync(model.Username);

if (user == null)
{
    ModelState.AddModelError(string.Empty, "User not found.");
    return View(model);
}
```

- Gets full user details
- Safety check (shouldn't happen if ValidateUserAsync worked)

#### Step 4: Create Claims

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
};

if (!string.IsNullOrEmpty(user.Email))
{
    claims.Add(new Claim(ClaimTypes.Email, user.Email));
}
```

**What are Claims?**
- Pieces of information about the user
- Stored in the authentication cookie
- Can be accessed anywhere: `User.Identity.Name`, `User.FindFirst(ClaimTypes.Email)`

**Common ClaimTypes:**
- `ClaimTypes.Name`: Username
- `ClaimTypes.NameIdentifier`: User ID
- `ClaimTypes.Email`: Email address
- `ClaimTypes.Role`: User role (for authorization)

**To add more claims**: Add more `claims.Add(...)` lines

#### Step 5: Create Claims Identity

```csharp
var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
```

- Combines all claims into an identity
- **"CookieAuth"**: MUST match the scheme name in Program.cs!
- This is what we fixed earlier - it was using default "Cookies" instead

#### Step 6: Set Cookie Properties

```csharp
var authProperties = new AuthenticationProperties
{
    IsPersistent = model.RememberMe,
    ExpiresUtc = model.RememberMe 
        ? DateTimeOffset.UtcNow.AddDays(30) 
        : DateTimeOffset.UtcNow.AddHours(8)
};
```

- `IsPersistent`: If true, cookie survives browser restart
- `ExpiresUtc`: When cookie expires
  - If "Remember Me" checked: 30 days
  - If not checked: 8 hours (matches Program.cs setting)

#### Step 7: Sign In User

```csharp
await HttpContext.SignInAsync(
    "CookieAuth",
    new ClaimsPrincipal(claimsIdentity),
    authProperties);
```

- Creates the authentication cookie
- **"CookieAuth"**: MUST match scheme name!
- Stores claims in cookie
- Browser will send this cookie with every request

#### Step 8: Redirect

```csharp
if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
{
    return Redirect(returnUrl);
}

return RedirectToAction("Index", "Home");
```

- If user was redirected here, send them back
- `Url.IsLocalUrl`: Security check (prevents redirect to external sites)
- Otherwise, go to home page

### Logout Action:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await HttpContext.SignOutAsync("CookieAuth");
    _logger.LogInformation("User logged out");
    return RedirectToAction("Login", "Account");
}
```

- `[ValidateAntiForgeryToken]`: Prevents CSRF attacks
- `SignOutAsync("CookieAuth")`: Deletes the authentication cookie
- Redirects to login page

**Why POST?** Logout should be POST (not GET) for security - prevents accidental logout from links

---

## 5. Login View - User Interface

### Location: `HRMS/Views/Account/Login.cshtml`

### Key Parts:

```csharp
@model HRMS.Models.LoginViewModel
```
- Tells view what data type it receives
- Enables `asp-for` helpers

```html
<form asp-action="Login" asp-controller="Account" method="post">
```
- `asp-action`: Which action method to call (Login)
- `asp-controller`: Which controller (Account)
- `method="post"`: Sends POST request (triggers [HttpPost] action)

```html
<div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>
```
- Shows validation errors at top of form
- `ModelOnly`: Only shows errors not tied to specific fields

```html
<input asp-for="Username" class="form-control" autocomplete="username" />
<span asp-validation-for="Username" class="text-danger"></span>
```
- `asp-for="Username"`: Binds to `LoginViewModel.Username`
- `asp-validation-for`: Shows field-specific errors
- `autocomplete="username"`: Browser autofill hint

```html
@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```
- Includes jQuery validation scripts
- Enables client-side validation (shows errors before submitting)

### To Customize:

- **Change styling**: Modify Bootstrap classes (`btn-primary`, `card`, etc.)
- **Add fields**: Add more input fields with `asp-for`
- **Change layout**: Modify HTML structure

---

## 6. Home Controller Protection

### Location: `HRMS/Controllers/HomeController.cs`

### What We Added:

```csharp
[Authorize]
public class HomeController : Controller
{
    // ... existing code
}
```

**What `[Authorize]` does:**
- Requires user to be logged in
- If not logged in, redirects to `/Account/Login`
- Can be applied to:
  - Entire controller: `[Authorize]` above class
  - Specific action: `[Authorize]` above method

**To protect specific actions only:**
```csharp
public class HomeController : Controller
{
    [Authorize]
    public IActionResult Index() { ... }
    
    public IActionResult Privacy() { ... }  // No login required
}
```

**To allow anonymous access:**
```csharp
[Authorize]
public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Privacy() { ... }  // No login required
}
```

---

## 7. Layout Updates - Navigation Bar

### Location: `HRMS/Views/Shared/_Layout.cshtml`

### What We Added:

```html
@if (User.Identity?.IsAuthenticated == true)
{
    <span class="navbar-text me-3">Welcome, @User.Identity.Name!</span>
    <form asp-controller="Account" asp-action="Logout" method="post">
        <button type="submit" class="btn btn-link nav-link text-dark">Logout</button>
    </form>
}
else
{
    <a class="nav-link text-dark" asp-controller="Account" asp-action="Login">Login</a>
}
```

**How it works:**
- `User.Identity?.IsAuthenticated`: Checks if user is logged in
- `?`: Null-safe operator (prevents error if User is null)
- If logged in: Shows welcome message and logout button
- If not logged in: Shows login link

**To customize:**
- Change welcome message text
- Add user email: `@User.FindFirst(ClaimTypes.Email)?.Value`
- Add user roles: `@User.IsInRole("Admin")`

---

## 🔧 Common Modifications

### Change Cookie Expiration Time:

**In Program.cs:**
```csharp
options.ExpireTimeSpan = TimeSpan.FromDays(1);  // 24 hours
```

**In AccountController (line 76):**
```csharp
ExpiresUtc = model.RememberMe 
    ? DateTimeOffset.UtcNow.AddDays(60)  // Change from 30
    : DateTimeOffset.UtcNow.AddDays(1)    // Change from 8 hours
```

### Change Authentication Scheme Name:

**In Program.cs:**
```csharp
builder.Services.AddAuthentication("MyCustomScheme")  // Change here
    .AddCookie("MyCustomScheme", options => { ... });  // And here
```

**In AccountController:**
```csharp
var claimsIdentity = new ClaimsIdentity(claims, "MyCustomScheme");  // Change here
await HttpContext.SignInAsync("MyCustomScheme", ...);  // And here
await HttpContext.SignOutAsync("MyCustomScheme");  // And here
```

**IMPORTANT**: All three places must match!

### Add More User Information to Claims:

**In AccountController (after line 60):**
```csharp
claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
claims.Add(new Claim("CustomClaim", "CustomValue"));  // Custom claim
```

**To use in views:**
```html
@User.FindFirst("CustomClaim")?.Value
```

### Change Login Redirect:

**In AccountController (line 92):**
```csharp
return RedirectToAction("Dashboard", "Home");  // Change action/controller
// OR
return Redirect("/Dashboard");  // Direct URL
```

---

## 🐛 Troubleshooting

### "No sign-in authentication handler is registered"
- **Cause**: Scheme name mismatch
- **Fix**: Make sure "CookieAuth" matches in Program.cs and AccountController

### Login works but redirects back to login
- **Cause**: Middleware order wrong
- **Fix**: Check `UseAuthentication()` comes before `UseAuthorization()`

### Validation errors not showing
- **Cause**: Missing validation scripts
- **Fix**: Check `_ValidationScriptsPartial` is included in view

### Cookie not persisting
- **Cause**: Cookie settings or browser blocking
- **Fix**: Check `IsPersistent` and `ExpiresUtc` in AccountController

---

## 📝 Summary

1. **Program.cs**: Configures authentication scheme and middleware
2. **LoginViewModel**: Defines form data structure
3. **IUserAuthenticationService**: Interface for user validation (database-ready)
4. **AccountController**: Handles login/logout logic
5. **Login View**: User interface for login form
6. **Home Controller**: Protected with `[Authorize]`
7. **Layout**: Shows login/logout based on authentication status

**Key Rule**: The authentication scheme name ("CookieAuth") must be consistent everywhere!

