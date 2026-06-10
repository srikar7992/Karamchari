<#
.SYNOPSIS
    Karamchari — unified local health check with PASS/WARN/FAIL output.
.DESCRIPTION
    Probes the local development stack (docker engine, SQL Server, Redis, RabbitMQ,
    Seq, OTel collector, Prometheus, Grafana, API liveness/readiness) and prints one
    aligned PASS/WARN/FAIL line per component. Exit code is 0 when nothing FAILs.

    WARN means "optional component unreachable" (observability stack); FAIL means a
    component required to run the platform is down.
.PARAMETER ApiBaseUrl
    Base URL of the running API. Defaults to the docker-compose mapping
    (http://localhost:8080). Pass e.g. -ApiBaseUrl http://localhost:5000 for `dotnet run`.
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:8080'
)

$ErrorActionPreference = 'SilentlyContinue'
$script:FailCount = 0

function Test-TcpPort {
    param([string]$TargetHost, [int]$Port, [int]$TimeoutMs = 2000)
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $task = $client.ConnectAsync($TargetHost, $Port)
        if ($task.Wait($TimeoutMs) -and $client.Connected) { return $true }
        return $false
    } catch {
        return $false
    } finally {
        $client.Dispose()
    }
}

function Write-Status {
    param([string]$Component, [string]$Status, [string]$Detail = '')
    $color = switch ($Status) {
        'PASS' { 'Green' }
        'WARN' { 'Yellow' }
        default { 'Red' }
    }
    if ($Status -eq 'FAIL') { $script:FailCount++ }
    Write-Host ('{0,-22} ' -f $Component) -NoNewline
    Write-Host ('{0,-5}' -f $Status) -ForegroundColor $color -NoNewline
    if ($Detail) { Write-Host "  $Detail" } else { Write-Host '' }
}

function Test-Component {
    param([string]$Component, [int]$Port, [bool]$Required = $true, [string]$TargetHost = 'localhost')
    if (Test-TcpPort -TargetHost $TargetHost -Port $Port) {
        Write-Status $Component 'PASS' "port $Port reachable"
    } elseif ($Required) {
        Write-Status $Component 'FAIL' "port $Port unreachable"
    } else {
        Write-Status $Component 'WARN' "port $Port unreachable (optional)"
    }
}

function Test-HttpEndpoint {
    param([string]$Component, [string]$Url, [bool]$Required = $true)
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        Write-Status $Component 'PASS' "$Url -> $($response.StatusCode)"
    } catch {
        if ($Required) {
            Write-Status $Component 'FAIL' "$Url unreachable"
        } else {
            Write-Status $Component 'WARN' "$Url unreachable (optional)"
        }
    }
}

Write-Host '================ Karamchari Health Check ================'

# Container engine
$dockerOk = $false
try {
    docker info *> $null
    $dockerOk = ($LASTEXITCODE -eq 0)
} catch { }
if ($dockerOk) {
    Write-Status 'Docker Engine' 'PASS'
} else {
    Write-Status 'Docker Engine' 'FAIL' 'daemon unreachable (start Docker Desktop)'
}

# Core infrastructure (ports per infrastructure/local/docker-compose.yml)
Test-Component 'SQL Server'   1433
Test-Component 'Redis'        6379
Test-Component 'RabbitMQ'     5672
Test-Component 'RabbitMQ Mgmt' 15672 -Required $false

# Observability stack (optional for running the platform)
Test-Component 'Seq'           5341 -Required $false
Test-Component 'OTel Collector' 4317 -Required $false
Test-Component 'Prometheus'    9090 -Required $false
Test-Component 'Grafana'       3000 -Required $false

# Application
Test-HttpEndpoint 'API liveness'  "$ApiBaseUrl/health/live"
Test-HttpEndpoint 'API readiness' "$ApiBaseUrl/health/ready" -Required $false

Write-Host '========================================================='
if ($script:FailCount -gt 0) {
    Write-Host "Result: $script:FailCount component(s) FAILED." -ForegroundColor Red
    exit 1
}
Write-Host 'Result: all required components healthy.' -ForegroundColor Green
exit 0
