# Analyzer & Code Quality Policy

## 1. Zero Warnings Policy
Karamchari treats all compiler, analyzer, and code style warnings as **errors**. This is globally enforced in `Directory.Build.props` via `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

## 2. Enabled Analyzers
By default, `<EnableNETAnalyzers>true</EnableNETAnalyzers>` and `<AnalysisLevel>latest-recommended</AnalysisLevel>` are enabled for the entire repository.

## 3. Suppression Rules
**Silent suppressions are strictly prohibited.**

If an analyzer warning must be bypassed:
1. Do **not** disable the analyzer globally in `.editorconfig` unless discussed in an ADR.
2. Use `#pragma warning disable` ONLY around the specific line of code.
3. You **MUST** include a justification comment above the pragma explaining *why* the suppression is necessary and safe.

Example:
```csharp
// Justification: Legacy API requires passing null here; guaranteed safe by bounded context X.
#pragma warning disable CA1062 // Validate arguments of public methods
SomeLegacyCall(null);
#pragma warning restore CA1062
```

## 4. XML Documentation
Public APIs (especially Core domain primitives and Integration Events) must have XML documentation. `CS1591` is treated as an error. If a DTO property is self-explanatory, provide a brief `<summary>`.
