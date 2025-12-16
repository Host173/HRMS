# Fix MSBuild SDK Resolver Issue with SSMS
# This script renames the problematic SSMS SDK resolver manifest file

$manifestPath = "C:\Program Files\Microsoft SQL Server Management Studio 22\Release\MSBuild\Current\Bin\SdkResolvers\Microsoft.Build.NuGetSdkResolver\Microsoft.Build.NuGetSdkResolver.xml"

if (Test-Path $manifestPath) {
    try {
        # Rename the manifest file to disable it (requires admin rights)
        $backupPath = $manifestPath + ".disabled"
        Rename-Item -Path $manifestPath -NewName "Microsoft.Build.NuGetSdkResolver.xml.disabled" -Force
        Write-Host "Successfully disabled the problematic SSMS SDK resolver manifest." -ForegroundColor Green
        Write-Host "The file has been renamed to: $backupPath" -ForegroundColor Yellow
        Write-Host "Please restart Rider/your IDE and try building again." -ForegroundColor Yellow
    }
    catch {
        Write-Host "Error: Could not rename the manifest file. You may need to run this script as Administrator." -ForegroundColor Red
        Write-Host "Error details: $_" -ForegroundColor Red
        Write-Host "`nAlternative solution: Manually rename the file:" -ForegroundColor Yellow
        Write-Host "  $manifestPath" -ForegroundColor Cyan
        Write-Host "  to:" -ForegroundColor Yellow
        Write-Host "  $manifestPath.disabled" -ForegroundColor Cyan
    }
}
else {
    Write-Host "The manifest file was not found at the expected location." -ForegroundColor Yellow
    Write-Host "The issue may have already been resolved or the path is different." -ForegroundColor Yellow
}
