# PowerShell script to run the SQL migration script
# This script will execute the SQL script to add the irregularity_reason column to the LeaveRequest table

$connectionString = "Server=HESTIA\SQLEXPRESS;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;"
$sqlScriptPath = Join-Path $PSScriptRoot "SQL_ADD_IRREGULARITY_REASON.sql"

Write-Host "Running LeaveRequest irregularity_reason column migration..." -ForegroundColor Cyan
Write-Host "Connection: $connectionString" -ForegroundColor Gray
Write-Host "Script: $sqlScriptPath" -ForegroundColor Gray
Write-Host ""

# Check if sqlcmd is available
$sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmdPath) {
    Write-Host "ERROR: sqlcmd is not found in PATH." -ForegroundColor Red
    Write-Host "Please install SQL Server Command Line Utilities or run the script manually in SSMS." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Manual steps:" -ForegroundColor Yellow
    Write-Host "1. Open SQL Server Management Studio" -ForegroundColor White
    Write-Host "2. Connect to: HESTIA\SQLEXPRESS" -ForegroundColor White
    Write-Host "3. Select database: HRMS" -ForegroundColor White
    Write-Host "4. Open file: $sqlScriptPath" -ForegroundColor White
    Write-Host "5. Execute the script (F5)" -ForegroundColor White
    exit 1
}

# Read the SQL script
if (-not (Test-Path $sqlScriptPath)) {
    Write-Host "ERROR: SQL script not found at: $sqlScriptPath" -ForegroundColor Red
    exit 1
}

# Run the SQL script
Write-Host "Executing SQL script..." -ForegroundColor Green
try {
    $result = sqlcmd -S "HESTIA\SQLEXPRESS" -d "HRMS" -i $sqlScriptPath -E
    Write-Host ""
    Write-Host "Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "The following column has been added to LeaveRequest table:" -ForegroundColor Cyan
    Write-Host "  - irregularity_reason (NVARCHAR(MAX), nullable)" -ForegroundColor White
} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to execute SQL script." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Please run the script manually in SQL Server Management Studio." -ForegroundColor Yellow
    exit 1
}


