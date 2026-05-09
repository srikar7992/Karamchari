# Centralized Package Governance

## 1. Central Package Management (CPM)
Karamchari uses NuGet Central Package Management. All package versions are defined exclusively in `Directory.Packages.props`.

## 2. Rules for Engineers
- **Do not** specify a `Version` attribute inside a `<PackageReference>` in your `.csproj` file.
- **Do not** introduce a new third-party dependency without architectural review.
- If you need to upgrade a package, upgrade it in `Directory.Packages.props`. This ensures the entire solution uses the exact same version, preventing DLL hell and runtime binding redirects.

## 3. Transitive Pinning & Security
`<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` is enabled.
If the CI security audit (`dotnet list package --vulnerable --include-transitive`) detects a vulnerability in a downstream package, you must explicitly pin the patched version in `Directory.Packages.props` under the "Security patches" item group.

## 4. NuGet Audit
We rely on NuGet Audit (`<NuGetAudit>true</NuGetAudit>`). Exceptions to advisories (e.g., if we are not utilizing the vulnerable API path and no patch exists) must be explicitly recorded in `<NuGetAuditSuppress>` within `Directory.Build.props` with a comment detailing the review date and rationale.
