#!/usr/bin/env bash
# Phase 7 security live probes against running API.
BASE=http://localhost:8080
ACME_EMP="292a5e18-8f93-4fa7-a1e0-b8080b5b15e4"   # known acme PayrollProfile/employee id
code() { curl -s -o /dev/null -w "%{http_code}" "$@"; }

echo "### 1. No token -> protected endpoint (expect 401)"
echo "  GET /api/v1/hr/employees/ -> $(code $BASE/api/v1/hr/employees/)"

echo "### 2. alg=none forged token (expect 401)"
# header {"alg":"none","typ":"JWT"} . payload {tenant_id:dev,...} . (empty sig)
H=$(printf '{"alg":"none","typ":"JWT"}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
P=$(printf '{"sub":"attacker","tenant_id":"dev","exp":9999999999,"iss":"https://karamchari.com","aud":"https://karamchari.com"}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
NONE_TOK="$H.$P."
echo "  alg=none -> $(code -H "Authorization: Bearer $NONE_TOK" $BASE/api/v1/hr/employees/)"

echo "### 3. Garbage/tampered token (expect 401)"
echo "  garbage -> $(code -H "Authorization: Bearer not.a.jwt" $BASE/api/v1/hr/employees/)"

echo "### 4. Valid dev token -> own data (expect 200) + cross-tenant read of acme emp (expect 404/empty)"
TS=$(date +%s)
EMAIL="sec_dev_${TS}@example.com"
curl -s -X POST $BASE/api/identity/register -H "Content-Type: application/json" -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" >/dev/null
TOK=$(curl -s -X POST $BASE/api/identity/login -H "Content-Type: application/json" -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))")
echo "  dev own GET list -> $(code -H "Authorization: Bearer $TOK" $BASE/api/v1/hr/employees/)"
echo "  dev cross-tenant GET acme emp $ACME_EMP -> $(code -H "Authorization: Bearer $TOK" $BASE/api/v1/hr/employees/$ACME_EMP)"

echo "### 5. Security headers on a response"
curl -s -D - -o /dev/null $BASE/health/live | grep -iE "x-content-type-options|x-frame-options|content-security-policy|strict-transport|referrer-policy|x-xss" | sed 's/^/  /' || echo "  (none found)"

echo "### 6. Rate limiting on /api/identity/login (RequireRateLimiting auth) — 25 rapid hits"
N429=0; N200=0; NOTHER=0
for i in $(seq 1 25); do
  c=$(code -X POST $BASE/api/identity/login -H "Content-Type: application/json" -d '{"email":"x@x.com","password":"wrong","tenantId":"dev"}')
  case $c in 429) N429=$((N429+1));; 200|401) N200=$((N200+1));; *) NOTHER=$((NOTHER+1));; esac
done
echo "  results: 429=$N429  200/401=$N200  other=$NOTHER"
echo "### DONE security-probe"
