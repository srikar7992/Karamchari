[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "Validating local development environment..." -ForegroundColor Cyan

# 1. Check .NET SDK
$requiredSdkVersion = "10.0"
try {
    $sdkVersion = dotnet --version
    if (-not $sdkVersion.StartsWith($requiredSdkVersion)) {
        Write-Warning "Required .NET SDK $requiredSdkVersion not found. Found $sdkVersion."
    } else {
        Write-Host "[OK] .NET SDK $sdkVersion" -ForegroundColor Green
    }
} catch {
    throw "dotnet SDK not found. Please install .NET $requiredSdkVersion."
}

# 2. Check Docker
try {
    $dockerVersion = docker version --format '{{.Server.Version}}' 2>$null
    Write-Host "[OK] Docker $dockerVersion" -ForegroundColor Green
} catch {
    throw "Docker is not running or not installed."
}

# 3. Check npm
try {
    $npmVersion = npm --version
    Write-Host "[OK] npm $npmVersion" -ForegroundColor Green
} catch {
    Write-Warning "npm not found. Frontend/Mobile builds will be skipped."
}

# 4. Check memory (Recommend 8GB+ for the full stack)
$os = Get-CimInstance Win32_OperatingSystem
$totalRamGB = [Math]::Round($os.TotalVisibleMemorySize / 1MB)
if ($totalRamGB -lt 8) {
    Write-Warning "Total RAM is $totalRamGB GB. Recommended is 8GB+ for the full observability stack."
} else {
    Write-Host "[OK] RAM $totalRamGB GB" -ForegroundColor Green
}

# 5. Port collision check
$ports = @(8080, 1433, 6379, 5672, 15672, 5341, 8081, 4317, 9090, 3000, 10000, 8025)
$collisions = @()

foreach ($port in $ports) {
    $listener = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($listener) {
        $collisions += $port
    }
}

if ($collisions.Count -gt 0) {
    Write-Warning "Port collisions detected on: $($collisions -join ', '). Ensure these are free before running the stack."
} else {
    Write-Host "[OK] No port collisions detected." -ForegroundColor Green
}

Write-Host "Environment validation complete." -ForegroundColor Cyan
