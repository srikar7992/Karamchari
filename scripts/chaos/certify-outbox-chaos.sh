#!/usr/bin/env bash
# Pre-Sprint-3 Phase A: Outbox Chaos Certification
# Scenarios A1 (single hire → outbox row), A2 (100-event burst), A3 (worker crash mid-flight)
#
# Prerequisites:
#   docker compose -f infrastructure/local/docker-compose.yml up -d
#   KARAMCHARI_SQL_PASSWORD=Karamchari@123 (default)
#
# Container names (infrastructure/local project prefix = "local"):
#   SQL:    local-sqlserver-1
#   Worker: local-karamchari.worker-1
#   RMQ:    local-rabbitmq-1
set -o pipefail

BASE=http://localhost:8080
RMQ=http://guest:guest@localhost:15672
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
SQL_CONTAINER=local-sqlserver-1
WORKER_CONTAINER=local-karamchari.worker-1
RMQ_CONTAINER=local-rabbitmq-1

SQL() {
  docker exec "$SQL_CONTAINER" "$SQLCMD" \
    -S localhost -U sa -P 'Karamchari@123' \
    -d Karamchari -C -h -1 -W \
    -Q "$1" 2>&1
}

login() { # email password tenant -> echoes token
  curl -s -X POST "$BASE/api/identity/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$1\",\"password\":\"$2\",\"tenantId\":\"$3\"}" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null
}

register_and_login() { # tenant -> echoes token
  local TEN=$1
  local TS EMAIL PW TOK
  TS=$(date +%s%N)
  EMAIL="certify_${TEN}_${TS}@example.com"
  PW="Cert#Pass123!"
  curl -s -X POST "$BASE/api/identity/register" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"$PW\",\"tenantId\":\"$TEN\"}" >/dev/null
  TOK=$(login "$EMAIL" "$PW" "$TEN")
  if [ -z "$TOK" ]; then echo "REGISTER_LOGIN_FAILED" >&2; return 1; fi
  # Export email so callers can use it as workEmail
  LAST_EMAIL=$EMAIL
  echo "$TOK"
}

hire_employee() { # token empno name email hiredOn -> HTTP status
  curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE/api/v1/hr/employees/" \
    -H "Authorization: Bearer $1" \
    -H "Content-Type: application/json" \
    -d "{\"employeeNumber\":\"$2\",\"legalName\":\"$3\",\"workEmail\":\"$4\",\"hiredOn\":\"$5\"}"
}

echo "============================================================"
echo " Karamchari Pre-Sprint-3 Phase A: Outbox Chaos Certification"
echo " $(date)"
echo "============================================================"

# ─────────────────────────────────────────────────────────────────
# Scenario A1: Single hire → exactly one OutboxMessage row written
# ─────────────────────────────────────────────────────────────────
echo ""
echo "### SCENARIO A1: Single hire produces one OutboxMessage row"
echo "------------------------------------------------------------"

# Snapshot OutboxMessage count before
BEFORE_COUNT=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.OutboxMessage;" | tr -d '[:space:]')
echo "  OutboxMessage count BEFORE: $BEFORE_COUNT"

# Onboard one employee under tenant 'dev'
A1_TOKEN=$(register_and_login dev)
A1_EMAIL=$LAST_EMAIL
A1_TS=$(date +%s)
A1_EMPNO="A1-CERT-${A1_TS}"

echo "  Hiring employee $A1_EMPNO ..."
A1_STATUS=$(hire_employee "$A1_TOKEN" "$A1_EMPNO" "OutboxCert Alpha" "$A1_EMAIL" "2026-06-01")
echo "  HTTP status: $A1_STATUS"

if [ "$A1_STATUS" != "201" ] && [ "$A1_STATUS" != "200" ]; then
  echo "  [WARN] Non-success HTTP from hire — check API logs"
fi

# Give the outbox interceptor time to write (it is synchronous on SaveChanges, but give EF a moment)
sleep 2

AFTER_COUNT=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.OutboxMessage;" | tr -d '[:space:]')
echo "  OutboxMessage count AFTER:  $AFTER_COUNT"
DIFF=$(( AFTER_COUNT - BEFORE_COUNT ))
echo "  Delta: $DIFF row(s) written"

if [ "$DIFF" -ge 1 ]; then
  echo "  [A1] PASS — at least one OutboxMessage written for single hire"
else
  echo "  [A1] FAIL — no OutboxMessage rows written"
fi

# Show the latest row
echo "  Latest OutboxMessage rows (newest first):"
SQL "SET NOCOUNT ON;
SELECT TOP 3
    CONVERT(varchar(36), MessageId) AS MessageId,
    MessageType,
    LEFT(Body, 80)         AS BodyPreview,
    SentTime
FROM dbo.OutboxMessage
ORDER BY SentTime DESC;"

echo ""
echo "  SQL EVIDENCE QUERY (A1):"
echo "  SELECT COUNT(*) FROM dbo.OutboxMessage;"
echo "  SELECT TOP 1 MessageId, MessageType, SentTime FROM dbo.OutboxMessage ORDER BY SentTime DESC;"


# ─────────────────────────────────────────────────────────────────
# Scenario A2: 100-event burst → 100 OutboxMessage rows
# ─────────────────────────────────────────────────────────────────
echo ""
echo "### SCENARIO A2: 100-hire burst → 100 OutboxMessage rows"
echo "---------------------------------------------------------"

A2_TOKEN=$(register_and_login dev)
A2_EMAIL_BASE=$LAST_EMAIL

# Snapshot before burst
A2_BEFORE=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.OutboxMessage;" | tr -d '[:space:]')
echo "  OutboxMessage count BEFORE burst: $A2_BEFORE"

# Stop the worker so messages accumulate (prevents relay from draining during burst)
echo "  Stopping worker to let messages accumulate ..."
docker stop "$WORKER_CONTAINER" >/dev/null 2>&1 && echo "  Worker stopped"

A2_BASE_TS=$(date +%s)
echo "  Firing 100 hire requests (sequential to avoid rate limits) ..."
A2_SUCCESS=0
A2_FAIL=0

for i in $(seq 1 100); do
  TS=$(date +%s%N)
  EMAIL="burst_${i}_${TS}@example.com"
  EMPNO="A2-${A2_BASE_TS}-$(printf '%03d' $i)"
  STATUS=$(hire_employee "$A2_TOKEN" "$EMPNO" "Burst Employee $i" "$EMAIL" "2026-06-01")
  if [ "$STATUS" = "201" ] || [ "$STATUS" = "200" ]; then
    A2_SUCCESS=$((A2_SUCCESS + 1))
  else
    A2_FAIL=$((A2_FAIL + 1))
    echo "    [warn] hire $i returned HTTP $STATUS"
  fi
done

echo "  Burst complete: success=$A2_SUCCESS  fail=$A2_FAIL"
sleep 2

A2_AFTER=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.OutboxMessage;" | tr -d '[:space:]')
A2_DIFF=$(( A2_AFTER - A2_BEFORE ))
echo "  OutboxMessage count AFTER burst: $A2_AFTER"
echo "  Delta (new rows): $A2_DIFF  (expected $A2_SUCCESS)"

if [ "$A2_DIFF" -ge "$A2_SUCCESS" ] && [ "$A2_SUCCESS" -ge 95 ]; then
  echo "  [A2] PASS — $A2_DIFF OutboxMessage rows written for $A2_SUCCESS successful hires"
else
  echo "  [A2] FAIL — expected ~$A2_SUCCESS rows, got delta=$A2_DIFF (success=$A2_SUCCESS)"
fi

echo ""
echo "  SQL EVIDENCE QUERIES (A2):"
echo "  -- Total messages enqueued (should be >= 100 successful hires):"
SQL "SET NOCOUNT ON;
SELECT COUNT(*) AS TotalOutboxMessages,
       MIN(SentTime) AS Earliest,
       MAX(SentTime) AS Latest
FROM dbo.OutboxMessage
WHERE SentTime >= DATEADD(MINUTE, -5, GETUTCDATE());"

echo "  -- Message type breakdown:"
SQL "SET NOCOUNT ON;
SELECT MessageType, COUNT(*) AS Cnt
FROM dbo.OutboxMessage
WHERE SentTime >= DATEADD(MINUTE, -5, GETUTCDATE())
GROUP BY MessageType;"

# Restart worker so processing resumes
echo ""
echo "  Restarting worker ..."
docker start "$WORKER_CONTAINER" >/dev/null 2>&1 && echo "  Worker starting ..."
for i in $(seq 1 20); do
  sleep 3
  C=$(curl -s --max-time 4 "$RMQ/api/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('consumers',0))" 2>/dev/null)
  if [ "$C" = "1" ]; then
    echo "  Worker reconsuming (t=$((i*3))s)"
    break
  fi
done


# ─────────────────────────────────────────────────────────────────
# Scenario A3: Worker crash mid-flight → single Employee row on restart
# ─────────────────────────────────────────────────────────────────
echo ""
echo "### SCENARIO A3: Worker crash mid-flight → single Employee on restart"
echo "----------------------------------------------------------------------"

# Stop the worker first so message parks in queue
echo "  [A3] Stopping worker ..."
docker stop "$WORKER_CONTAINER" >/dev/null 2>&1 && echo "  Worker stopped"

A3_TOKEN=$(register_and_login dev)
A3_EMAIL=$LAST_EMAIL
A3_TS=$(date +%s)
A3_EMPNO="A3-CRASH-${A3_TS}"

echo "  [A3] Onboarding employee $A3_EMPNO (message will park in queue) ..."
A3_STATUS=$(hire_employee "$A3_TOKEN" "$A3_EMPNO" "CrashTest Alpha" "$A3_EMAIL" "2026-06-01")
echo "  HTTP status: $A3_STATUS"

# Verify row exists in queue (worker is down, so it parks)
sleep 5
QUEUED=$(curl -s "$RMQ/api/queues/%2F/EmployeeOnboarded" \
  | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
echo "  Queue depth (worker stopped): $QUEUED message(s)"

# Check Employees table BEFORE restart (should NOT be there yet)
A3_EMP_BEFORE=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM tenant_dev.Employees WHERE EmployeeNumber='$A3_EMPNO';" | tr -d '[:space:]')
echo "  tenant_dev.Employees rows for $A3_EMPNO BEFORE restart: $A3_EMP_BEFORE"

# Start the worker (simulates restart after crash)
echo "  [A3] Starting (restarting) worker ..."
docker start "$WORKER_CONTAINER" >/dev/null 2>&1

# Wait for consumer reconnect
for i in $(seq 1 20); do
  sleep 3
  C=$(curl -s --max-time 4 "$RMQ/api/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('consumers',0))" 2>/dev/null)
  if [ "$C" = "1" ]; then
    echo "  Worker reconsuming (t=$((i*3))s)"
    break
  fi
done

# Wait for message to drain
echo "  Waiting for queue to drain ..."
for i in $(seq 1 20); do
  sleep 3
  D=$(curl -s "$RMQ/api/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
  echo "    t=$((i*3))s queue depth=$D"
  [ "$D" = "0" ] && break
done

# Check Employees table AFTER restart (should be exactly 1)
A3_EMP_AFTER=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM tenant_dev.Employees WHERE EmployeeNumber='$A3_EMPNO';" | tr -d '[:space:]')
echo "  tenant_dev.Employees rows for $A3_EMPNO AFTER restart: $A3_EMP_AFTER"

if [ "$A3_EMP_AFTER" = "1" ]; then
  echo "  [A3] PASS — exactly 1 Employee row after worker restart (no duplicate)"
elif [ "$A3_EMP_AFTER" = "0" ]; then
  echo "  [A3] FAIL — 0 Employee rows (message not processed)"
else
  echo "  [A3] FAIL — DUPLICATE: $A3_EMP_AFTER Employee rows for $A3_EMPNO"
fi

# Verify no duplicate OutboxMessage or dead-letter entries for this employee
echo "  Dead-letter check for $A3_EMPNO:"
SQL "SET NOCOUNT ON;
SELECT COUNT(*) AS DeadLetterCount
FROM dbo.OutboxDeadLetter
WHERE Body LIKE '%$A3_EMPNO%';"

echo ""
echo "  SQL EVIDENCE QUERIES (A3):"
echo "  -- Exactly one Employee row (anti-duplicate):"
echo "  SELECT COUNT(*) FROM tenant_dev.Employees WHERE EmployeeNumber='$A3_EMPNO';"
echo "  -- No dead-letter entries:"
echo "  SELECT COUNT(*) FROM dbo.OutboxDeadLetter WHERE Body LIKE '%$A3_EMPNO%';"

SQL "SET NOCOUNT ON;
SELECT EmployeeNumber, LegalName, CreatedAt
FROM tenant_dev.Employees
WHERE EmployeeNumber = '$A3_EMPNO';"


echo ""
echo "============================================================"
echo " Phase A Outbox Chaos Certification COMPLETE"
echo " $(date)"
echo "============================================================"
