# Troubleshooting: Home Page Not Redirecting to Login

## Problem
When you access the home page directly, it doesn't redirect to the login page even though `[Authorize]` is set.

## Quick Fixes

### 1. Clear Browser Cookies
**Most Common Issue**: Old authentication cookies from previous testing.

**Solution**:
- Press `Ctrl+Shift+Delete` (Windows) or `Cmd+Shift+Delete` (Mac)
- Select "Cookies" and "Cached images and files"
- Clear data
- **OR** use an Incognito/Private browser window

### 2. Check Application Logs
Look for errors in:
- Rider's output window
- Application console
- Browser developer tools (F12 → Console tab)

Common errors:
- Database connection failed
- Table not found
- Column not found

### 3. Verify Database Connection
Make sure:
- SQL Server is running
- Database `HRMS` exists
- Connection string in `appsettings.json` is correct
- `password_hash` column exists in Employee table

### 4. Test the Redirect Manually
1. Stop the application
2. Clear all browser data
3. Start the application
4. Navigate to: `https://localhost:XXXX/` (your port)
5. Should redirect to: `https://localhost:XXXX/Account/Login`

### 5. Check if You're Already Logged In
If you see "Welcome, [name]!" in the navigation bar, you're already logged in.
- Click "Logout" to test the redirect
- Then try accessing home page again

## Verification Steps

### Step 1: Verify Home Controller
File: `HRMS/Controllers/HomeController.cs`
```csharp
[Authorize]  // ← This should be here
public class HomeController : Controller
```

### Step 2: Verify Program.cs
File: `HRMS/Program.cs`
```csharp
app.UseAuthentication();  // ← Must come before
app.UseAuthorization();   // ← This
```

### Step 3: Verify Login Path
File: `HRMS/Program.cs`
```csharp
options.LoginPath = "/Account/Login";  // ← Should match
```

### Step 4: Test Database Connection
Run this in SQL Server Management Studio:
```sql
SELECT TOP 1 * FROM Employee;
```

If this fails, your database connection is the issue.

## Still Not Working?

### Check Browser Network Tab
1. Open browser developer tools (F12)
2. Go to "Network" tab
3. Try accessing home page
4. Look for:
   - Status code 302 (redirect) - Good!
   - Status code 200 (success) - Bad! Should redirect
   - Status code 500 (error) - Database/application error

### Check Application Output
In Rider, look at the "Output" or "Run" window for:
- Database connection errors
- Exception stack traces
- Authentication errors

### Manual Test
1. Open browser
2. Go to: `https://localhost:XXXX/Account/Login`
3. If login page shows → Authentication is working
4. Try logging in with test credentials
5. Then try accessing home page

## Expected Behavior

**When NOT logged in:**
- Access `/` or `/Home/Index` → Redirects to `/Account/Login`
- Status code: 302 (Redirect)

**When logged in:**
- Access `/` or `/Home/Index` → Shows home page
- Status code: 200 (OK)

## Common Issues

### Issue: "Cannot open database"
**Solution**: Check SQL Server is running and connection string is correct

### Issue: "Invalid column name 'password_hash'"
**Solution**: Run the SQL script: `SQL_ADD_PASSWORD_COLUMN.sql`

### Issue: Page loads but shows error
**Solution**: Check application logs for the actual error message

### Issue: Redirect loop
**Solution**: Check that AccountController has `[AllowAnonymous]` attribute

## Need More Help?

If none of these work:
1. Share the error message from application logs
2. Share what you see in browser developer tools (F12)
3. Verify SQL Server is accessible
4. Check that all NuGet packages are restored

