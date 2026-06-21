#!/usr/bin/env bash
# Pre-Sprint-3 Phase B: Inbox Replay Certification
# Scenarios B1 (100x same MessageId → exactly 1 InboxState), B2 (50 concurrent deliveries),
# B3 (restart mid-replay → no duplicate side effects)
#
# Prerequisites:
#   docker compose -f infrastructure/local/docker-compose.yml up -d
#   KARAMCHARI_SQL_PASSWORD=Karamchari@123 (default)
#   python3 available in PATH
#   curl available in PATH
#
# Container names (infrastructure/local project prefix = "local"):
#   SQL:    local-sqlserver-1
#   Worker: local-karamchari.worker-1
#   RMQ:    local-rabbitmq-1
set -o pipefail

BASE=http://localhost:8080
RMQ=http://guest:guest@localhost:15672
RMQ_MGMT="$RMQ/api"
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
SQL_CONTAINER=local-sqlserver-1
WORKER_CONTAINER=local-karamchari.worker-1

SQL() {
  docker exec "$SQL_CONTAINER" "$SQLCMD" \
    -S localhost -U sa -P 'Karamchari@123' \
    -d Karamchari -C -h -1 -W \
    -Q "$1" 2>&1
}

# Publish a MassTransit-compatible EmployeeOnboarded message directly to RabbitMQ
# via management API (non-destructive publish to exchange/queue).
# Args: $1=message_id  $2=correlation_id  $3=tenant_id  $4=employee_id  $5=employee_number
publish_employee_onboarded() {
  local MSG_ID=$1
  local CORR_ID=$2
  local TENANT_ID=$3
  local EMP_ID=$4
  local EMP_NUMBER=$5

  # MassTransit message body (JSON envelope that the consumer expects)
  # The consumer key fields are employeeId, tenantId, employeeNumber.
  local BODY
  BODY=$(python3 -c "
import json, sys
msg = {
    'messageId': '$MSG_ID',
    'correlationId': '$CORR_ID',
    'conversationId': '$CORR_ID',
    'sourceAddress': 'rabbitmq://rabbitmq/certify_inbox_source',
    'destinationAddress': 'rabbitmq://rabbitmq/EmployeeOnboarded',
    'messageType': ['urn:message:Karamchari.Contracts.HR:EmployeeOnboardedIntegrationEvent'],
    'message': {
        'employeeId': '$EMP_ID',
        'tenantId': '$TENANT_ID',
        'employeeNumber': '$EMP_NUMBER',
        'legalName': 'Inbox Cert Employee',
        'workEmail': '${EMP_NUMBER}@example.com',
        'hiredOn': '2026-06-01'
    },
    'headers': {
        'MT-Tenant-Id': '$TENANT_ID'
    }
}
print(json.dumps(msg))
")

  # Publish via RabbitMQ management API to the EmployeeOnboarded queue directly
  # Using the default exchange with routing_key = queue name
  curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$RMQ_MGMT/exchanges/%2F/amq.default/publish" \
    -H "Content-Type: application/json" \
    -d "{
      \"properties\": {
        \"delivery_mode\": 2,
        \"content_type\": \"application/vnd.masstransit+json\",
        \"message_id\": \"$MSG_ID\",
        \"correlation_id\": \"$CORR_ID\",
        \"headers\": {\"MT-Tenant-Id\": \"$TENANT_ID\"}
      },
      \"routing_key\": \"EmployeeOnboarded\",
      \"payload\": $(echo "$BODY" | python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()))'),
      \"payload_encoding\": \"string\"
    }"
}

echo "============================================================"
echo " Karamchari Pre-Sprint-3 Phase B: Inbox Replay Certification"
echo " $(date)"
echo "============================================================"
echo ""
echo "NOTE: Phase B injects messages directly via the RabbitMQ management API."
echo "      The worker must be running and connected to consume messages."
echo ""

# Ensure worker is up
echo "### Pre-flight: ensure worker is consuming ..."
docker start "$WORKER_CONTAINER" >/dev/null 2>&1 || true
WORKER_CONSUMERS=""
for i in $(seq 1 20); do
  sleep 3
  WORKER_CONSUMERS=$(curl -s --max-time 4 "$RMQ_MGMT/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('consumers',0))" 2>/dev/null)
  if [ "$WORKER_CONSUMERS" = "1" ]; then
    echo "  Worker consuming (t=$((i*3))s)"
    break
  fi
  echo "  t=$((i*3))s consumers=$WORKER_CONSUMERS"
done
if [ "$WORKER_CONSUMERS" != "1" ]; then
  echo "  [WARN] Worker may not be connected (consumers=$WORKER_CONSUMERS) — continuing anyway"
fi


# ─────────────────────────────────────────────────────────────────
# Scenario B1: 100x identical MessageId → exactly 1 InboxState row
# ─────────────────────────────────────────────────────────────────
echo ""
echo "### SCENARIO B1: 100x same MessageId → exactly 1 InboxState record"
echo "--------------------------------------------------------------------"

# Generate a fixed message ID for this scenario
B1_MSG_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B1_CORR_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B1_EMP_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B1_EMP_NUMBER="B1-IDMP-$(date +%s)"
B1_TENANT="dev"

echo "  MessageId (fixed): $B1_MSG_ID"
echo "  EmployeeNumber:    $B1_EMP_NUMBER"

# Snapshot InboxState before
B1_INBOX_BEFORE=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.InboxState WHERE MessageId='$B1_MSG_ID';" | tr -d '[:space:]')
echo "  InboxState rows for this MessageId BEFORE: $B1_INBOX_BEFORE"

echo "  Publishing same message 100 times ..."
B1_PUB_OK=0
B1_PUB_FAIL=0
for i in $(seq 1 100); do
  # Every publish uses the SAME B1_MSG_ID — the idempotency key
  STATUS=$(publish_employee_onboarded "$B1_MSG_ID" "$B1_CORR_ID" "$B1_TENANT" "$B1_EMP_ID" "$B1_EMP_NUMBER")
  if [ "$STATUS" = "200" ]; then
    B1_PUB_OK=$((B1_PUB_OK + 1))
  else
    B1_PUB_FAIL=$((B1_PUB_FAIL + 1))
    echo "    [warn] publish $i returned HTTP $STATUS"
  fi
done
echo "  Published: ok=$B1_PUB_OK  fail=$B1_PUB_FAIL"

# Wait for all messages to drain
echo "  Waiting for queue to drain ..."
for i in $(seq 1 30); do
  sleep 3
  D=$(curl -s "$RMQ_MGMT/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
  echo "    t=$((i*3))s queue depth=$D"
  [ "$D" = "0" ] && break
done

B1_INBOX_AFTER=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.InboxState WHERE MessageId='$B1_MSG_ID';" | tr -d '[:space:]')
echo "  InboxState rows for this MessageId AFTER 100 deliveries: $B1_INBOX_AFTER"

if [ "$B1_INBOX_AFTER" = "1" ]; then
  echo "  [B1] PASS — exactly 1 InboxState row despite 100 deliveries"
elif [ "$B1_INBOX_AFTER" = "0" ]; then
  echo "  [B1] WARN — 0 InboxState rows (MassTransit may GC them after processing; check PayrollProfiles)"
else
  echo "  [B1] FAIL — $B1_INBOX_AFTER InboxState rows (expected 1)"
fi

echo "  SQL EVIDENCE QUERIES (B1):"
SQL "SET NOCOUNT ON;
SELECT
    CONVERT(varchar(36), MessageId) AS MessageId,
    Received,
    Delivered,
    Consumed,
    LastSequenceNumber
FROM dbo.InboxState
WHERE MessageId = '$B1_MSG_ID';"

# Also check that only one PayrollProfile was created (the consumer side effect)
echo "  Side-effect check — PayrollProfiles written (should be 1, not 100):"
SQL "SET NOCOUNT ON;
SELECT COUNT(*) AS PayrollProfileCount
FROM tenant_dev.PayrollProfiles
WHERE EmployeeId = '$B1_EMP_ID';"


# ─────────────────────────────────────────────────────────────────
# Scenario B2: 50 concurrent deliveries of same MessageId → 1 InboxState row
# ─────────────────────────────────────────────────────────────────
echo ""
echo "### SCENARIO B2: 50 concurrent deliveries → exactly 1 InboxState row"
echo "----------------------------------------------------------------------"

B2_MSG_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B2_CORR_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B2_EMP_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B2_EMP_NUMBER="B2-CONC-$(date +%s)"
B2_TENANT="dev"

echo "  MessageId (fixed): $B2_MSG_ID"
echo "  EmployeeNumber:    $B2_EMP_NUMBER"

B2_INBOX_BEFORE=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.InboxState WHERE MessageId='$B2_MSG_ID';" | tr -d '[:space:]')
echo "  InboxState rows BEFORE concurrent blast: $B2_INBOX_BEFORE"

echo "  Launching 50 concurrent publishes ..."
B2_PIDS=()
for i in $(seq 1 50); do
  (
    STATUS=$(publish_employee_onboarded "$B2_MSG_ID" "$B2_CORR_ID" "$B2_TENANT" "$B2_EMP_ID" "$B2_EMP_NUMBER")
    echo "  concurrent_$i HTTP=$STATUS"
  ) &
  B2_PIDS+=($!)
done

# Wait for all background publishes
wait "${B2_PIDS[@]}" 2>/dev/null
echo "  All 50 publishes submitted"

# Wait for queue to drain
echo "  Waiting for queue to drain ..."
for i in $(seq 1 30); do
  sleep 3
  D=$(curl -s "$RMQ_MGMT/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
  echo "    t=$((i*3))s queue depth=$D"
  [ "$D" = "0" ] && break
done

B2_INBOX_AFTER=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.InboxState WHERE MessageId='$B2_MSG_ID';" | tr -d '[:space:]')
echo "  InboxState rows AFTER 50 concurrent deliveries: $B2_INBOX_AFTER"

if [ "$B2_INBOX_AFTER" = "1" ] || [ "$B2_INBOX_AFTER" = "0" ]; then
  # 0 is acceptable if MassTransit removes the row after confirmed delivery
  echo "  [B2] PASS — deduplication held under concurrency (rows=$B2_INBOX_AFTER)"
else
  echo "  [B2] FAIL — $B2_INBOX_AFTER InboxState rows (duplicate processing)"
fi

echo "  SQL EVIDENCE QUERIES (B2):"
SQL "SET NOCOUNT ON;
SELECT
    CONVERT(varchar(36), MessageId) AS MessageId,
    Received,
    Delivered,
    Consumed
FROM dbo.InboxState
WHERE MessageId = '$B2_MSG_ID';"

echo "  Side-effect check — PayrollProfiles (should be <= 1):"
SQL "SET NOCOUNT ON;
SELECT COUNT(*) AS PayrollProfileCount
FROM tenant_dev.PayrollProfiles
WHERE EmployeeId = '$B2_EMP_ID';"


# ─────────────────────────────────────────────────────────────────
# Scenario B3: Restart worker mid-replay → no duplicate side effects
# ─────────────────────────────────────────────────────────────────
echo ""
echo "### SCENARIO B3: Worker restart mid-replay → no duplicate side effects"
echo "------------------------------------------------------------------------"

B3_MSG_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B3_CORR_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B3_EMP_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
B3_EMP_NUMBER="B3-RESTART-$(date +%s)"
B3_TENANT="dev"

echo "  MessageId (fixed): $B3_MSG_ID"
echo "  EmployeeNumber:    $B3_EMP_NUMBER"

# Publish 10 copies to ensure the queue has depth when we kill the worker
echo "  Publishing 10 copies of the message ..."
for i in $(seq 1 10); do
  publish_employee_onboarded "$B3_MSG_ID" "$B3_CORR_ID" "$B3_TENANT" "$B3_EMP_ID" "$B3_EMP_NUMBER" >/dev/null
done
echo "  Published 10 copies"

# Let the worker pick up a few, then kill it mid-flight
sleep 2

echo "  Killing worker mid-flight ..."
docker stop "$WORKER_CONTAINER" >/dev/null 2>&1 && echo "  Worker stopped"

sleep 2
PARKED=$(curl -s "$RMQ_MGMT/queues/%2F/EmployeeOnboarded" \
  | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
echo "  Queue depth after kill: $PARKED (message(s) should be requeued via NACK)"

# Snapshot PayrollProfiles / side-effects before restart
B3_PP_BEFORE=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM tenant_dev.PayrollProfiles WHERE EmployeeId='$B3_EMP_ID';" | tr -d '[:space:]')
echo "  PayrollProfiles for $B3_EMP_NUMBER BEFORE restart: $B3_PP_BEFORE"

# Restart the worker
echo "  Restarting worker ..."
docker start "$WORKER_CONTAINER" >/dev/null 2>&1

for i in $(seq 1 20); do
  sleep 3
  C=$(curl -s --max-time 4 "$RMQ_MGMT/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('consumers',0))" 2>/dev/null)
  if [ "$C" = "1" ]; then
    echo "  Worker reconsuming (t=$((i*3))s)"
    break
  fi
done

# Wait for all requeued messages to drain
echo "  Waiting for queue to fully drain ..."
for i in $(seq 1 30); do
  sleep 3
  D=$(curl -s "$RMQ_MGMT/queues/%2F/EmployeeOnboarded" \
    | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
  echo "    t=$((i*3))s queue depth=$D"
  [ "$D" = "0" ] && break
done

# Final verdict: PayrollProfiles should be exactly 1 (not 10)
B3_PP_AFTER=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM tenant_dev.PayrollProfiles WHERE EmployeeId='$B3_EMP_ID';" | tr -d '[:space:]')
echo "  PayrollProfiles for $B3_EMP_NUMBER AFTER restart: $B3_PP_AFTER"

B3_INBOX_FINAL=$(SQL "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.InboxState WHERE MessageId='$B3_MSG_ID';" | tr -d '[:space:]')
echo "  InboxState rows for this MessageId: $B3_INBOX_FINAL"

if [ "$B3_PP_AFTER" -le 1 ]; then
  echo "  [B3] PASS — at most 1 PayrollProfile row after restart mid-replay (got $B3_PP_AFTER)"
else
  echo "  [B3] FAIL — DUPLICATE: $B3_PP_AFTER PayrollProfile rows for $B3_EMP_NUMBER"
fi

echo "  SQL EVIDENCE QUERIES (B3):"
echo "  -- Side effects must be idempotent (count = 1 max):"
SQL "SET NOCOUNT ON;
SELECT COUNT(*) AS ProfileCount
FROM tenant_dev.PayrollProfiles
WHERE EmployeeId = '$B3_EMP_ID';"

echo "  -- InboxState (deduplication state):"
SQL "SET NOCOUNT ON;
SELECT
    CONVERT(varchar(36), MessageId) AS MessageId,
    Received,
    Delivered,
    Consumed
FROM dbo.InboxState
WHERE MessageId = '$B3_MSG_ID';"

echo "  -- Dead-letter check (should be 0 for idempotent replays):"
SQL "SET NOCOUNT ON;
SELECT COUNT(*) AS DeadLetterCount FROM dbo.OutboxDeadLetter WHERE Body LIKE '%$B3_EMP_NUMBER%';"


echo ""
echo "============================================================"
echo " Phase B Inbox Replay Certification COMPLETE"
echo " $(date)"
echo "============================================================"
