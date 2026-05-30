#!/usr/bin/env python3
"""
Karamchari load ramp harness (no external tools required).

Closed-loop concurrency model: N "virtual users" each issue requests back-to-back
for a fixed duration. Records p50/p90/p95/p99, throughput (req/s) and error rate.
Not a substitute for k6/NBomber on a production-grade host, but real HTTP under real
concurrency against the running deployment. Designed to find the breaking point.

Usage:
  python3 scripts/perf/load-ramp.py --base http://localhost:8080 --duration 6 \
      --levels 10,50,100,250,500,1000,2000
"""
import argparse, json, statistics, sys, threading, time, urllib.request, urllib.error

def login(base):
    import secrets
    email = f"load_{int(time.time())}_{secrets.token_hex(3)}@example.com"
    body = json.dumps({"email": email, "password": "Load#Pass123!", "tenantId": "dev"}).encode()
    for path in ("/api/identity/register", "/api/identity/login"):
        req = urllib.request.Request(base + path, data=body, headers={"Content-Type": "application/json"})
        try:
            r = urllib.request.urlopen(req, timeout=15); data = r.read()
        except urllib.error.HTTPError as e:
            data = e.read()
        if path.endswith("login"):
            try:
                j = json.loads(data)
                return j.get("accessToken") or j.get("access_token")
            except Exception:
                return None
    return None

def run_level(base, path, vus, duration, headers):
    stop = time.time() + duration
    lats = []; errs = 0; oks = 0
    lock = threading.Lock()
    def worker():
        nonlocal errs, oks
        local = []
        while time.time() < stop:
            s = time.perf_counter()
            try:
                req = urllib.request.Request(base + path, headers=headers or {})
                r = urllib.request.urlopen(req, timeout=30); r.read()
                code = r.status
            except urllib.error.HTTPError as e:
                code = e.code
            except Exception:
                code = 0
            dt = (time.perf_counter() - s) * 1000
            local.append((dt, code))
        with lock:
            for dt, code in local:
                lats.append(dt)
                if code == 200: oks += 1
                else: errs += 1
    threads = [threading.Thread(target=worker) for _ in range(vus)]
    t0 = time.perf_counter()
    for t in threads: t.start()
    for t in threads: t.join()
    elapsed = time.perf_counter() - t0
    lats.sort(); n = len(lats)
    pct = lambda p: lats[min(n-1, int(p/100*n))] if n else 0
    total = oks + errs
    return {
        "vus": vus, "requests": total, "throughput_rps": round(total/elapsed, 1) if elapsed else 0,
        "error_pct": round(100*errs/total, 2) if total else 0,
        "p50": round(pct(50),1), "p90": round(pct(90),1), "p95": round(pct(95),1),
        "p99": round(pct(99),1), "max": round(lats[-1],1) if n else 0,
    }

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:8080")
    ap.add_argument("--duration", type=float, default=6.0)
    ap.add_argument("--levels", default="10,50,100,250,500,1000,2000")
    ap.add_argument("--path", default="/api/v1/hr/employees", help="endpoint under load")
    ap.add_argument("--anon", action="store_true", help="do not authenticate")
    args = ap.parse_args()

    headers = {}
    if not args.anon:
        tok = login(args.base)
        if not tok:
            print("WARN: login failed; falling back to anon", file=sys.stderr)
        else:
            headers = {"Authorization": "Bearer " + tok}

    levels = [int(x) for x in args.levels.split(",") if x.strip()]
    print(f"# target={args.base}{args.path} duration={args.duration}s/level authed={bool(headers)}")
    print(f"{'VUs':>6} {'req':>8} {'rps':>9} {'err%':>6} {'p50':>8} {'p90':>8} {'p95':>9} {'p99':>9} {'max':>9}")
    results = []
    for vus in levels:
        r = run_level(args.base, args.path, vus, args.duration, headers)
        results.append(r)
        print(f"{r['vus']:>6} {r['requests']:>8} {r['throughput_rps']:>9} {r['error_pct']:>6} "
              f"{r['p50']:>8} {r['p90']:>8} {r['p95']:>9} {r['p99']:>9} {r['max']:>9}")
        time.sleep(1)
    print("\nJSON:" + json.dumps(results))

if __name__ == "__main__":
    main()
