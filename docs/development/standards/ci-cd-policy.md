# CI/CD Policy

## 1. Pipeline Definition
The primary CI pipeline is defined in `.github/workflows/ci.yml`. It runs automatically on all Pull Requests and pushes to `main`.

## 2. Fail-Fast Principles
The pipeline is designed to fail early to save compute resources and provide fast feedback to engineers:
1. **Restore:** Fails if `Directory.Packages.props` is malformed or locked versions drift.
2. **Formatting:** Fails if code deviates from `.editorconfig` (`dotnet format --verify-no-changes`).
3. **Build:** Fails if there are ANY warnings or compilation errors (`-warnaserror`).
4. **Tests:** Executes isolated unit and integration tests.
5. **Security:** Fails if known vulnerable transitive dependencies exist.

## 3. Deterministic Builds
The CI pipeline relies on `ContinuousIntegrationBuild=true` in `Directory.Build.props`, ensuring deterministic output generation (e.g., path mapping, deterministic GUIDs during build).

## 4. Artifacts and Deployment
Artifacts are generated in the centralized `artifacts/bin/` and `artifacts/obj/` directories, preventing pollution of the `src/` tree and enabling easy caching and artifact uploading.
