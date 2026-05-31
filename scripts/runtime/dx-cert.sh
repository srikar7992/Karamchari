#!/usr/bin/env bash
# Module/API/DX certification: default creds, OpenAPI security fix, globex, module cross-section.
BASE=http://localhost:8080
PW='Dev@Pass123!'
code() { curl -s -o /dev/null -w "%{http_code}" "$@"; }
login() { # email -> echoes token
  curl -s -X POST $BASE/api/identity/login -H "Content-Type: application/json" \
    -d "{\"email\":\"$1\",\"password\":\"$PW\",\"tenantId\":\"$2\"}" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null
}

echo "### 1. Default credentials login (Phase 7)"
for u in admin@dev.local manager@dev.local employee@dev.local readonly@dev.local admin@acme.local admin@globex.local; do
  t="${u#*@}"; tn="${t%.local}"
  TOK=$(login "$u" "$tn")
  echo "  $u -> $([ -n "$TOK" ] && echo "OK (token ${#TOK} chars)" || echo "FAIL")"
done

echo "### 2. OpenAPI per-operation security (Phase 5 fix)"
curl -s $BASE/openapi/v1.json -o /tmp/openapi2.json
python3 - <<'PY'
import json
d=json.load(open('/tmp/openapi2.json'))
prot=anon=0; samples={'protected':[],'anon':[]}
for route,item in d['paths'].items():
    for m,op in item.items():
        if m not in('get','post','put','delete','patch'): continue
        if op.get('security'):
            prot+=1
            if len(samples['protected'])<3: samples['protected'].append(f"{m.upper()} {route}")
        else:
            anon+=1
            if len(samples['anon'])<6: samples['anon'].append(f"{m.upper()} {route}")
print(f"  protected (have security)={prot}  anonymous (no security)={anon}")
print("  anon sample:", samples['anon'])
print("  protected sample:", samples['protected'])
PY

echo "### 3. globex tenant isolation (new tenant) — onboard + verify"
GTOK=$(login admin@globex.local globex)
TS=$(date +%s)
echo "  onboard globex employee: $(curl -s -o /dev/null -w '%{http_code}' -X POST $BASE/api/v1/hr/employees/ -H "Authorization: Bearer $GTOK" -H 'Content-Type: application/json' -d "{\"employeeNumber\":\"GLX-$TS\",\"legalName\":\"Globex One\",\"workEmail\":\"glx$TS@globex.local\",\"hiredOn\":\"2026-05-30\"}")"

echo "### 4. Module cross-section (admin@dev) — positive GETs"
DTOK=$(login admin@dev.local dev)
for ep in \
  "/api/v1/hr/employees/" \
  "/api/v1/time/leave-balances" \
  "/api/v1/time/holidays" \
  "/api/v1/workforce/attendance/sessions/live" \
  "/api/v1/capability/skills" \
  "/api/v1/leaves/my" \
  "/api/v1/approvals/my" \
  "/api/v1/billing/ar/summary" \
  "/api/v1/strategy/health" ; do
  echo "  GET $ep -> $(code -H "Authorization: Bearer $DTOK" $BASE$ep)"
done

echo "### 5. Negative + security paths"
echo "  no-token GET employees -> $(code $BASE/api/v1/hr/employees/)"
echo "  bad-token GET employees -> $(code -H 'Authorization: Bearer x.y.z' $BASE/api/v1/hr/employees/)"
echo "  readonly token GET employees (should 200 read) -> $(code -H "Authorization: Bearer $(login readonly@dev.local dev)" $BASE/api/v1/hr/employees/)"
echo "### DONE dx-cert"
