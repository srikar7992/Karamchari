# BUILD CERTIFICATION (Phase 1)

**Date:** 2026-05-30 · **SDK:** 10.0.300 (global.json pins 10.0.203, rollForward latestFeature).

| Step | Result | Evidence |
|---|---|---|
| `dotnet restore` (sln) | ✅ VERIFIED | 40 projects restored, ~4.5s |
| `dotnet build -c Release -warnaserror` | ✅ **VERIFIED** | **0 Warning(s), 0 Error(s)**, 40 projects, ~24s |
| Docker build `karamchari-api` | ✅ VERIFIED | image `3d47a2d7…` built + deployed |
| Docker build `karamchari-worker` | ✅ VERIFIED | image `d90fea0e…` built + deployed |
| Publish validation (`dotnet publish` API) | ⏳ NOT RUN this pass | CI performs `-warnaserror` publish (`ci.yml`); not re-run locally |
| Debug build | ⏳ NOT RUN | Release `-warnaserror` is the stricter gate; Debug not separately run |
| Frontend (`portal`) build | NOT VERIFIED | not run this pass (Node toolchain present; CI `node` job covers it) |
| Mobile build | NOT VERIFIED | not run this pass |

**Key result:** the entire solution — including the previously-uncommitted D1 fix (generic tenant
filters, `ExecutionContextSigner`, dual publish-filter registration) and the new cert/chaos/regression
test projects — **compiles clean under warnings-as-errors.** The fix is real and links.

**Warnings as errors:** enforced (`Directory.Build.props` + `-warnaserror`); build is zero-warning.

**Verdict: Phase 1 Build — VERIFIED** for the backend solution + container images (the deployable
artifacts). Frontend/mobile/publish builds delegated to CI and classified NOT VERIFIED locally this pass.
