#!/usr/bin/env bash
BASE=http://localhost:8080
code() { curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$@"; }

echo "### Phase 10 — Observability stack reachability"
echo "  Seq (8081):        $(code http://localhost:8081)"
echo "  Prometheus healthy: $(code http://localhost:9090/-/healthy)"
echo "  Grafana health:     $(code http://localhost:3000/api/health)"
echo "  Mailpit (8025):     $(code http://localhost:8025)"
echo "  --- Prometheus scrape targets (up count) ---"
curl -s --max-time 5 http://localhost:9090/api/v1/targets | python3 -c "
import sys,json
try:
  d=json.load(sys.stdin); ts=d['data']['activeTargets']
  up=[t for t in ts if t['health']=='up']; down=[t for t in ts if t['health']!='up']
  print(f'  targets up={len(up)} down={len(down)}')
  for t in ts: print(f\"    {t['labels'].get('job','?')}: {t['health']}\")
except Exception as e: print('  prom targets parse err:',e)
"
echo "  --- API /metrics: tenant + http server metrics present? ---"
curl -s --max-time 5 $BASE/metrics 2>/dev/null | grep -ciE "tenant|http_server|kestrel|process_" | sed 's/^/  metric lines matched: /' || echo "  /metrics not exposed on :8080"

echo "### Phase 8 — Perf sample on rebuilt API (business endpoint, light concurrency)"
TS=$(date +%s); EMAIL="perf_${TS}@example.com"
curl -s -X POST $BASE/api/identity/register -H "Content-Type: application/json" -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" >/dev/null
TOK=$(curl -s -X POST $BASE/api/identity/login -H "Content-Type: application/json" -d "{\"email\":\"$EMAIL\",\"password\":\"Cert#Pass123!\",\"tenantId\":\"dev\"}" | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))")
python3 - "$BASE" "$TOK" <<'PY'
import sys,urllib.request,time
base,tok=sys.argv[1],sys.argv[2]
lat=[]
for _ in range(60):
    r=urllib.request.Request(base+"/api/v1/hr/employees/",headers={"Authorization":"Bearer "+tok})
    t=time.time()
    try:
        urllib.request.urlopen(r,timeout=10).read()
    except Exception as e:
        print("req err",e); continue
    lat.append((time.time()-t)*1000)
lat.sort()
n=len(lat)
def p(q): return lat[min(n-1,int(q*n))]
print(f"  n={n} p50={p(.5):.0f}ms p90={p(.9):.0f}ms p95={p(.95):.0f}ms p99={p(.99):.0f}ms max={lat[-1]:.0f}ms")
PY
echo "### DONE obs-perf-sweep"
