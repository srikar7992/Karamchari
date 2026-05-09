# Local Setup & Environment Validation Guide

## 1. One-Command Setup
To bootstrap your local development environment, run the setup script from the repository root:

**Windows (PowerShell):**
```powershell
.\setup.ps1
```

**macOS/Linux:**
```bash
git config core.hooksPath .githooks
dotnet restore src/Backend/Karamchari.sln
dotnet build src/Backend/Karamchari.sln
```

## 2. What Does Setup Do?
- **Hooks:** Points `git` to the `.githooks` directory to enforce pre-commit checks (Formatting, Build, Analyzer, Secret scanning).
- **Restore:** Restores all centralized NuGet packages defined in `Directory.Packages.props`.
- **Build:** Executes a strict build applying the Zero Warnings Policy.

## 3. Local Developer Experience Rules
- **No Global Tools:** Do not rely on globally installed dotnet tools if they mutate code formatting. Use the local `dotnet format` command.
- **Secrets:** Never check in `appsettings.Development.json` if it contains real secrets. Use `dotnet user-secrets` for local connections to databases or service buses.
- **Reproducibility:** If your build works locally but fails in CI, ensure your local SDK matches `global.json` (or the CI pipeline version, currently `10.0.x`).
