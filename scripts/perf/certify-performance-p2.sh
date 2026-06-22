#!/usr/bin/env bash
# Phase C — Performance Certification P2: Full recruitment journey, concurrent.
#
# Single journey:
#   POST /api/v1/recruitment/requisitions
#   -> POST /api/v1/recruitment/candidates
#   -> POST /api/v1/recruitment/applications
#   -> POST /api/v1/recruitment/interviews
#   -> POST /api/v1/recruitment/offers
#   -> POST /api/v1/recruitment/offers/{id}/approve
#   -> POST /api/v1/recruitment/offers/{id}/issue
#   -> POST /api/v1/recruitment/applications/{id}/hire
#
# Runs JOURNEYS (default 10) concurrent journeys and measures per-journey
# wall-clock time. Uses admin@dev.local (all recruitment.* permissions).
#
# Usage:
#   bash scripts/perf/certify-performance-p2.sh
#   bash scripts/perf/certify-performance-p2.sh --base http://localhost:8080 --journeys 10
#
# Target: median journey < 5s, 0% journey failures.

set -euo pipefail
BASE="http://localhost:8080"
JOURNEYS=10

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base) BASE="$2"; shift 2;;
    --journeys) JOURNEYS="$2"; shift 2;;
    *) echo "Unknown arg: $1"; exit 1;;
  esac
done

echo "# Phase C — P2: Full recruitment journey concurrent test"
echo "# base=$BASE  concurrent_journeys=$JOURNEYS"
echo "# Criterion: median journey < 5s, 0 failed journeys"
echo ""

# Obtain admin token (admin role has all permissions including all recruitment.*)
login() {
  local email=$1 pw=$2 tenant=$3
  curl -s -X POST "$BASE/api/identity/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$pw\",\"tenantId\":\"$tenant\"}" \
    | python3 -c "import sys,json;print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null
}

echo "Obtaining admin token for dev tenant..."
ADMIN_TOK=$(login "admin@dev.local" "Dev@Pass123!" "dev")
if [ -z "$ADMIN_TOK" ]; then
  echo "ERROR: Could not obtain admin token. Is the API running at $BASE?"
  exit 1
fi
echo "  Token obtained (${#ADMIN_TOK} chars)"

# Also need a department and hiring manager to create requisitions.
# We use the first employee from the seeded data, or use a placeholder GUID.
# The endpoint accepts any Guid for DepartmentId and HiringManagerId (FK not enforced at create).
DEPT_ID="00000000-0000-0000-0000-000000000001"
MANAGER_ID="00000000-0000-0000-0000-000000000002"
INTERVIEWER_ID="00000000-0000-0000-0000-000000000003"

python3 - "$BASE" "$ADMIN_TOK" "$JOURNEYS" "$DEPT_ID" "$MANAGER_ID" "$INTERVIEWER_ID" <<'PY'
import sys, json, threading, time, urllib.request, urllib.error, uuid, secrets

base          = sys.argv[1]
token         = sys.argv[2]
n_journeys    = int(sys.argv[3])
dept_id       = sys.argv[4]
manager_id    = sys.argv[5]
interviewer_id = sys.argv[6]

headers = {
    "Authorization": "Bearer " + token,
    "Content-Type":  "application/json",
}

def post(path, body):
    """POST JSON body, return (status_code, parsed_json_or_None)."""
    data = json.dumps(body).encode()
    req  = urllib.request.Request(base + path, data=data, headers=headers)
    try:
        resp = urllib.request.urlopen(req, timeout=30)
        raw  = resp.read()
        return resp.status, json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        raw = e.read()
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, {}
    except Exception as ex:
        return 0, {"error": str(ex)}

def do_post_noreturn(path):
    """POST empty-ish body for state transitions (approve, publish, advance, hire)."""
    req = urllib.request.Request(base + path, data=b'{}', headers=headers, method="POST")
    try:
        resp = urllib.request.urlopen(req, timeout=30)
        resp.read()
        return resp.status
    except urllib.error.HTTPError as e:
        e.read()
        return e.code
    except Exception:
        return 0

def run_journey(idx):
    uid   = secrets.token_hex(4)
    steps = []
    errors = []

    def step(name, fn):
        t0 = time.perf_counter()
        result = fn()
        ms = (time.perf_counter() - t0) * 1000
        steps.append({"step": name, "ms": round(ms, 1), "result": result})
        return result

    total_start = time.perf_counter()

    # 1. Create requisition
    def create_req():
        code, body = post("/api/v1/recruitment/requisitions", {
            "title": f"Perf-P2 Role {uid} Journey {idx}",
            "departmentId": dept_id,
            "hiringManagerId": manager_id,
        })
        if code not in (200, 201):
            errors.append(f"requisition create {code}")
            return None
        return body.get("id")

    req_id = step("create_requisition", create_req)
    if req_id is None:
        return {"journey": idx, "failed": True, "errors": errors, "steps": steps, "total_ms": 0}

    # 2. Create candidate
    def create_cand():
        code, body = post("/api/v1/recruitment/candidates", {
            "firstName": f"Perf{idx}",
            "lastName":  f"Journey{uid}",
            "email":     f"p2_{idx}_{uid}@example.com",
            "phoneNumber": None,
        })
        if code not in (200, 201):
            errors.append(f"candidate create {code}")
            return None
        return body.get("id")

    cand_id = step("create_candidate", create_cand)
    if cand_id is None:
        return {"journey": idx, "failed": True, "errors": errors, "steps": steps, "total_ms": 0}

    # 3. Apply candidate to requisition
    def apply():
        code, body = post("/api/v1/recruitment/applications", {
            "candidateId":  cand_id,
            "requisitionId": req_id,
        })
        if code not in (200, 201):
            errors.append(f"application create {code}")
            return None
        return body.get("id")

    app_id = step("create_application", apply)
    if app_id is None:
        return {"journey": idx, "failed": True, "errors": errors, "steps": steps, "total_ms": 0}

    # 4. Schedule interview
    from datetime import datetime, timezone, timedelta
    future = (datetime.now(timezone.utc) + timedelta(days=7)).isoformat()

    def schedule_iv():
        code, body = post("/api/v1/recruitment/interviews", {
            "applicationId": app_id,
            "scheduledAt":   future,
            "durationMinutes": 60,
            "interviewerIds": [interviewer_id],
        })
        if code not in (200, 201):
            errors.append(f"interview schedule {code}")
            return None
        return body.get("id")

    iv_id = step("schedule_interview", schedule_iv)
    # Interview scheduling failure is non-fatal for offer/hire flow (some configs may not require it)

    # 5. Create offer
    def create_offer():
        code, body = post("/api/v1/recruitment/offers", {
            "applicationId": app_id,
            "baseSalary":    95000.00,
            "currency":      "USD",
        })
        if code not in (200, 201):
            errors.append(f"offer create {code}")
            return None
        return body.get("id")

    offer_id = step("create_offer", create_offer)
    if offer_id is None:
        return {"journey": idx, "failed": True, "errors": errors, "steps": steps, "total_ms": 0}

    # 6. Approve offer
    def approve_offer():
        return do_post_noreturn(f"/api/v1/recruitment/offers/{offer_id}/approve")

    approve_code = step("approve_offer", approve_offer)
    if approve_code not in (200, 204):
        errors.append(f"offer approve {approve_code}")

    # 7. Issue offer (requires ExpiresAt)
    expires_at = (datetime.now(timezone.utc) + timedelta(days=14)).isoformat()

    def issue_offer():
        code, _ = post(f"/api/v1/recruitment/offers/{offer_id}/issue", {"expiresAt": expires_at})
        return code

    issue_code = step("issue_offer", issue_offer)
    if issue_code not in (200, 204):
        errors.append(f"offer issue {issue_code}")

    # 8. Hire candidate
    def hire():
        return do_post_noreturn(f"/api/v1/recruitment/applications/{app_id}/hire")

    hire_code = step("hire_candidate", hire)
    if hire_code not in (200, 204):
        errors.append(f"hire {hire_code}")

    total_ms = (time.perf_counter() - total_start) * 1000
    failed   = len(errors) > 0
    return {"journey": idx, "failed": failed, "errors": errors, "steps": steps,
            "total_ms": round(total_ms, 1)}

print(f"Running {n_journeys} concurrent journeys...")
results  = [None] * n_journeys
threads  = []
t_wall   = time.perf_counter()

def run(i):
    results[i] = run_journey(i)

for i in range(n_journeys):
    t = threading.Thread(target=run, args=(i,))
    threads.append(t)

for t in threads: t.start()
for t in threads: t.join()
wall_s = time.perf_counter() - t_wall

# Report
print()
print(f"{'Journey':>8} {'total_ms':>10} {'steps':>7} {'errors'}")
print("-" * 60)
journey_ms = []
failures   = 0
for r in results:
    if r is None:
        continue
    flag = "FAIL" if r["failed"] else "ok"
    print(f"{r['journey']:>8} {r['total_ms']:>10} {len(r['steps']):>7}   {flag}  {'; '.join(r['errors'])}")
    journey_ms.append(r["total_ms"])
    if r["failed"]:
        failures += 1

journey_ms.sort()
n = len(journey_ms)
med   = journey_ms[n // 2]        if n else 0
p95   = journey_ms[min(n-1, int(0.95*n))] if n else 0
p99   = journey_ms[min(n-1, int(0.99*n))] if n else 0

print()
print(f"Wall time: {wall_s:.1f}s  |  Journeys: {n}  |  Failures: {failures}")
print(f"Per-journey — median: {med:.0f}ms  p95: {p95:.0f}ms  p99: {p99:.0f}ms  max: {journey_ms[-1] if n else 0:.0f}ms")

print()
print("=== CERTIFICATION VERDICT ===")
PASS = True
if med < 5000:
    print(f"  Median journey {med:.0f}ms < 5000ms: PASS")
else:
    print(f"  Median journey {med:.0f}ms >= 5000ms: FAIL")
    PASS = False
if failures == 0:
    print(f"  Journey failures: 0 of {n}: PASS")
else:
    print(f"  Journey failures: {failures} of {n}: FAIL")
    PASS = False

print()
print("JSON:" + json.dumps({
    "phase": "C-P2", "concurrent_journeys": n_journeys,
    "failures": failures, "median_ms": med, "p95_ms": p95, "p99_ms": p99,
    "max_ms": journey_ms[-1] if n else 0,
    "criterion_median_5s": "PASS" if med < 5000 else "FAIL",
    "criterion_zero_failures": "PASS" if failures == 0 else "FAIL",
    "verdict": "PASS" if PASS else "FAIL",
}))
import sys
sys.exit(0 if PASS else 1)
PY
