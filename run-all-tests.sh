#!/usr/bin/env bash
# =============================================================================
# Karamchari — One-Command Test Execution (Unix/macOS)
# =============================================================================
# Runs the full backend test suite (unit, integration, architecture, tenant
# isolation, security, financial/chaos) and emits TRX + coverage + summary.
#
#   ./run-all-tests.sh                 run everything
#   ./run-all-tests.sh --no-integration  skip suites needing live infra
#
# Exit code mirrors the pipeline: 0 = all passed, non-zero = at least one failed.
# Artifacts: artifacts/test-results/*.trx, **/coverage.cobertura.xml,
#            docs/audits/hostile/test-summary.txt
# =============================================================================
# NOTE: intentionally NOT using `set -u`. macOS ships bash 3.2, which raises an
# "unbound variable" error when expanding an *empty* array (e.g. "${cov_args[@]}"
# for the no-coverage architecture-test case). `set -o pipefail` is sufficient.
set -o pipefail
REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_ROOT"
SOLUTION="src/Backend/Karamchari.sln"
RESULTS="artifacts/test-results"
SUMMARY="docs/audits/hostile/test-summary.txt"
mkdir -p "$RESULTS" docs/audits/hostile
: > "$SUMMARY"

SKIP_INTEGRATION=0
for a in "$@"; do case "$a" in --no-integration) SKIP_INTEGRATION=1 ;; esac; done

# Test projects classified. Integration/cert suites require Docker infra up.
UNIT=(
  # Platform
  "tests/Backend/Karamchari.Core.UnitTests/Karamchari.Core.UnitTests.csproj"
  "tests/Backend/Karamchari.Api.UnitTests/Karamchari.Api.UnitTests.csproj"
  "tests/Backend/Karamchari.ArchitectureTests/Karamchari.ArchitectureTests.csproj"
  "tests/Backend/Karamchari.Identity.Tests/Karamchari.Identity.Tests.csproj"
  # Domain modules — Sprint 1
  "tests/Backend/Karamchari.Recruitment.Tests/Karamchari.Recruitment.Tests.csproj"
  "tests/Backend/Karamchari.HR.Tests/Karamchari.HR.Tests.csproj"
  "tests/Backend/Karamchari.DataMigration.Tests/Karamchari.DataMigration.Tests.csproj"
  # Domain modules — Sprint 2
  "tests/Backend/Karamchari.Billing.Tests/Karamchari.Billing.Tests.csproj"
  "tests/Backend/Karamchari.Compensation.Tests/Karamchari.Compensation.Tests.csproj"
  "tests/Backend/Karamchari.FinancialOps.Tests/Karamchari.FinancialOps.Tests.csproj"
  "tests/Backend/Karamchari.Notifications.Tests/Karamchari.Notifications.Tests.csproj"
  "tests/Backend/Karamchari.Governance.Tests/Karamchari.Governance.Tests.csproj"
  "tests/Backend/Karamchari.Workflow.Tests/Karamchari.Workflow.Tests.csproj"
  "tests/Backend/Karamchari.Intelligence.Tests/Karamchari.Intelligence.Tests.csproj"
  "tests/Backend/Karamchari.Capability.Tests/Karamchari.Capability.Tests.csproj"
  "tests/Backend/Karamchari.Forecasting.Tests/Karamchari.Forecasting.Tests.csproj"
  "tests/Backend/Karamchari.Outbox.Tests/Karamchari.Outbox.Tests.csproj"
  # Existing payroll / PSA / TA / chaos
  "tests/Backend/Karamchari.Payroll.Tests/Karamchari.Payroll.Tests.csproj"
  "tests/Backend/Karamchari.PSA.Tests/Karamchari.PSA.Tests.csproj"
  "tests/Backend/Karamchari.TimeAttendance.Tests/Karamchari.TimeAttendance.Tests.csproj"
  "tests/Backend/Karamchari.FinancialChaosTests/Karamchari.FinancialChaosTests.csproj"
)
INTEGRATION=(
  "tests/Backend/Karamchari.Core.IntegrationTests/Karamchari.Core.IntegrationTests.csproj"
  "tests/Backend/Karamchari.Identity.IntegrationTests/Karamchari.Identity.IntegrationTests.csproj"
  "tests/Backend/Karamchari.TenantIsolationCertification/Karamchari.TenantIsolationCertification.csproj"
)

PASS=0; FAILED=0; FAILED_LIST=""
run_proj() {
  local proj="$1" name rc; name="$(basename "$proj" .csproj)"
  # NetArchTest reflects over every type in the target assemblies and throws
  # TypeLoadException on the Coverlet 'XPlat Code Coverage' instrumentation
  # tracker types. Architecture tests therefore MUST run without coverage.
  # Use an array so the space-bearing '"XPlat Code Coverage"' stays one token.
  local cov_args=()
  case "$name" in
    *ArchitectureTests*) cov_args=() ;;
    *) cov_args=(--collect:"XPlat Code Coverage") ;;
  esac
  printf '\n==> %s\n' "$name" | tee -a "$SUMMARY"
  # Trust the dotnet test exit code (authoritative). Do NOT pipe to grep -q:
  # under `set -o pipefail`, grep closing the pipe early sends SIGPIPE to
  # dotnet test and corrupts the pipeline status into a false failure.
  dotnet test "$proj" -c Debug \
    --logger "trx;LogFileName=${name}.trx" \
    --results-directory "$RESULTS" \
    "${cov_args[@]}" >>"$SUMMARY" 2>&1
  rc=$?
  if [ "$rc" -eq 0 ]; then
    PASS=$((PASS+1)); echo "[PASS] $name" | tee -a "$SUMMARY"
  else
    FAILED=$((FAILED+1)); FAILED_LIST="$FAILED_LIST $name"; echo "[FAIL] $name (exit $rc)" | tee -a "$SUMMARY"
  fi
}

echo "Restoring & building once..." | tee -a "$SUMMARY"
dotnet build "$SOLUTION" -c Debug 2>&1 | tail -3 | tee -a "$SUMMARY"

for p in "${UNIT[@]}"; do run_proj "$p"; done
if [ "$SKIP_INTEGRATION" -eq 0 ]; then
  for p in "${INTEGRATION[@]}"; do run_proj "$p"; done
else
  echo "[SKIP] integration/cert suites (--no-integration)" | tee -a "$SUMMARY"
fi

{
  echo ""; echo "================ SUMMARY ================"
  echo "Projects passed: $PASS"
  echo "Projects failed: $FAILED"
  [ -n "$FAILED_LIST" ] && echo "Failed:$FAILED_LIST"
  echo "TRX + coverage under: $RESULTS"
} | tee -a "$SUMMARY"

[ "$FAILED" -eq 0 ] && exit 0 || exit 1
