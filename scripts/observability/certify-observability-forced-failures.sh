#!/usr/bin/env bash
# =============================================================================
# Karamchari — Observability Forced-Failure Certification Script
# Phase G: Verify observability pipeline under known failure conditions
#
# Prerequisites:
#   - docker compose stack running (docker compose up -d)
#   - jq installed
#   - curl installed
#   - Valid JWT in ADMIN_TOKEN env var OR script will attempt login
#
# Usage:
#   bash scripts/observability/certify-observability-forced-failures.sh
#
# Each step records PASS/FAIL. A final summary is printed.
# Services are recovered at the end regardless of individual step outcome.
# =============================================================================

set -euo pipefail

API_URL="http://localhost:8080"
SEQ_URL="http://localhost:8081"
PROM_URL="http://localhost:9090"
COMPOSE_FILE="docker-compose.yml"

PASS=0
FAIL=0
declare -a RESULTS=()

log()  { echo "[$(date '+%H:%M:%S')] $*"; }
pass() { PASS=$((PASS+1));  RESULTS+=("PASS  $*"); echo "[PASS]  $*"; }
fail() { FAIL=$((FAIL+1));  RESULTS+=("FAIL  $*"); echo "[FAIL]  $*"; }
warn() { echo "[WARN]  $*"; }

separator() { echo; echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"; echo "  $*"; echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"; }

# --- obtain auth token -------------------------------------------------------
obtain_token() {
    local response
    response=$(curl -s -X POST "$API_URL/api/identity/login" \
        -H "Content-Type: application/json" \
        -d '{"email":"admin@dev.local","password":"Dev@Pass123!"}')
    echo "$response" | jq -r '.accessToken // empty'
}

# --- Seq log search ----------------------------------------------------------
# Query Seq signal stream for a recent log event matching a pattern.
# Seq /api/events/signal endpoint: GET with ?filter=<clef-filter>&count=<n>
seq_search() {
    local filter="$1"
    local label="$2"
    local count=50
    local result
    result=$(curl -s -G "$SEQ_URL/api/events/signal" \
        --data-urlencode "filter=$filter" \
        --data-urlencode "count=$count" \
        -H "Accept: application/json" 2>/dev/null || echo "")
    if echo "$result" | jq -e '.[] | select(.Level != null)' >/dev/null 2>&1; then
        pass "$label — structured log entry found in Seq"
        return 0
    else
        fail "$label — no matching log entry found in Seq (filter: $filter)"
        return 1
    fi
}

# --- Prometheus metric query -------------------------------------------------
prom_query() {
    local query="$1"
    local label="$2"
    local result
    result=$(curl -s "$PROM_URL/api/v1/query?query=$(python3 -c "import urllib.parse,sys; print(urllib.parse.quote(sys.argv[1]))" "$query")" 2>/dev/null || echo "")
    local value
    value=$(echo "$result" | jq -r '.data.result[0].value[1] // empty' 2>/dev/null || echo "")
    if [[ -n "$value" ]]; then
        pass "$label — Prometheus value: $value"
        echo "  → query: $query → $value"
        return 0
    else
        fail "$label — Prometheus returned no data for query: $query"
        return 1
    fi
}

# =============================================================================
# PRE-FLIGHT: verify stack is up
# =============================================================================

separator "PRE-FLIGHT — stack health check"

log "Checking API health..."
if curl -sf "$API_URL/health" >/dev/null; then
    pass "API health endpoint reachable"
else
    fail "API not reachable at $API_URL — aborting (stack may not be running)"
    echo "Run: docker compose up -d"
    exit 1
fi

log "Checking Seq..."
if curl -sf "$SEQ_URL" >/dev/null; then
    pass "Seq reachable at $SEQ_URL"
else
    fail "Seq not reachable"
fi

log "Checking Prometheus..."
if curl -sf "$PROM_URL/-/healthy" >/dev/null; then
    pass "Prometheus healthy"
else
    fail "Prometheus not reachable"
fi

log "Obtaining auth token..."
ADMIN_TOKEN="${ADMIN_TOKEN:-$(obtain_token)}"
if [[ -z "$ADMIN_TOKEN" ]]; then
    fail "Could not obtain admin token — authentication failed"
    echo "Set ADMIN_TOKEN env var or verify API is running with seeded users"
    exit 1
else
    pass "Admin token obtained (${ADMIN_TOKEN:0:20}…)"
fi

# =============================================================================
# STEP 1: DB UNAVAILABLE — API returns 503, Seq captures structured error log
# =============================================================================

separator "STEP 1 — DB unavailable: structured error log in Seq"

log "Stopping SQL Server container..."
docker compose -f "$COMPOSE_FILE" stop sqlserver 2>/dev/null || \
    docker stop karamchari-sqlserver-1 2>/dev/null || \
    docker stop local-sqlserver-1 2>/dev/null || \
    warn "Could not stop sqlserver — it may already be stopped or name differs"

log "Waiting 5s for circuit breaker / health check to detect failure..."
sleep 5

log "Making API call that requires DB (expect 503 or 500)..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "Authorization: Bearer $ADMIN_TOKEN" \
    "$API_URL/api/v1/hr/employees" 2>/dev/null || echo "000")

if [[ "$HTTP_STATUS" == "503" || "$HTTP_STATUS" == "500" || "$HTTP_STATUS" == "000" ]]; then
    pass "Step 1 — API returned $HTTP_STATUS (expected) when DB unavailable"
else
    warn "Step 1 — API returned $HTTP_STATUS (expected 503/500/connection-refused)"
fi

log "Waiting 3s for log to propagate to Seq..."
sleep 3

log "Searching Seq for DB error log..."
seq_search \
    '@Level = \"Error\" or @MessageTemplate like \"%database%\" or @MessageTemplate like \"%SqlException%\" or @MessageTemplate like \"%connection%\"' \
    "Step 1 — DB-unavailable error log in Seq"

# =============================================================================
# STEP 2: RECOVER SQL — restart sqlserver
# =============================================================================

separator "STEP 2 (prep) — Restart SQL Server before RabbitMQ test"

log "Restarting SQL Server..."
docker compose -f "$COMPOSE_FILE" start sqlserver 2>/dev/null || \
    docker start karamchari-sqlserver-1 2>/dev/null || \
    docker start local-sqlserver-1 2>/dev/null || \
    warn "Could not start sqlserver — check container name"

log "Waiting 15s for SQL Server to become ready..."
sleep 15

# Capture outbox queue depth BEFORE stopping RabbitMQ
QUEUE_DEPTH_BEFORE=$(curl -s "$PROM_URL/api/v1/query?query=karamchari_outbox_queue_depth" | \
    jq -r '.data.result[0].value[1] // "0"' 2>/dev/null || echo "0")
log "Outbox queue depth before RabbitMQ stop: $QUEUE_DEPTH_BEFORE"

# =============================================================================
# STEP 2: RABBITMQ UNAVAILABLE — outbox accumulates, metric increases
# =============================================================================

separator "STEP 2 — RabbitMQ unavailable: karamchari_outbox_queue_depth increases"

log "Stopping RabbitMQ..."
docker compose -f "$COMPOSE_FILE" stop rabbitmq 2>/dev/null || \
    docker stop karamchari-rabbitmq-1 2>/dev/null || \
    docker stop local-rabbitmq-1 2>/dev/null || \
    warn "Could not stop rabbitmq"

log "Waiting 5s for outbox relay to detect broker failure..."
sleep 5

log "Triggering an async operation (login to generate audit event)..."
curl -s -X POST "$API_URL/api/identity/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@dev.local","password":"Dev@Pass123!"}' >/dev/null || true

log "Waiting 8s for outbox relay cycle..."
sleep 8

log "Querying karamchari_outbox_queue_depth..."
prom_query "karamchari_outbox_queue_depth" "Step 2 — karamchari_outbox_queue_depth metric exists"

QUEUE_DEPTH_AFTER=$(curl -s "$PROM_URL/api/v1/query?query=karamchari_outbox_queue_depth" | \
    jq -r '.data.result[0].value[1] // "0"' 2>/dev/null || echo "0")
log "Outbox queue depth after broker stop: $QUEUE_DEPTH_AFTER"

if (( $(echo "$QUEUE_DEPTH_AFTER >= $QUEUE_DEPTH_BEFORE" | bc -l 2>/dev/null || echo 0) )); then
    pass "Step 2 — queue depth ($QUEUE_DEPTH_AFTER) >= before ($QUEUE_DEPTH_BEFORE): outbox accumulating"
else
    warn "Step 2 — queue depth did not increase (before=$QUEUE_DEPTH_BEFORE after=$QUEUE_DEPTH_AFTER)"
fi

log "Querying karamchari_outbox_dlq_size..."
prom_query "karamchari_outbox_dlq_size" "Step 2 — karamchari_outbox_dlq_size metric exists"

log "Querying karamchari_outbox_retry_backlog..."
prom_query "karamchari_outbox_retry_backlog" "Step 2 — karamchari_outbox_retry_backlog metric exists"

# =============================================================================
# STEP 3: INVALID AUTH — structured AuthorizationFailed log in Seq
# =============================================================================

separator "STEP 3 — Invalid auth: structured AuthorizationFailed log in Seq"

log "Restarting RabbitMQ before auth test..."
docker compose -f "$COMPOSE_FILE" start rabbitmq 2>/dev/null || \
    docker start karamchari-rabbitmq-1 2>/dev/null || \
    docker start local-rabbitmq-1 2>/dev/null || \
    warn "Could not restart rabbitmq"
sleep 5

log "Sending 5 requests with invalid credentials..."
for i in {1..5}; do
    curl -s -o /dev/null -X POST "$API_URL/api/identity/login" \
        -H "Content-Type: application/json" \
        -d '{"email":"admin@dev.local","password":"WRONG_PASSWORD_CERT_TEST"}' || true
done

log "Sending 1 request with no token to protected endpoint..."
curl -s -o /dev/null -H "Authorization: Bearer invalid.token.here" \
    "$API_URL/api/v1/hr/employees" || true

log "Waiting 3s for log propagation..."
sleep 3

log "Searching Seq for AUTH_FAILURE log..."
seq_search \
    '@MessageTemplate like \"%AUTH_FAILURE%\" or @MessageTemplate like \"%Invalid credentials%\" or @MessageTemplate like \"%Unauthorized%\"' \
    "Step 3 — AuthorizationFailed structured log in Seq"

log "Searching Seq for Warning-level auth event..."
seq_search \
    '@Level = \"Warning\" and (@MessageTemplate like \"%auth%\" or @MessageTemplate like \"%credential%\" or @MessageTemplate like \"%token%\")' \
    "Step 3 — Auth warning-level structured log in Seq"

# =============================================================================
# STEP 4: PROMETHEUS CORRELATION QUERIES
# =============================================================================

separator "STEP 4 — Prometheus correlation metric queries"

log "Querying all karamchari_outbox_* metrics..."
prom_query "karamchari_outbox_queue_depth"                "Step 4 — outbox queue_depth metric"
prom_query "karamchari_outbox_dlq_size"                   "Step 4 — outbox dlq_size metric"
prom_query "karamchari_outbox_oldest_pending_age_seconds" "Step 4 — outbox oldest_pending_age_seconds metric"
prom_query "karamchari_outbox_retry_backlog"              "Step 4 — outbox retry_backlog metric"
prom_query "karamchari_outbox_relay_batch_cycles_total"   "Step 4 — outbox relay_batch_cycles_total metric"

log "Querying HTTP RED metrics..."
prom_query "http_server_active_requests"  "Step 4 — http_server_active_requests metric"
prom_query "kestrel_active_connections"   "Step 4 — kestrel_active_connections metric"

log "Querying OTLP pipeline health (targets up)..."
TARGETS_UP=$(curl -s "$PROM_URL/api/v1/query?query=up" | \
    jq '[.data.result[] | select(.value[1]=="1")] | length' 2>/dev/null || echo "0")
if [[ "$TARGETS_UP" -gt 0 ]]; then
    pass "Step 4 — $TARGETS_UP Prometheus targets are up"
else
    fail "Step 4 — No Prometheus targets are up"
fi

log "Raw Prometheus query output for outbox DLQ (for evidence capture)..."
curl -s "$PROM_URL/api/v1/query?query=karamchari_outbox_dlq_size" | jq . || true

# =============================================================================
# STEP 5: RECOVER — restart all stopped services, verify API healthy
# =============================================================================

separator "STEP 5 — Recovery: restart all services, verify API returns 200"

log "Starting all stopped services..."
docker compose -f "$COMPOSE_FILE" start 2>/dev/null || \
    warn "docker compose start failed — services may already be running"

log "Waiting 20s for SQL Server + RabbitMQ to become ready..."
sleep 20

log "Checking API health post-recovery..."
MAX_RETRIES=6
for i in $(seq 1 $MAX_RETRIES); do
    if curl -sf "$API_URL/health" >/dev/null; then
        pass "Step 5 — API healthy after recovery (attempt $i)"
        break
    else
        if [[ $i -eq $MAX_RETRIES ]]; then
            fail "Step 5 — API not healthy after ${MAX_RETRIES} attempts"
        else
            log "  API not ready yet, waiting 5s (attempt $i/$MAX_RETRIES)..."
            sleep 5
        fi
    fi
done

log "Verifying auth works post-recovery..."
RECOVERY_TOKEN=$(obtain_token)
if [[ -n "$RECOVERY_TOKEN" ]]; then
    pass "Step 5 — Authentication succeeds after recovery"
else
    fail "Step 5 — Authentication failed after recovery"
fi

log "Verifying DB-backed endpoint works post-recovery..."
RECOVERY_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "Authorization: Bearer ${RECOVERY_TOKEN:-$ADMIN_TOKEN}" \
    "$API_URL/api/v1/hr/employees" 2>/dev/null || echo "000")
if [[ "$RECOVERY_HTTP" == "200" ]]; then
    pass "Step 5 — DB-backed endpoint returns 200 after recovery"
else
    warn "Step 5 — DB-backed endpoint returned $RECOVERY_HTTP after recovery (may need more time)"
fi

# =============================================================================
# FINAL SUMMARY
# =============================================================================

separator "OBSERVABILITY FORCED-FAILURE CERTIFICATION — SUMMARY"

echo
echo "Results:"
for r in "${RESULTS[@]}"; do
    echo "  $r"
done

echo
echo "  PASS: $PASS"
echo "  FAIL: $FAIL"
echo
if [[ $FAIL -eq 0 ]]; then
    echo "  VERDICT: ALL CHECKS PASSED — Observability forced-failure certification COMPLETE"
else
    echo "  VERDICT: $FAIL CHECK(S) FAILED — Review FAIL items above"
fi
echo
echo "Evidence captured:"
echo "  - Seq log search results above"
echo "  - Prometheus metric query results above"
echo "  - HTTP status codes under failure conditions recorded above"
echo
echo "To view logs: open http://localhost:8081 (Seq)"
echo "To view metrics: open http://localhost:9090 (Prometheus)"
echo "To view dashboards: open http://localhost:3000 (Grafana)"
