#!/usr/bin/env bash
# D1 wire-header proof: stop worker, onboard dev employee, peek RabbitMQ message headers.
set -o pipefail
BASE=http://localhost:8080
RMQ=http://guest:guest@localhost:15672
TS=$(date +%s)
EMAIL="wireproof_${TS}@example.com"
EMPNO="WP-${TS}"

echo "### 1. Stop worker so the message parks in the queue"
docker stop local-karamchari.worker-1 >/dev/null && echo "worker stopped"

echo "### 2. Register + login (tenant=dev)"
curl -s -X POST $BASE/api/identity/register -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" >/dev/null
TOK=$(curl -s -X POST $BASE/api/identity/login -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" \
  | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))")
[ -z "$TOK" ] && { echo "LOGIN FAILED"; exit 1; }
echo "token acquired (len=${#TOK})"

echo "### 3. Onboard dev employee $EMPNO"
ONB=$(curl -s -X POST $BASE/api/v1/hr/employees/ -H "Authorization: Bearer $TOK" \
  -H "Content-Type: application/json" \
  -d "{\"employeeNumber\":\"$EMPNO\",\"legalName\":\"Wire Proof\",\"workEmail\":\"$EMAIL\",\"hiredOn\":\"2026-05-30\"}" \
  -w "\nHTTP:%{http_code}")
echo "onboard response: $ONB"

echo "### 4. Wait for outbox relay (5s interval) to publish to RabbitMQ"
for i in $(seq 1 12); do
  sleep 3
  DEPTH=$(curl -s "$RMQ/api/queues/%2F/EmployeeOnboarded" | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
  echo "  t=$((i*3))s queue depth=$DEPTH"
  [ "$DEPTH" != "0" ] && [ -n "$DEPTH" ] && break
done

echo "### 5. Peek message headers (ack_requeue_true = non-destructive)"
curl -s -X POST "$RMQ/api/queues/%2F/EmployeeOnboarded/get" \
  -H "Content-Type: application/json" \
  -d '{"count":3,"ackmode":"ack_requeue_true","encoding":"auto"}' \
  | python3 -c "
import sys,json
msgs=json.load(sys.stdin)
if not msgs:
    print('NO MESSAGES IN QUEUE')
for m in msgs:
    props=m.get('properties',{})
    hdrs=props.get('headers',{})
    print('--- message ---')
    print('routing_key:', m.get('routing_key'))
    print('header keys:', sorted(hdrs.keys()))
    for k in ('MT-Tenant-Id','MT-Context-Signature','MT-Correlation-Id','MT-Source-Service','MT-Execution-Envelope'):
        v=hdrs.get(k)
        if isinstance(v,str) and len(v)>80: v=v[:80]+'...'
        print(f'  {k} = {v}')
"
echo "### DONE wire-proof"
