<#
.SYNOPSIS
    Bootstraps the local development environment for the Karamchari platform.
#>

$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Karamchari Local Environment Setup" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

Write-Host "Configuring Git to use .githooks directory..."
git config core.hooksPath .githooks
if (-not $?) { throw "Failed to configure Git hooks." }
Write-Host "Git hooks configured successfully." -ForegroundColor Green

Write-Host "Checking .NET SDK..."
$sdkVersion = dotnet --version
Write-Host "Found .NET SDK version: $sdkVersion"

Write-Host "Restoring NuGet packages in locked mode..."
dotnet restore src/Backend/Karamchari.sln --locked-mode
if (-not $?) { throw "Package restore failed." }
Write-Host "Packages restored successfully." -ForegroundColor Green

Write-Host "Verifying local build (Zero Warnings Policy)..."
dotnet build src/Backend/Karamchari.sln --no-restore -c Debug -warnaserror
if (-not $?) { throw "Build failed." }
Write-Host "Build succeeded with zero warnings." -ForegroundColor Green

if (Get-Command npm -ErrorAction SilentlyContinue) {
    if (Test-Path "src/Frontend/portal/package-lock.json") {
        Write-Host "Installing portal dependencies from package-lock.json..."
        npm ci --prefix src/Frontend/portal --ignore-scripts --loglevel=error
        if (-not $?) { throw "Portal npm install failed." }
    }

    if (Test-Path "src/Mobile/karamchari-mobile/package-lock.json") {
        Write-Host "Installing mobile dependencies from package-lock.json..."
        npm ci --prefix src/Mobile/karamchari-mobile --ignore-scripts --loglevel=error
        if (-not $?) { throw "Mobile npm install failed." }
    }
} else {
    Write-Host "npm not found; skipping frontend/mobile install. CI still enforces npm lockfile audits." -ForegroundColor Yellow
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Environment is ready for development!" -ForegroundColor Cyan
Write-Host " Next steps: Run .\scripts\validate.ps1 for the full validation gate." -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
