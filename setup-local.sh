#!/usr/bin/env bash
# =============================================================================
# Karamchari — One-Command Local Setup (Unix/macOS)
# =============================================================================
# Brings a brand-new machine from zero state to a running, verified platform.
#
#   ./setup-local.sh                 full setup + start API + verify
#   ./setup-local.sh --no-run        setup only (do not start API)
#   ./setup-local.sh --fresh         docker compose down -v first (DESTROYS data)
#   ./setup-local.sh --skip-seed     skip optional seed step
#
# Exit code 0 = system operational. Non-zero = a gate failed (see report).
# Report written to: docs/hostile-audit/setup-report.txt
# =============================================================================
set -uo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_ROOT"

COMPOSE="infrastructure/local/docker-compose.yml"
SOLUTION="src/Backend/Karamchari.sln"
API_PROJECT="src/Backend/Karamchari.Api"
API_DLL="artifacts/bin/Karamchari.Api/Debug/net10.0/Karamchari.Api.dll"
API_CONTENT_ROOT="$REPO_ROOT/$API_PROJECT"
SQL_PW="${KARAMCHARI_SQL_PASSWORD:-Karamchari@123}"
# Self-sufficient connection wiring: the API host (Development) defaults to Docker SQL,
# but we export explicitly so the script works even if appsettings drifts or a custom
# SQL password is supplied. These env vars override appsettings via the standard
# ConnectionStrings__<Name> convention.
export ConnectionStrings__KaramchariDb="Server=localhost,1433;Database=Karamchari;User Id=sa;Password=${SQL_PW};TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
export ConnectionStrings__Redis="localhost:6379"
export ConnectionStrings__RabbitMQ="amqp://guest:guest@localhost:5672"
HEALTH_URL="https://localhost:60462/health"
REPORT="docs/audits/hostile/setup-report.txt"

RUN_API=1; FRESH=0; SKIP_SEED=0
for a in "$@"; do
  case "$a" in
    --no-run) RUN_API=0 ;;
    --fresh) FRESH=1 ;;
    --skip-seed) SKIP_SEED=1 ;;
    *) echo "Unknown arg: $a"; exit 2 ;;
  esac
done

mkdir -p docs/audits/hostile
: > "$REPORT"
FAIL=0
log()  { printf '%s\n' "$*" | tee -a "$REPORT"; }
ok()   { log "[ OK ] $*"; }
warn() { log "[WARN] $*"; }
err()  { log "[FAIL] $*"; FAIL=1; }
hdr()  { log ""; log "==== $* ===="; }

log "Karamchari local setup — $(date -u +%Y-%m-%dT%H:%M:%SZ)"
log "Repo: $REPO_ROOT"

# ---------------------------------------------------------------------------
hdr "1. Prerequisite validation"
if command -v docker >/dev/null 2>&1; then ok "docker: $(docker --version)"; else err "docker not installed"; fi
if docker info >/dev/null 2>&1; then ok "docker daemon reachable"; else err "docker daemon not running"; fi
if command -v dotnet >/dev/null 2>&1; then
  DV="$(dotnet --version)"; MAJ="${DV%%.*}"
  if [ "${MAJ:-0}" -ge 10 ] 2>/dev/null; then ok ".NET SDK: $DV"; else err ".NET SDK >=10 required, found $DV"; fi
else err "dotnet not installed"; fi
command -v curl >/dev/null 2>&1 && ok "curl present" || warn "curl missing (health check will be skipped)"

hdr "2. Port availability (informational)"
for p in 1433 6379 5672 15672 8081 9090 3000 8025 60462; do
  if nc -z localhost "$p" >/dev/null 2>&1; then warn "port $p already in use (existing container or conflict)"; else ok "port $p free"; fi
done

[ "$FAIL" -eq 1 ] && { err "Prerequisite gate failed — aborting."; exit 1; }

# ---------------------------------------------------------------------------
if [ "$FRESH" -eq 1 ]; then
  hdr "0. FRESH teardown (docker compose down -v)"
  docker compose -f "$COMPOSE" down -v && ok "volumes destroyed" || warn "down -v reported an issue"
fi

hdr "3. Start infrastructure"
docker compose -f "$COMPOSE" up -d sqlserver redis rabbitmq otel-collector seq prometheus grafana mailpit azurite \
  && ok "compose up issued" || err "compose up failed"

hdr "4. Wait for SQL Server"
SQLCID="$(docker compose -f "$COMPOSE" ps -q sqlserver)"
READY=0
for i in $(seq 1 40); do
  if docker exec "$SQLCID" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SQL_PW" -C -Q "SELECT 1" -b >/dev/null 2>&1; then
    READY=1; break; fi
  sleep 3
done
[ "$READY" -eq 1 ] && ok "SQL Server accepting connections" || err "SQL Server not ready after ~120s"

hdr "5. Restore & build"
dotnet restore "$SOLUTION" >/dev/null 2>&1 && ok "restore" || err "restore failed"
dotnet build "$SOLUTION" --no-restore -c Debug >/dev/null 2>&1 && ok "build (0 errors)" || err "build failed"

hdr "6. Provision database, RLS & dev tenants"
if dotnet run --project "$API_PROJECT" --no-build -- --provision-dev-tenants >>"$REPORT" 2>&1; then
  ok "provisioning exit 0"
else
  err "provisioning failed (exit $?)"
fi

hdr "7. Verify provisioned state"
verify_sql() {
  docker exec "$SQLCID" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SQL_PW" -d Karamchari -C -h -1 -W -Q "SET NOCOUNT ON; $1" 2>/dev/null | tr -d '\r' | head -1
}
IDT="$(verify_sql "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id WHERE s.name='identity'")"
TEN="$(verify_sql "SELECT COUNT(*) FROM sys.schemas WHERE name LIKE 'tenant\\_%' ESCAPE '\\'")"
POL="$(verify_sql "SELECT COUNT(*) FROM sys.security_policies")"
KEY="$(verify_sql "SELECT COUNT(*) FROM [identity].[SigningKeys]")"
[ "${IDT:-0}" -ge 11 ] && ok "identity tables = $IDT" || err "identity tables = ${IDT:-0} (expected >=11)"
[ "${TEN:-0}" -ge 3 ]  && ok "tenant schemas = $TEN"  || err "tenant schemas = ${TEN:-0} (expected >=3)"
[ "${POL:-0}" -ge 3 ]  && ok "RLS policies = $POL"    || err "RLS policies = ${POL:-0} (expected >=3)"
[ "${KEY:-0}" -ge 1 ]  && ok "signing keys = $KEY"    || warn "signing keys = ${KEY:-0} (expected >=1)"

hdr "8. Optional seed"
if [ "$SKIP_SEED" -eq 1 ]; then warn "seed skipped (--skip-seed)";
elif [ -f docs/seed/local-dev-seed.sql ]; then
  if docker exec -i "$SQLCID" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SQL_PW" -d Karamchari -C < docs/seed/local-dev-seed.sql >>"$REPORT" 2>&1; then
    ok "seed applied"; else warn "seed script returned non-zero (non-fatal; see report)"; fi
else warn "no seed script found"; fi

# ---------------------------------------------------------------------------
if [ "$RUN_API" -eq 1 ]; then
  hdr "9. Start API & health check"
  nohup env ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="https://localhost:60462;http://localhost:60463" \
    dotnet "$API_DLL" --contentRoot "$API_CONTENT_ROOT" </dev/null >/tmp/karamchari-api.log 2>&1 &
  APIPID=$!
  disown "$APIPID" 2>/dev/null || true
  echo "$APIPID" > /tmp/karamchari-api.pid
  log "API starting (pid $APIPID); polling $HEALTH_URL"
  HOK=0
  for i in $(seq 1 30); do
    code="$(curl -k -s -o /dev/null -w '%{http_code}' -m 3 "$HEALTH_URL" 2>/dev/null || true)"
    if [ "$code" = "200" ]; then HOK=1; break; fi
    sleep 2
  done
  if [ "$HOK" -eq 1 ]; then ok "API /health = 200 (pid $APIPID, logs /tmp/karamchari-api.log)";
  else err "API health never reached 200 (see /tmp/karamchari-api.log)"; fi
else
  hdr "9. API start skipped (--no-run)"
  log "Start manually:  dotnet run --project $API_PROJECT"
fi

hdr "Result"
if [ "$FAIL" -eq 0 ]; then log "SETUP SUCCEEDED — system operational from zero state."; else log "SETUP FAILED — see [FAIL] lines above."; fi
exit "$FAIL"
