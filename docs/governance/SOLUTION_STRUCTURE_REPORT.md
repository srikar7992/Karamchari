# Solution Structure Report
**Generated:** 2026-05-31

## Existing Solutions
| Solution File | Path | Purpose |
|---|---|---|
| `Karamchari.sln` | `src/Backend/Karamchari.sln` | Master solution containing all projects. |

## Target Solution Structure
To improve developer experience and CI performance, the following solution files will be established:

| Solution File | Target Path | Purpose | CI Authority |
|---|---|---|---|
| `Karamchari.sln` | `src/Backend/Karamchari.sln` | **Master Solution**. Includes all Platform, Modules, Hosts, and Tests. | YES (Release) |
| `Karamchari.Backend.sln` | `src/Backend/Karamchari.Backend.sln` | **Backend Work**. Includes Platform, Modules, and Hosts. Excludes Tests. | NO |
| `Karamchari.Tests.sln` | `src/Backend/Karamchari.Tests.sln` | **Test Execution**. Includes all Test projects and their dependencies. | YES (Test) |
| `Karamchari.Tools.sln` | `src/Backend/Karamchari.Tools.sln` | **Tooling**. Includes all projects in `tools/`. | NO |

## Governance Rules
1. `Karamchari.sln` must always include 100% of projects in the repository.
2. New modules must be added to both `Karamchari.sln` and `Karamchari.Backend.sln`.
3. New tests must be added to both `Karamchari.sln` and `Karamchari.Tests.sln`.
