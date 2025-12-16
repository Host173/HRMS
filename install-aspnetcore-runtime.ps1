# Install ASP.NET Core 8.0 Runtime
# This script downloads and installs the ASP.NET Core 8.0 runtime

Write-Host "Installing ASP.NET Core 8.0 Runtime..." -ForegroundColor Yellow
Write-Host ""

# Download URL for ASP.NET Core 8.0 Runtime (x64)
$downloadUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.22-windows-x64-installer"
$installerPath = "$env:TEMP\aspnetcore-runtime-8.0.22-win-x64.exe"

Write-Host "Please download and install the ASP.NET Core 8.0 Runtime from:" -ForegroundColor Cyan
Write-Host "https://dotnet.microsoft.com/en-us/download/dotnet/8.0" -ForegroundColor Green
Write-Host ""
Write-Host "Or use this direct link:" -ForegroundColor Cyan
Write-Host "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.22-windows-x64-installer" -ForegroundColor Green
Write-Host ""
Write-Host "After installation, restart Rider and rebuild your project." -ForegroundColor Yellow

# Try to open the download page
Start-Process "https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
