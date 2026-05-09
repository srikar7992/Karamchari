## Validation

- [ ] `dotnet restore src/Backend/Karamchari.sln --locked-mode`
- [ ] `dotnet format src/Backend/Karamchari.sln --verify-no-changes --severity warn --no-restore`
- [ ] `dotnet build src/Backend/Karamchari.sln --no-restore -c Release -warnaserror`
- [ ] `dotnet test src/Backend/Karamchari.sln --no-build -c Release --collect:"XPlat Code Coverage"`
- [ ] `dotnet list src/Backend/Karamchari.sln package --vulnerable --include-transitive`
- [ ] `npm audit --prefix src/Frontend/portal --audit-level=moderate`
- [ ] `npm audit --prefix src/Mobile/karamchari-mobile --audit-level=moderate`

## Governance

- [ ] No direct commits to `main` or `master`
- [ ] No dependency version changes outside `Directory.Packages.props` or package lock files
- [ ] No secrets, credentials, tokens, or environment-specific config committed
- [ ] Public contract changes include XML documentation and tests
- [ ] Architecture boundaries remain intact
