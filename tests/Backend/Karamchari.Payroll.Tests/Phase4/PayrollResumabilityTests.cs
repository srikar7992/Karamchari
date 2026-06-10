// -----------------------------------------------------------------------
// <copyright file="PayrollResumabilityTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Deductions;
using Xunit.Abstractions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// Validates the scatter-gather resumability guarantee:
/// "restart after partial completion produces no duplicates,
///  no missed employees, and identical results."
///
/// Simulates CalculateAllEmployeePayCommandConsumer's core algorithm in-memory:
///   1. Build employee list
///   2. Calculate first N (simulating partial run before "crash")
///   3. Restart: exclude already-done IDs (same logic as alreadyDone HashSet)
///   4. Process remainder
///   5. Assert totals + idempotency + determinism
/// </summary>
public class PayrollResumabilityTests(ITestOutputHelper output)
{
    private static readonly TaxCalculatorService TaxSvc = new();

    private static PayrollCalculationEngine Engine() => new([
        new ProvidentFundCalculator(),
        new EsiCalculator(),
        new ProfessionalTaxCalculator(),
    ]);

    private static StatutoryConfigSnapshot Stat() =>
        new(15_000m, 21_000m, 0.0075m, 0.0325m, 0.12m, 0.0367m, 1_250m);

    private sealed record EmployeeRecord(Guid EmployeeId, EmployeePayrollInput Input);

    private static EmployeeRecord BuildRecord(int index)
    {
        var id = Guid.NewGuid();
        var salary = 25_000m + (index % 100) * 1_000m; // 25k–124k
        var hourlyRate = Math.Round(salary / 208m, 2);
        var state = new[] { "KA", "MH", "AP", "TN", "WB" }[index % 5];

        var att = Enumerable.Range(0, 26)
            .Select(i => new DayAttendanceRecord(
                new DateOnly(2025, 4, 1).AddDays(i),
                8m + (i % 3 == 0 ? 2m : 0m), // some OT days
                true, false, false, null, null))
            .ToList();

        var input = new EmployeePayrollInput(
            id, $"Emp_{index}", salary, hourlyRate, "INR", 40m, 0m,
            att,
            Array.Empty<HolidayRecord>(),
            new OvertimePolicySnapshot(
                [new OvertimeRuleSnapshot(0m, null, 1.5m, "Regular", 1)]),
            Array.Empty<ShiftPremiumSnapshot>(),
            Array.Empty<PendingAdjustmentRecord>(),
            false, false, state, "New",
            0m, 0m, 0m, 4, 2025,
            Guid.NewGuid(), "FY2024-25-V1", Stat());

        return new EmployeeRecord(id, input);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core resumability: no duplicates, no omissions, same results
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Resumability_AfterPartialRun_NoDuplicates_NoMissed()
    {
        const int total = 1_000;
        const int firstBatch = 400; // "crash" after 400

        var runId = Guid.NewGuid();
        var employees = Enumerable.Range(0, total).Select(BuildRecord).ToList();
        var engine = Engine();

        // ── Run 1: process first 400, then "crash" ──────────────────────────
        var completedResults = new Dictionary<Guid, decimal>(); // simulates DB
        foreach (var e in employees.Take(firstBatch))
        {
            var r = engine.Calculate("t1", runId, "Apr 2025", 1, "snap", e.Input, TaxSvc);
            completedResults[e.EmployeeId] = r.NetPay;
        }
        Assert.Equal(firstBatch, completedResults.Count);

        // ── Restart: CalculateAllEmployeePayCommandConsumer resume algorithm ─
        var alreadyDone = completedResults.Keys.ToHashSet(); // mirrors DB query
        var pending = employees.Where(e => !alreadyDone.Contains(e.EmployeeId)).ToList();

        Assert.Equal(total - firstBatch, pending.Count); // exactly remaining
        Assert.Empty(pending.Select(e => e.EmployeeId).Intersect(alreadyDone)); // no overlap

        // ── Run 2: process remaining 600 ────────────────────────────────────
        var restartResults = new Dictionary<Guid, decimal>();
        foreach (var e in pending)
        {
            var r = engine.Calculate("t1", runId, "Apr 2025", 1, "snap", e.Input, TaxSvc);
            restartResults[e.EmployeeId] = r.NetPay;
        }

        // ── Merge and verify ─────────────────────────────────────────────────
        var allResults = completedResults
            .Concat(restartResults)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal(total, allResults.Count); // all 1000 exactly once
        Assert.All(employees, e => Assert.True(allResults.ContainsKey(e.EmployeeId)));

        output.WriteLine($"Run 1: {firstBatch} employees");
        output.WriteLine($"Restart: {restartResults.Count} remaining employees");
        output.WriteLine($"Total: {allResults.Count} — no duplicates, no omissions");
    }

    [Fact]
    public void Resumability_EngineIsDeterministic_SameInputSameOutput()
    {
        // Idempotency: re-calculating an already-done employee gives identical net pay.
        // This is what makes it safe to use alreadyDone as a skip-guard.
        var engine = Engine();
        var records = Enumerable.Range(0, 50).Select(BuildRecord).ToList();
        var runId = Guid.NewGuid();

        var first = records.Select(e =>
            engine.Calculate("t1", runId, "Apr 2025", 1, "snap", e.Input, TaxSvc).NetPay).ToList();

        var second = records.Select(e =>
            engine.Calculate("t1", runId, "Apr 2025", 1, "snap", e.Input, TaxSvc).NetPay).ToList();

        Assert.Equal(first, second); // bitwise identical
    }

    [Fact]
    public void Resumability_RunIdIsolation_SameEmployeeDifferentRun_IndependentResults()
    {
        // Two runs for same employee must produce independent results (different RunIds).
        // Proves RunId is correctly propagated and results don't bleed across runs.
        var engine = Engine();
        var record = BuildRecord(99);
        var runA = Guid.NewGuid();
        var runB = Guid.NewGuid();

        var rA = engine.Calculate("t1", runA, "Apr 2025", 1, "snap", record.Input, TaxSvc);
        var rB = engine.Calculate("t1", runB, "Apr 2025", 1, "snap", record.Input, TaxSvc);

        // Same input → same net pay regardless of RunId
        Assert.Equal(rA.NetPay, rB.NetPay);
        // But different RunIds
        Assert.NotEqual(rA.PayrollRunId, rB.PayrollRunId);
    }

    [Fact]
    public void Resumability_ZeroAlreadyDone_ProcessesAll()
    {
        // Fresh start: alreadyDone is empty → all employees in pending
        const int count = 200;
        var employees = Enumerable.Range(0, count).Select(BuildRecord).ToList();
        var alreadyDone = new HashSet<Guid>(); // empty DB

        var pending = employees.Where(e => !alreadyDone.Contains(e.EmployeeId)).ToList();
        Assert.Equal(count, pending.Count);
    }

    [Fact]
    public void Resumability_AllAlreadyDone_ProcessesNone()
    {
        // Complete resume: all employees already in DB → scatter sends zero batches
        const int count = 200;
        var employees = Enumerable.Range(0, count).Select(BuildRecord).ToList();
        var alreadyDone = employees.Select(e => e.EmployeeId).ToHashSet(); // all done

        var pending = employees.Where(e => !alreadyDone.Contains(e.EmployeeId)).ToList();
        Assert.Empty(pending); // nothing to re-process
    }

    [Fact]
    public void Resumability_BatchChunking_100PerBatch_NoBatchOverlap()
    {
        // Mirrors Chunk(100) logic in CalculateAllEmployeePayCommandConsumer.
        // Verifies no employee appears in two batches and all are covered.
        const int count = 750; // non-round number to test remainder batch
        var pending = Enumerable.Range(0, count).Select(i => Guid.NewGuid()).ToList();

        var batches = pending.Chunk(100).ToList();

        Assert.Equal(8, batches.Count); // ceil(750/100) = 8 batches
        Assert.Equal(count, batches.Sum(b => b.Length));

        var allInBatches = batches.SelectMany(b => b).ToHashSet();
        Assert.Equal(count, allInBatches.Count); // no duplicates across batches
        Assert.All(pending, id => Assert.Contains(id, allInBatches));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scale resumability: 10k employees, crash at 4k, restart
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Resumability_10k_CrashAt4k_RestartCompletes_NoDuplicates()
    {
        const int total = 10_000;
        const int crashAt = 4_000;

        var runId = Guid.NewGuid();
        var engine = Engine();
        var employees = Enumerable.Range(0, total).Select(BuildRecord).ToList();

        var sw = Stopwatch.StartNew();

        // Run 1: 4000 employees
        var phase1 = new Dictionary<Guid, decimal>(crashAt);
        foreach (var e in employees.Take(crashAt))
        {
            var r = engine.Calculate("t1", runId, "Apr 2025", 1, "snap", e.Input, TaxSvc);
            phase1[e.EmployeeId] = r.NetPay;
        }

        // Restart: identify remaining
        var alreadyDone = phase1.Keys.ToHashSet();
        var phase2Employees = employees.Where(e => !alreadyDone.Contains(e.EmployeeId)).ToList();

        // Run 2: remaining 6000
        var phase2 = new Dictionary<Guid, decimal>(total - crashAt);
        foreach (var e in phase2Employees)
        {
            var r = engine.Calculate("t1", runId, "Apr 2025", 1, "snap", e.Input, TaxSvc);
            phase2[e.EmployeeId] = r.NetPay;
        }

        sw.Stop();

        var allResults = phase1.Concat(phase2).ToDictionary(k => k.Key, k => k.Value);

        output.WriteLine($"Phase 1 (before crash): {phase1.Count}");
        output.WriteLine($"Phase 2 (after restart): {phase2.Count}");
        output.WriteLine($"Total: {allResults.Count} | Time: {sw.ElapsedMilliseconds}ms");

        // No duplicates
        Assert.Equal(total, allResults.Count);
        // No omissions
        Assert.All(employees, e => Assert.True(allResults.ContainsKey(e.EmployeeId)));
        // Phases don't overlap
        Assert.Empty(phase1.Keys.Intersect(phase2.Keys));
        // Phase counts correct
        Assert.Equal(crashAt, phase1.Count);
        Assert.Equal(total - crashAt, phase2.Count);
        // Determinism: re-calculate first 10 from phase1 and verify identical
        foreach (var e in employees.Take(10))
        {
            var reCalc = engine.Calculate("t1", runId, "Apr 2025", 1, "snap", e.Input, TaxSvc);
            Assert.Equal(phase1[e.EmployeeId], reCalc.NetPay);
        }
    }
}
