# WS7 — Financial Data Integrity (Decimal Precision)

**Status: ✅ CLOSED (was MEDIUM).**

## Changes implemented
Added a global EF Core convention in the shared base `KaramchariDbContext.ConfigureConventions`:
```csharp
configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
configurationBuilder.Properties<decimal?>().HavePrecision(18, 2);
```
This applies to **all 15+ module DbContexts** (they all inherit `KaramchariDbContext`). Properties that declare their own `HasPrecision`/`HasColumnType` keep their explicit settings (explicit config overrides conventions). `(18,2)` matches the existing SQL Server default column type, so it makes precision **explicit** without changing physical storage or requiring new migrations.

## Verification
- **Startup decimal warnings: 0** (was 25+ `No store type was specified for the decimal property ...`).
- DB decimal precision distribution (dbo template, 175 decimal columns):
  ```
  18,2 : 113   |  5,2 : 28   |  18,4 : 22   |  7,2 : 5   |  9,2 : 2   |  5,4 : 2  |  7,4 : 1  |  4,2 : 1  |  10,2 : 1
  ```
- **Decimal columns with scale 0 (truncation risk): 0.**
- Every decimal column now has an explicit precision/scale (the 18,4 / 5,4 / 7,4 entries are properties with their own `HasPrecision` that the convention correctly did not override).

## Sample (Entity.Property : precision)
```
Billing_BillableEntries.Rate     : decimal(18,2)
Billing_BillableEntries.Amount   : decimal(18,2)
Analytics_ProjectMetrics.Revenue : decimal(18,2)
ArrearCalculations.TotalTdsDelta : decimal(18,2)
```

## Verdict
Financial Data Integrity = **PASS** — no implicit/silent-truncation decimals remain.
