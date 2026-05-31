#!/usr/bin/env bash
# D1 end-to-end + multi-tenant isolation proof.
set -o pipefail
BASE=http://localhost:8080
RMQ=http://guest:guest@localhost:15672
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
SQL() { docker exec local-sqlserver-1 $SQLCMD -S localhost -U sa -P 'Karamchari@123' -d Karamchari -C -h -1 -W -Q "$1" 2>&1; }

onboard() { # tenant empno name
  local TEN=$1 EMPNO=$2 NAME=$3 TS EMAIL TOK
  TS=$(date +%s%N)
  EMAIL="e2e_${TEN}_${TS}@example.com"
  curl -s -X POST $BASE/api/identity/register -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"$TEN\"}" >/dev/null
  TOK=$(curl -s -X POST $BASE/api/identity/login -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"$TEN\"}" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))")
  if [ -z "$TOK" ]; then echo "[$TEN] LOGIN FAILED"; return 1; fi
  curl -s -X POST $BASE/api/v1/hr/employees/ -H "Authorization: Bearer $TOK" \
    -H "Content-Type: application/json" \
    -d "{\"employeeNumber\":\"$EMPNO\",\"legalName\":\"$NAME\",\"workEmail\":\"$EMAIL\",\"hiredOn\":\"2026-05-30\"}" \
    -w " HTTP:%{http_code}\n"
}

echo "### 1. Start worker"
docker start local-karamchari.worker-1 >/dev/null && echo "worker starting"
for i in $(seq 1 20); do
  sleep 3
  C=$(curl -s "$RMQ/api/queues/%2F/EmployeeOnboarded" | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('consumers',0))" 2>/dev/null)
  echo "  t=$((i*3))s consumers=$C"
  [ "$C" = "1" ] && break
done

echo "### 2. Drain parked dev message (queue -> 0)"
for i in $(seq 1 10); do
  sleep 2
  D=$(curl -s "$RMQ/api/queues/%2F/EmployeeOnboarded" | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('messages',0))" 2>/dev/null)
  echo "  t=$((i*2))s depth=$D"
  [ "$D" = "0" ] && break
done

echo "### 3. Concurrent multi-tenant onboard (acme + contoso + 2nd dev)"
T=$(date +%s)
onboard acme    "ACME-$T"    "Acme Person"    &
onboard contoso "CONTOSO-$T" "Contoso Person" &
onboard dev     "DEV2-$T"    "Dev Second"     &
wait
echo "### 4. Wait for async consumption"
sleep 12

echo "### 5. PayrollProfiles by schema + TenantId stamp"
SQL "SET NOCOUNT ON;
SELECT s.name + '.PayrollProfiles', p.rows
FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id
JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1)
WHERE t.name='PayrollProfiles' AND (s.name LIKE 'tenant[_]%' OR s.name='dbo') ORDER BY s.name;"

echo "--- dbo rows (should be unchanged baseline=7, none new) ---"
SQL "SET NOCOUNT ON; SELECT TenantId, COUNT(*) FROM dbo.PayrollProfiles GROUP BY TenantId;"
echo "--- tenant_dev rows (TenantId stamp) ---"
SQL "SET NOCOUNT ON; SELECT TenantId, COUNT(*) FROM tenant_dev.PayrollProfiles GROUP BY TenantId;"
echo "--- tenant_acme rows ---"
SQL "SET NOCOUNT ON; SELECT TenantId, COUNT(*) FROM tenant_acme.PayrollProfiles GROUP BY TenantId;"
echo "--- tenant_contoso rows ---"
SQL "SET NOCOUNT ON; SELECT TenantId, COUNT(*) FROM tenant_contoso.PayrollProfiles GROUP BY TenantId;"

echo "### 6. Error/skipped queues (should be absent/empty)"
curl -s "$RMQ/api/queues" | python3 -c "
import sys,json
qs=json.load(sys.stdin)
bad=[q for q in qs if ('error' in q['name'].lower() or 'skipped' in q['name'].lower())]
print('\n'.join(f\"{q['name']}: messages={q.get('messages',0)}\" for q in bad) or 'NO error/skipped queues')
"
echo "### DONE e2e-proof"
