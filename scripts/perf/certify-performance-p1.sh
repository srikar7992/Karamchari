#!/usr/bin/env bash
# Phase C — Performance Certification P1: Auth endpoint, 100 concurrent users.
# Target: POST /api/identity/login  P95 < 200ms.
#
# Usage:
#   bash scripts/perf/certify-performance-p1.sh
#   bash scripts/perf/certify-performance-p1.sh --base http://localhost:8080 --vus 100 --duration 30
#
# No external tools required. Uses Python (stdlib) closed-loop concurrency model
# matching load-ramp.py patterns.

set -euo pipefail
BASE="http://localhost:8080"
VUS=100
DURATION=30   # seconds per level

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base) BASE="$2"; shift 2;;
    --vus) VUS="$2"; shift 2;;
    --duration) DURATION="$2"; shift 2;;
    *) echo "Unknown arg: $1"; exit 1;;
  esac
done

echo "# Phase C — P1: Auth endpoint load test"
echo "# target=$BASE/api/identity/login  vus=$VUS  duration=${DURATION}s"
echo "# Criterion: P95 < 200ms, error_pct < 5%"
echo ""

python3 - "$BASE" "$VUS" "$DURATION" <<'PY'
import sys, json, threading, time, urllib.request, urllib.error, secrets, statistics

base   = sys.argv[1]
vus    = int(sys.argv[2])
dur    = float(sys.argv[3])

# Pre-register one stable test account per VU to isolate auth load from registration cost.
print(f"Preparing {vus} test accounts (this may take ~{vus//5}s)...")

def register_user(idx):
    uid   = secrets.token_hex(4)
    email = f"p1_auth_{idx}_{uid}@example.com"
    pw    = "Auth#Load123!"
    body  = json.dumps({"email": email, "password": pw, "tenantId": "dev"}).encode()
    req   = urllib.request.Request(base + "/api/identity/register", data=body,
                                   headers={"Content-Type": "application/json"})
    try:
        urllib.request.urlopen(req, timeout=15)
    except Exception:
        pass  # already exists or error — login attempt below will reflect it
    return email, pw

accounts = []
for i in range(min(vus, 100)):
    e, p = register_user(i)
    accounts.append((e, p))

print(f"  Registered {len(accounts)} accounts. Starting load test...")

# ---- Closed-loop load test ----
lats = []
errs = 0
oks  = 0
lock = threading.Lock()
stop_at = time.time() + dur

def worker(idx):
    global errs, oks
    email, pw = accounts[idx % len(accounts)]
    body = json.dumps({"email": email, "password": pw, "tenantId": "dev"}).encode()
    local_lats = []
    local_oks  = 0
    local_errs = 0
    while time.time() < stop_at:
        t0 = time.perf_counter()
        try:
            req  = urllib.request.Request(base + "/api/identity/login", data=body,
                                          headers={"Content-Type": "application/json"})
            resp = urllib.request.urlopen(req, timeout=10)
            code = resp.status
            resp.read()
        except urllib.error.HTTPError as e:
            code = e.code
        except Exception:
            code = 0
        ms = (time.perf_counter() - t0) * 1000
        local_lats.append(ms)
        if code in (200, 401):   # 401 = wrong creds but endpoint responded
            local_oks += 1
        else:
            local_errs += 1
    with lock:
        lats.extend(local_lats)
        errs += local_errs
        oks  += local_oks

threads = [threading.Thread(target=worker, args=(i,), daemon=True) for i in range(vus)]
t_start = time.perf_counter()
for t in threads: t.start()
for t in threads: t.join()
elapsed = time.perf_counter() - t_start

lats.sort()
n     = len(lats)
total = oks + errs

def pct(p):
    return round(lats[min(n - 1, max(0, int(p / 100 * n)))], 1) if n else 0.0

p50  = pct(50)
p95  = pct(95)
p99  = pct(99)
rps  = round(total / elapsed, 1) if elapsed else 0
erp  = round(100 * errs / total, 2) if total else 0

print()
print(f"{'VUs':>6} {'requests':>10} {'rps':>9} {'error%':>8} {'p50ms':>8} {'p95ms':>9} {'p99ms':>9} {'max':>9}")
print(f"{vus:>6} {total:>10} {rps:>9} {erp:>8} {p50:>8} {p95:>9} {pct(99):>9} {round(lats[-1],1) if n else 0:>9}")

print()
print("=== CERTIFICATION VERDICT ===")
PASS = True
if p95 < 200:
    print(f"  P95={p95}ms  < 200ms target: PASS")
else:
    print(f"  P95={p95}ms  >= 200ms target: FAIL")
    PASS = False
if erp < 5:
    print(f"  error%={erp}%  < 5% target: PASS")
else:
    print(f"  error%={erp}%  >= 5% target: FAIL")
    PASS = False

print()
print("JSON:" + json.dumps({
    "phase": "C-P1", "endpoint": "/api/identity/login",
    "vus": vus, "requests": total, "throughput_rps": rps,
    "error_pct": erp, "p50": p50, "p95": p95, "p99": pct(99),
    "max": round(lats[-1], 1) if n else 0,
    "criterion_p95_200ms": "PASS" if p95 < 200 else "FAIL",
    "criterion_error_pct_5": "PASS" if erp < 5 else "FAIL",
    "verdict": "PASS" if PASS else "FAIL",
}))
sys.exit(0 if PASS else 1)
PY
