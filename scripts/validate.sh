#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPO_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
SOLUTION="$REPO_ROOT/src/Backend/Karamchari.sln"

cd "$REPO_ROOT"

run() {
  printf '\n==> %s\n' "$1"
  shift
  "$@"
}

run "Restore locked NuGet graph" dotnet restore "$SOLUTION" --locked-mode
run "Verify formatting" dotnet format "$SOLUTION" --verify-no-changes --severity warn --no-restore
run "Build with analyzers and warnings as errors" dotnet build "$SOLUTION" --no-restore -c Release -warnaserror
run "Run backend unit and integration tests with coverage" dotnet test "$SOLUTION" --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
run "NuGet vulnerability audit" dotnet list "$SOLUTION" package --vulnerable --include-transitive

if [ -f "$REPO_ROOT/src/Frontend/portal/package-lock.json" ]; then
  run "Portal deterministic install" npm ci --prefix "$REPO_ROOT/src/Frontend/portal" --ignore-scripts --loglevel=error
  run "Portal vulnerability audit" npm audit --prefix "$REPO_ROOT/src/Frontend/portal" --audit-level=moderate
  run "Portal typecheck" npm run --prefix "$REPO_ROOT/src/Frontend/portal" typecheck
  run "Portal production build" npm run --prefix "$REPO_ROOT/src/Frontend/portal" build
fi

if [ -f "$REPO_ROOT/src/Mobile/karamchari-mobile/package-lock.json" ]; then
  run "Mobile deterministic install" npm ci --prefix "$REPO_ROOT/src/Mobile/karamchari-mobile" --ignore-scripts --loglevel=error
  run "Mobile vulnerability audit" npm audit --prefix "$REPO_ROOT/src/Mobile/karamchari-mobile" --audit-level=moderate
fi

run "Publish validation" dotnet publish "$REPO_ROOT/src/Backend/Karamchari.Api/Karamchari.Api.csproj" --no-restore -c Release -o "$REPO_ROOT/artifacts/publish/Karamchari.Api" -warnaserror

printf '\nValidation completed successfully.\n'
