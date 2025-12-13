# Script to commit HRMS changes to GitHub
# This preserves local files that were deleted on GitHub

Write-Host "=== HRMS Git Commit Script ===" -ForegroundColor Cyan
Write-Host ""

# Save all files first (reminder)
Write-Host "Step 1: Make sure all files are saved in Visual Studio (Ctrl+Shift+S)" -ForegroundColor Yellow
Write-Host ""

# Add all changes
Write-Host "Step 2: Adding all changes..." -ForegroundColor Cyan
git add -A

# Check status
$status = git status --porcelain
if ($status) {
    Write-Host "Changes detected:" -ForegroundColor Green
    git status --short
    
    Write-Host ""
    Write-Host "Step 3: Committing changes..." -ForegroundColor Cyan
    git commit -m "Complete HRMS implementation: role-based access, employee profiles, contract management, notifications, team management
    
- Role-based registration (System Admin, HR Admin, Line Manager, Employee)
- System Admins can create employee accounts
- HR Admins can edit any part of employee profiles
- Profile picture upload
- Employee profile management with full access
- System Admins can assign roles and view all employees
- Managers can view team details and assign employees
- HR Admin profile completeness management
- Contract management (create, renew, update)
- Active, expiring, and expired contract listings
- Contract update notifications
- Role-based dashboard navigation
- Team management for employees and managers"
    
    Write-Host ""
    Write-Host "Step 4: Pushing to GitHub..." -ForegroundColor Cyan
    git push origin master
    
    Write-Host ""
    Write-Host "Successfully committed and pushed to GitHub!" -ForegroundColor Green
} else {
    Write-Host "No changes to commit." -ForegroundColor Yellow
    Write-Host "All changes are already committed or files need to be saved in Visual Studio first." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Note: Local files that were deleted on GitHub are preserved." -ForegroundColor Cyan

