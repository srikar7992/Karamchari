[CmdletBinding()]
param(
    [switch] $SkipIntegration
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $repoRoot "src/Backend/Karamchari.sln"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string] $Name,
        [Parameter(Mandatory = $true)][scriptblock] $Command
    )

    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

Set-Location $repoRoot

Invoke-Step "Restore locked NuGet graph" {
    dotnet restore $solution --locked-mode
}

Invoke-Step "Verify formatting" {
    dotnet format $solution --verify-no-changes --severity warn --no-restore
}

Invoke-Step "Build with analyzers and warnings as errors" {
    dotnet build $solution --no-restore -c Release -warnaserror
}

if (-not $SkipIntegration) {
    Invoke-Step "Run backend unit and integration tests with coverage" {
        dotnet test $solution --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
    }
} else {
    Invoke-Step "Run backend non-integration test projects with coverage" {
        dotnet test "tests/Backend/Karamchari.Core.UnitTests/Karamchari.Core.UnitTests.csproj" --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
        dotnet test "tests/Backend/Karamchari.Api.UnitTests/Karamchari.Api.UnitTests.csproj" --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
        dotnet test "src/Backend/Karamchari.Payroll.Tests/Karamchari.Payroll.Tests.csproj" --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
        dotnet test "src/Backend/Karamchari.PSA.Tests/Karamchari.PSA.Tests.csproj" --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
        dotnet test "src/Backend/Karamchari.TimeAttendance.Tests/Karamchari.TimeAttendance.Tests.csproj" --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
    }
}

Invoke-Step "NuGet vulnerability audit" {
    dotnet list $solution package --vulnerable --include-transitive
}

if (Test-Path "src/Frontend/portal/package-lock.json") {
    Invoke-Step "Portal deterministic install audit" {
        npm ci --prefix "src/Frontend/portal" --ignore-scripts --loglevel=error
        npm audit --prefix "src/Frontend/portal" --audit-level=moderate
    }

    Invoke-Step "Portal typecheck" {
        npm run --prefix "src/Frontend/portal" typecheck
    }

    Invoke-Step "Portal production build" {
        npm run --prefix "src/Frontend/portal" build
    }
}

if (Test-Path "src/Mobile/karamchari-mobile/package-lock.json") {
    Invoke-Step "Mobile deterministic install audit" {
        npm ci --prefix "src/Mobile/karamchari-mobile" --ignore-scripts --loglevel=error
        npm audit --prefix "src/Mobile/karamchari-mobile" --audit-level=moderate
    }
}

Invoke-Step "Publish validation" {
    dotnet publish "src/Backend/Karamchari.Api/Karamchari.Api.csproj" --no-restore -c Release -o "artifacts/publish/Karamchari.Api" -warnaserror
}

Write-Host "Validation completed successfully." -ForegroundColor Green
