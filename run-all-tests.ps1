<#
.SYNOPSIS  Karamchari — One-Command Test Execution (Windows / PowerShell).
.DESCRIPTION Runs unit, integration, architecture, tenant-isolation, security and
  financial/chaos suites; emits TRX + coverage + summary. Exit code mirrors pipeline.
.PARAMETER NoIntegration  Skip suites that require live Docker infrastructure.
#>
[CmdletBinding()]
param([switch]$NoIntegration)
$ErrorActionPreference = "Continue"
Set-Location $PSScriptRoot
$Solution = "src/Backend/Karamchari.sln"
$Results  = "artifacts/test-results"
$Summary  = "docs/audits/hostile/test-summary.txt"
New-Item -ItemType Directory -Force -Path $Results, "docs/audits/hostile" | Out-Null
"" | Set-Content $Summary

$Unit = @(
  "tests/Backend/Karamchari.Core.UnitTests/Karamchari.Core.UnitTests.csproj",
  "tests/Backend/Karamchari.Api.UnitTests/Karamchari.Api.UnitTests.csproj",
  "tests/Backend/Karamchari.ArchitectureTests/Karamchari.ArchitectureTests.csproj",
  "tests/Backend/Karamchari.Payroll.Tests/Karamchari.Payroll.Tests.csproj",
  "tests/Backend/Karamchari.PSA.Tests/Karamchari.PSA.Tests.csproj",
  "tests/Backend/Karamchari.TimeAttendance.Tests/Karamchari.TimeAttendance.Tests.csproj",
  "tests/Backend/Karamchari.FinancialChaosTests/Karamchari.FinancialChaosTests.csproj"
)
$Integration = @(
  "tests/Backend/Karamchari.Core.IntegrationTests/Karamchari.Core.IntegrationTests.csproj",
  "tests/Backend/Karamchari.Identity.IntegrationTests/Karamchari.Identity.IntegrationTests.csproj",
  "tests/Backend/Karamchari.TenantIsolationCertification/Karamchari.TenantIsolationCertification.csproj"
)

$script:Pass = 0; $script:Failed = 0; $script:FailedList = @()
function Run-Proj($proj) {
  $name = [IO.Path]::GetFileNameWithoutExtension($proj)
  "`n==> $name" | Tee-Object -FilePath $Summary -Append
  # NetArchTest throws TypeLoadException on Coverlet instrumentation tracker
  # types, so architecture tests must run without coverage.
  if ($name -like "*ArchitectureTests*") {
    $out = dotnet test $proj -c Debug --logger "trx;LogFileName=$name.trx" --results-directory $Results 2>&1
  } else {
    $out = dotnet test $proj -c Debug --logger "trx;LogFileName=$name.trx" --results-directory $Results --collect:"XPlat Code Coverage" 2>&1
  }
  $out | Tee-Object -FilePath $Summary -Append | Out-Null
  if ($LASTEXITCODE -eq 0) { $script:Pass++; "[PASS] $name" | Tee-Object -FilePath $Summary -Append }
  else { $script:Failed++; $script:FailedList += $name; "[FAIL] $name (exit $LASTEXITCODE)" | Tee-Object -FilePath $Summary -Append }
}

"Restoring & building once..." | Tee-Object -FilePath $Summary -Append
dotnet build $Solution -c Debug 2>&1 | Select-Object -Last 3 | Tee-Object -FilePath $Summary -Append

foreach ($p in $Unit) { Run-Proj $p }
if (-not $NoIntegration) { foreach ($p in $Integration) { Run-Proj $p } }
else { "[SKIP] integration/cert suites (-NoIntegration)" | Tee-Object -FilePath $Summary -Append }

"" | Tee-Object -FilePath $Summary -Append
"================ SUMMARY ================" | Tee-Object -FilePath $Summary -Append
"Projects passed: $($script:Pass)"          | Tee-Object -FilePath $Summary -Append
"Projects failed: $($script:Failed)"        | Tee-Object -FilePath $Summary -Append
if ($script:FailedList) { "Failed: $($script:FailedList -join ', ')" | Tee-Object -FilePath $Summary -Append }
"TRX + coverage under: $Results" | Tee-Object -FilePath $Summary -Append

if ($script:Failed -eq 0) { exit 0 } else { exit 1 }
