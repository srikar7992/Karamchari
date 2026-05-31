# CLEAN MACHINE CERTIFICATION (Phase 2)

**Date:** 2026-05-30 · **Status: PARTIAL — not re-run from zero this pass (by design).**

## What is VERIFIED
- One-command bootstrap scripts present: `setup-local.sh` / `setup-local.ps1`, `run-all-tests.sh` / `.ps1`.
- The currently-running system was **built from repository source and deployed via `docker compose`**
  (API `3d47a2d7…`, Worker `d90fea0e…`) — i.e. the repo *does* produce an operational system, proven by
  every runtime test in this program (D1, security, chaos all ran against source-built containers).
- Prior greenfield re-cert (`docker compose down -v` → bootstrap) passed in an earlier audit.

## What is NOT VERIFIED (honest)
- A **fresh `down -v` destroy + bootstrap-from-zero** was **deliberately NOT executed this pass** to preserve
  the D1-verified runtime environment for the rest of the certification. Per the program's "never inherit"
  rule, the *prior* greenfield pass is **not** counted as current evidence.
- Therefore: **time-to-operational** and **manual-intervention count** on a truly clean machine are NOT
  re-measured here.

## Recommendation
Re-run `docker compose down -v && ./setup-local.sh` once (now that the D1 fix is committed), timing it and
recording any manual steps. This is low-risk (the fix is in git) and would upgrade Phase 2 to VERIFIED.

## Verdict: **Phase 2 — PARTIAL / NOT RE-VERIFIED this pass.**
