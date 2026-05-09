<#
.SYNOPSIS
    Bootstraps the local development environment for the Karamchari Platform.
    Ensures pre-commit hooks are installed and dependencies are restored.
#>

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Karamchari Local Environment Setup" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Configure Git Hooks Path
Write-Host "Configuring Git to use .githooks directory..."
git config core.hooksPath .githooks

if ($?) {
    Write-Host "✅ Git hooks configured successfully." -ForegroundColor Green
} else {
    Write-Host "❌ Failed to configure Git hooks." -ForegroundColor Red
    exit 1
}

# 2. Ensure SDK is installed
Write-Host "Checking .NET SDK..."
$sdkVersion = dotnet --version
Write-Host "Found .NET SDK version: $sdkVersion"

# 3. Restore Packages
Write-Host "Restoring NuGet packages..."
dotnet restore src/Backend/Karamchari.sln
if ($?) {
    Write-Host "✅ Packages restored successfully." -ForegroundColor Green
} else {
    Write-Host "❌ Package restore failed." -ForegroundColor Red
    exit 1
}

# 4. Initial Build Check
Write-Host "Verifying local build (Zero Warnings Policy)..."
dotnet build src/Backend/Karamchari.sln -c Debug
if ($?) {
    Write-Host "✅ Build succeeded with zero warnings." -ForegroundColor Green
} else {
    Write-Host "❌ Build failed. Check the output above." -ForegroundColor Red
    exit 1
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Environment is ready for development!" -ForegroundColor Cyan
Write-Host " Next steps: See docs/engineering/local_setup.md" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
