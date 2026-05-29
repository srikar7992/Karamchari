# verify-local.ps1
$ErrorActionPreference = "Continue"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " Karamchari Day-1 Stack Smoke Test Verification" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$results = [System.Collections.Generic.List[PSObject]]::new()

function Check-Port($name, $host, $port) {
    try {
        $socket = New-Object System.Net.Sockets.TcpClient
        $connect = $socket.BeginConnect($host, $port, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne(1000, $false)
        if ($wait -and $socket.Connected) {
            $socket.Close()
            return "PASS"
        }
        $socket.Close()
        return "FAIL"
    } catch {
        return "FAIL"
    }
}

# 1. SQL Server Port check (1433)
$sqlStatus = Check-Port "SQL Server" "localhost" 1433
$results.Add([PSCustomObject]@{ Service = "SQL Server"; Port = 1433; Status = $sqlStatus })

# 2. RabbitMQ Port check (5672)
$rabbitStatus = Check-Port "RabbitMQ" "localhost" 5672
$results.Add([PSCustomObject]@{ Service = "RabbitMQ"; Port = 5672; Status = $rabbitStatus })

# 3. Redis Port check (6379)
$redisStatus = Check-Port "Redis" "localhost" 6379
$results.Add([PSCustomObject]@{ Service = "Redis"; Port = 6379; Status = $redisStatus })

# 4. API Gateway Port check (60462)
$apiPortStatus = Check-Port "API Port" "localhost" 60462
$results.Add([PSCustomObject]@{ Service = "API Port"; Port = 60462; Status = $apiPortStatus })

# 5. API Health Check
$healthStatus = "FAIL"
if ($apiPortStatus -eq "PASS") {
    try {
        # Suppress SSL certificate validation check in dev
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        $response = Invoke-RestMethod -Uri "https://localhost:60462/health" -Method Get -TimeoutSec 3
        if ($response.status -eq "Healthy") {
            $healthStatus = "PASS"
        }
    } catch {
        $healthStatus = "FAIL ($($_.Exception.Message))"
    }
}
$results.Add([PSCustomObject]@{ Service = "API Health Endpoint"; Port = 60462; Status = $healthStatus })

Write-Host ""
$results | Format-Table -AutoSize
Write-Host "====================================================" -ForegroundColor Cyan
