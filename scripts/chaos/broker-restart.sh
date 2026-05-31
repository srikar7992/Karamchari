#!/usr/bin/env bash
# Phase 9 chaos: RabbitMQ outage + recovery + post-outage tenant isolation.
BASE=http://localhost:8080
RMQ=http://guest:guest@localhost:15672
SQL() { docker exec local-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karamchari@123' -d Karamchari -C -h -1 -W -Q "$1" 2>&1; }

echo "### Pre: tenant_dev.PayrollProfiles"
SQL "SET NOCOUNT ON; SELECT p.rows FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1) WHERE t.name='PayrollProfiles' AND s.name='tenant_dev';"

echo "### 1. Kill RabbitMQ"
docker stop local-rabbitmq-1 >/dev/null && echo "rabbitmq stopped at $(date +%T)"

echo "### 2. API behaviour during outage (health/ready may degrade)"
sleep 4
echo "  health/ready during outage: $(curl -s -o /dev/null -w '%{http_code}' --max-time 5 $BASE/health/ready)"

echo "### 3. Restart RabbitMQ + measure recovery"
T0=$(date +%s)
docker start local-rabbitmq-1 >/dev/null && echo "rabbitmq starting at $(date +%T)"
for i in $(seq 1 40); do
  sleep 3
  UP=$(curl -s -o /dev/null -w '%{http_code}' --max-time 4 "$RMQ/api/overview")
  if [ "$UP" = "200" ]; then echo "  broker mgmt up after $(( $(date +%s)-T0 ))s"; break; fi
done
# wait for bus reconnect (queue has a consumer again)
for i in $(seq 1 30); do
  sleep 3
  C=$(curl -s --max-time 4 "$RMQ/api/queues/%2F/EmployeeOnboarded" | python3 -c "import sys,json;print(json.load(sys.stdin).get('consumers',0))" 2>/dev/null)
  if [ "$C" = "1" ]; then echo "  worker reconsuming after $(( $(date +%s)-T0 ))s (RTO)"; break; fi
done

echo "### 4. Post-recovery onboard (dev) must land in tenant_dev"
TS=$(date +%s); EMAIL="chaos_${TS}@example.com"
curl -s -X POST $BASE/api/identity/register -H "Content-Type: application/json" -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" >/dev/null
TOK=$(curl -s -X POST $BASE/api/identity/login -H "Content-Type: application/json" -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))")
curl -s -X POST $BASE/api/v1/hr/employees/ -H "Authorization: Bearer $TOK" -H "Content-Type: application/json" -d "{\"employeeNumber\":\"CHAOS-$TS\",\"legalName\":\"Chaos Survivor\",\"workEmail\":\"$EMAIL\",\"hiredOn\":\"2026-05-30\"}" -w " HTTP:%{http_code}\n"
sleep 12
echo "### Post: tenant_dev (should be +1) and dbo (should be unchanged=7)"
SQL "SET NOCOUNT ON; SELECT s.name+'.PayrollProfiles', p.rows FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1) WHERE t.name='PayrollProfiles' AND s.name IN('tenant_dev','dbo') ORDER BY s.name;"
echo "### DONE broker-restart"
