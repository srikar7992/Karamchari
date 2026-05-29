<#
.SYNOPSIS
  Karamchari — One-Command Local Setup (Windows / PowerShell).
.DESCRIPTION
  Brings a brand-new machine from zero state to a running, verified platform.
.PARAMETER NoRun   Setup only; do not start the API.
.PARAMETER Fresh   docker compose down -v first (DESTROYS local data/volumes).
.PARAMETER SkipSeed Skip the optional seed step.
.EXAMPLE
  ./setup-local.ps1
  ./setup-local.ps1 -Fresh
#>
[CmdletBinding()]
param([switch]$NoRun, [switch]$Fresh, [switch]$SkipSeed)

$ErrorActionPreference = "Continue"
$RepoRoot   = $PSScriptRoot
Set-Location $RepoRoot
$Compose    = "infrastructure/local/docker-compose.yml"
$Solution   = "src/Backend/Karamchari.sln"
$ApiProject = "src/Backend/Karamchari.Api"
$SqlPw      = if ($env:KARAMCHARI_SQL_PASSWORD) { $env:KARAMCHARI_SQL_PASSWORD } else { "Karamchari@123" }
# Self-sufficient connection wiring (override appsettings via ConnectionStrings__<Name>).
$env:ConnectionStrings__KaramchariDb = "Server=localhost,1433;Database=Karamchari;User Id=sa;Password=$SqlPw;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
$env:ConnectionStrings__Redis    = "localhost:6379"
$env:ConnectionStrings__RabbitMQ = "amqp://guest:guest@localhost:5672"
$HealthUrl  = "https://localhost:60462/health"
$Report     = "docs/audits/hostile/setup-report.txt"

New-Item -ItemType Directory -Force -Path "docs/audits/hostile" | Out-Null
"" | Set-Content $Report
$script:Fail = 0
function Log($m){ $m | Tee-Object -FilePath $Report -Append }
function Ok($m){ Log "[ OK ] $m" }
function Warn($m){ Log "[WARN] $m" }
function Err($m){ Log "[FAIL] $m"; $script:Fail = 1 }
function Hdr($m){ Log ""; Log "==== $m ====" }

Log "Karamchari local setup — $((Get-Date).ToUniversalTime().ToString('o'))"
Log "Repo: $RepoRoot"

Hdr "1. Prerequisite validation"
if (Get-Command docker -EA SilentlyContinue) { Ok "docker: $(docker --version)" } else { Err "docker not installed" }
docker info *> $null; if ($LASTEXITCODE -eq 0) { Ok "docker daemon reachable" } else { Err "docker daemon not running" }
if (Get-Command dotnet -EA SilentlyContinue) {
  $dv = (dotnet --version); $maj = [int]($dv.Split('.')[0])
  if ($maj -ge 10) { Ok ".NET SDK: $dv" } else { Err ".NET SDK >=10 required, found $dv" }
} else { Err "dotnet not installed" }

Hdr "2. Port availability (informational)"
foreach ($p in 1433,6379,5672,15672,8081,9090,3000,8025,60462) {
  $busy = Test-NetConnection -ComputerName localhost -Port $p -WarningAction SilentlyContinue -InformationLevel Quiet
  if ($busy) { Warn "port $p already in use" } else { Ok "port $p free" }
}
if ($script:Fail -eq 1) { Err "Prerequisite gate failed — aborting."; exit 1 }

if ($Fresh) { Hdr "0. FRESH teardown (down -v)"; docker compose -f $Compose down -v; Ok "volumes destroyed" }

Hdr "3. Start infrastructure"
docker compose -f $Compose up -d sqlserver redis rabbitmq otel-collector seq prometheus grafana mailpit azurite
if ($LASTEXITCODE -eq 0) { Ok "compose up issued" } else { Err "compose up failed" }

Hdr "4. Wait for SQL Server"
$sqlcid = (docker compose -f $Compose ps -q sqlserver)
$ready = $false
for ($i=0; $i -lt 40; $i++) {
  docker exec $sqlcid /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SqlPw -C -Q "SELECT 1" -b *> $null
  if ($LASTEXITCODE -eq 0) { $ready = $true; break }
  Start-Sleep -Seconds 3
}
if ($ready) { Ok "SQL Server accepting connections" } else { Err "SQL Server not ready after ~120s" }

Hdr "5. Restore & build"
dotnet restore $Solution *> $null; if ($LASTEXITCODE -eq 0) { Ok "restore" } else { Err "restore failed" }
dotnet build $Solution --no-restore -c Debug *> $null; if ($LASTEXITCODE -eq 0) { Ok "build (0 errors)" } else { Err "build failed" }

Hdr "6. Provision database, RLS & dev tenants"
dotnet run --project $ApiProject --no-build -- --provision-dev-tenants *>> $Report
if ($LASTEXITCODE -eq 0) { Ok "provisioning exit 0" } else { Err "provisioning failed (exit $LASTEXITCODE)" }

Hdr "7. Verify provisioned state"
function SqlScalar($q){ (docker exec $sqlcid /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SqlPw -d Karamchari -C -h -1 -W -Q "SET NOCOUNT ON; $q" 2>$null | Select-Object -First 1).Trim() }
$idt = SqlScalar "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id WHERE s.name='identity'"
$ten = SqlScalar "SELECT COUNT(*) FROM sys.schemas WHERE name LIKE 'tenant\_%' ESCAPE '\'"
$pol = SqlScalar "SELECT COUNT(*) FROM sys.security_policies"
if ([int]$idt -ge 11) { Ok "identity tables = $idt" } else { Err "identity tables = $idt (expected >=11)" }
if ([int]$ten -ge 3)  { Ok "tenant schemas = $ten" }  else { Err "tenant schemas = $ten (expected >=3)" }
if ([int]$pol -ge 3)  { Ok "RLS policies = $pol" }    else { Err "RLS policies = $pol (expected >=3)" }

Hdr "8. Optional seed"
if ($SkipSeed) { Warn "seed skipped (-SkipSeed)" }
elseif (Test-Path docs/seed/local-dev-seed.sql) {
  Get-Content docs/seed/local-dev-seed.sql | docker exec -i $sqlcid /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SqlPw -d Karamchari -C *>> $Report
  if ($LASTEXITCODE -eq 0) { Ok "seed applied" } else { Warn "seed returned non-zero (non-fatal)" }
} else { Warn "no seed script found" }

if (-not $NoRun) {
  Hdr "9. Start API & health check"
  $env:ASPNETCORE_ENVIRONMENT = "Development"
  $api = Start-Process dotnet -ArgumentList "run --project $ApiProject --no-build" -PassThru -RedirectStandardOutput "$env:TEMP/karamchari-api.log" -RedirectStandardError "$env:TEMP/karamchari-api.err"
  Log "API starting (pid $($api.Id)); polling $HealthUrl"
  $hok = $false
  for ($i=0; $i -lt 30; $i++) {
    try { $r = Invoke-WebRequest -Uri $HealthUrl -SkipCertificateCheck -TimeoutSec 3; if ($r.StatusCode -eq 200) { $hok=$true; break } } catch {}
    Start-Sleep -Seconds 2
  }
  if ($hok) { Ok "API /health = 200 (pid $($api.Id))" } else { Err "API health never reached 200" }
} else { Hdr "9. API start skipped (-NoRun)"; Log "Start manually: dotnet run --project $ApiProject" }

Hdr "Result"
if ($script:Fail -eq 0) { Log "SETUP SUCCEEDED — system operational from zero state." }
else { Log "SETUP FAILED — see [FAIL] lines above." }
exit $script:Fail
