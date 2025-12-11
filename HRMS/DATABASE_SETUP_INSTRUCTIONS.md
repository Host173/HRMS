# Database Integration Setup Instructions

## ✅ What Has Been Done

1. **Entity Framework Core** - Added packages for database connectivity
2. **ApplicationDbContext** - Created database context for Employee table
3. **Employee Model** - Created C# model matching your database structure
4. **Database Authentication Service** - Implemented with BCrypt password hashing
5. **Registration System** - Full registration page with validation
6. **Login Updated** - Now uses email instead of username
7. **Program.cs** - Configured to use database connection

## 📋 Required Steps Before Running

### Step 1: Run the SQL Script

**IMPORTANT**: You need to add the `password_hash` column to your Employee table.

1. Open SQL Server Management Studio (SSMS) or your SQL tool
2. Connect to your database: `KAREEM\SQLEXPRESS`
3. Select the `HRMS` database
4. Open and run the file: `HRMS/SQL_ADD_PASSWORD_COLUMN.sql`

This script will:
- Add `password_hash VARCHAR(255)` column to Employee table
- Create a unique index on email for faster login lookups

### Step 2: Restore NuGet Packages

The project now requires these NuGet packages:
- `Microsoft.EntityFrameworkCore.SqlServer` (v9.0.0)
- `Microsoft.EntityFrameworkCore.Tools` (v9.0.0)
- `BCrypt.Net-Next` (v4.0.3)

**In Rider:**
- Right-click on the solution → "Restore NuGet Packages"
- Or build the project (it will restore automatically)

**In Visual Studio:**
- Right-click on the project → "Restore NuGet Packages"
- Or build the project

### Step 3: Verify Connection String

Check that `appsettings.json` has the correct connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=KAREEM\\SQLEXPRESS;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

If your server name or database name is different, update it here.

### Step 4: Build and Run

1. Build the project (should restore packages automatically)
2. Run the application
3. Navigate to `/Account/Register` to create your first account
4. Or navigate to `/Account/Login` to log in

## 🎯 How It Works

### Login Flow:
1. User enters **email** and password
2. System queries Employee table by email
3. Compares password using BCrypt
4. If valid, creates authentication cookie
5. Redirects to home page

### Registration Flow:
1. User fills registration form (First Name, Last Name, Email, Phone, Password)
2. System checks if email already exists
3. Hashes password using BCrypt
4. Creates new Employee record
5. Automatically logs in the user
6. Redirects to home page

## 📁 Files Created/Modified

### New Files:
- `HRMS/Data/ApplicationDbContext.cs` - Database context
- `HRMS/Models/Employee.cs` - Employee entity model
- `HRMS/Models/RegisterViewModel.cs` - Registration form model
- `HRMS/Views/Account/Register.cshtml` - Registration page
- `HRMS/SQL_ADD_PASSWORD_COLUMN.sql` - Database migration script

### Modified Files:
- `HRMS/HRMS.csproj` - Added EF Core and BCrypt packages
- `HRMS/Program.cs` - Added DbContext and switched to DatabaseAuthenticationService
- `HRMS/Controllers/AccountController.cs` - Added registration actions
- `HRMS/Services/DatabaseAuthenticationService.cs` - Full database implementation
- `HRMS/Models/LoginViewModel.cs` - Changed to use email
- `HRMS/Views/Account/Login.cshtml` - Updated to show registration link

## 🔐 Password Security

- Passwords are hashed using **BCrypt** (industry standard)
- Original passwords are never stored in the database
- Each password gets a unique salt automatically
- Password verification is secure against timing attacks

## 🧪 Testing

### Test Registration:
1. Go to `/Account/Register`
2. Fill in the form:
   - First Name: John
   - Last Name: Doe
   - Email: john.doe@example.com
   - Phone: 1234567890
   - Password: Test123!
   - Confirm Password: Test123!
3. Click "Register"
4. Should automatically log you in and redirect to home

### Test Login:
1. Go to `/Account/Login`
2. Enter the email and password you registered with
3. Click "Login"
4. Should redirect to home page

## ⚠️ Important Notes

1. **Email is Unique**: The system enforces unique emails. If you try to register with an existing email, you'll get an error.

2. **Active Employees Only**: Only employees with `is_active = 1` can log in.

3. **Password Requirements**: 
   - Minimum 6 characters (can be changed in RegisterViewModel)
   - Passwords are case-sensitive

4. **Database Column**: Make sure you've run the SQL script to add `password_hash` column!

## 🔧 Customization

### Change Password Requirements:
Edit `HRMS/Models/RegisterViewModel.cs`:
```csharp
[StringLength(100, ErrorMessage = "Password must be at least 8 characters", MinimumLength = 8)]
```

### Add More Fields to Registration:
1. Add property to `RegisterViewModel.cs`
2. Add input field to `Register.cshtml`
3. Map to Employee in `AccountController.Register` action

### Change Login to Use Username Instead of Email:
1. Add `username` column to Employee table
2. Update `LoginViewModel` to use Username
3. Update `DatabaseAuthenticationService` to query by username
4. Update Employee model and DbContext

## 🐛 Troubleshooting

### "Cannot open database" error:
- Check connection string in `appsettings.json`
- Verify SQL Server is running
- Check database name is correct

### "Invalid object name 'Employee'" error:
- Make sure you're connected to the correct database
- Verify the table name matches (case-sensitive in some SQL Server configurations)

### "Column 'password_hash' does not exist":
- Run the SQL script: `SQL_ADD_PASSWORD_COLUMN.sql`
- Check the column was added: `SELECT * FROM Employee`

### Registration fails silently:
- Check application logs
- Verify email is unique
- Check database connection

### Login doesn't work:
- Verify employee exists in database
- Check `is_active = 1`
- Verify `password_hash` is not NULL
- Check email matches exactly (case-insensitive)

## 📞 Need Help?

If you encounter issues:
1. Check the application logs
2. Verify database connection
3. Ensure SQL script was run
4. Check that NuGet packages were restored

