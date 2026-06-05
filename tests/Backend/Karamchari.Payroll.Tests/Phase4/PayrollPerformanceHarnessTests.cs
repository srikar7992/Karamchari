using System.Diagnostics;
using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Deductions;
using Xunit.Abstractions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// Pure in-memory performance harness.
/// Measures PayrollCalculationEngine throughput at scale.
/// No IO, no DB — isolates engine CPU cost only.
///
/// Targets (single-core in-process, no DB overhead):
///   10,000 employees  < 30 seconds
///   Memory per employee < 50 KB
/// </summary>
public class PayrollPerformanceHarnessTests(ITestOutputHelper output)
{
    private static readonly TaxCalculatorService TaxSvc = new();

    private static PayrollCalculationEngine Engine() => new([
        new ProvidentFundCalculator(),
        new EsiCalculator(),
        new ProfessionalTaxCalculator(),
    ]);

    private static StatutoryConfigSnapshot Stat() =>
        new(15_000m, 21_000m, 0.0075m, 0.0325m, 0.12m, 0.0367m, 1_250m);

    private static OvertimePolicySnapshot OtPolicy() =>
        new([new OvertimeRuleSnapshot(0m, 4m, 1.5m, "Regular", 1),
             new OvertimeRuleSnapshot(4m, null, 2.0m, "Regular", 2)]);

    private static List<DayAttendanceRecord> RealisticAttendance(int seed)
    {
        var rng = new Random(seed);
        var days = new List<DayAttendanceRecord>(26);
        var start = new DateOnly(2025, 4, 1);
        for (var i = 0; i < 26; i++)
        {
            var present = rng.NextDouble() > 0.05; // ~5% absent
            var hours = present ? (decimal)(8 + rng.Next(0, 4)) : 0m; // 8-11 hours
            TimeOnly? shiftStart = rng.NextDouble() > 0.7
                ? new TimeOnly(22, 0)  // 30% night shift
                : null;
            days.Add(new DayAttendanceRecord(
                start.AddDays(i), hours, present, false, false, shiftStart, null));
        }
        return days;
    }

    private static EmployeePayrollInput BuildInput(int index)
    {
        var rng = new Random(index);
        var salary = 20_000m + index % 130_000m; // 20k–150k spread
        var basicPct = 40m;
        var hourlyRate = Math.Round(salary / (26m * 8m), 2);
        var ytdGross = salary * (index % 10); // 0–9 months of ytd
        var states = new[] { "KA", "MH", "AP", "TS", "WB", "TN", "GJ", "MP", "OD", "AS" };
        var state = states[index % 10];
        var regime = index % 3 == 0 ? "Old" : "New";

        return new EmployeePayrollInput(
            Guid.NewGuid(), $"Employee_{index}", salary, hourlyRate, "INR",
            basicPct, 0m,
            RealisticAttendance(index),
            Array.Empty<HolidayRecord>(),
            OtPolicy(),
            [new ShiftPremiumSnapshot("Night", 30m, new TimeOnly(22, 0), new TimeOnly(6, 0), false, false)],
            [new PendingAdjustmentRecord(Guid.NewGuid(), index % 5 == 0 ? 500m : 0m, "Credit", "Performance")],
            index % 7 == 0, // voluntary PF for ~14%
            index % 11 == 0, // ESIC locked for ~9%
            state, regime,
            ytdGross, 0m, index % 2 == 0 ? 100_000m : 0m,
            4, 2025,
            Guid.NewGuid(), "FY2024-25-V1", Stat());
    }

    [Fact]
    public void Performance_10000Employees_Under30Seconds()
    {
        const int count = 10_000;
        var runId = Guid.NewGuid();
        var engine = Engine();

        // Force GC baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memBefore = GC.GetTotalAllocatedBytes(precise: false);

        var sw = Stopwatch.StartNew();
        var results = new List<decimal>(count);

        for (var i = 0; i < count; i++)
        {
            var input = BuildInput(i);
            var r = engine.Calculate("perf-tenant", runId, "Apr 2025", 1, "snap", input, TaxSvc);
            results.Add(r.NetPay);
        }

        sw.Stop();
        var memAfter = GC.GetTotalAllocatedBytes(precise: false);
        var allocatedMb = (memAfter - memBefore) / 1_048_576.0;
        var elapsedMs = sw.ElapsedMilliseconds;
        var perEmployee = (double)elapsedMs / count;
        var allocPerEmployee = allocatedMb * 1024.0 / count; // KB per employee

        output.WriteLine($"10,000 employees: {elapsedMs}ms total | {perEmployee:F2}ms/employee");
        output.WriteLine($"Allocated: {allocatedMb:F1} MB total | {allocPerEmployee:F1} KB/employee");
        output.WriteLine($"Net pay range: {results.Min():N0}–{results.Max():N0}");
        output.WriteLine($"Zero-net count: {results.Count(n => n <= 0)} (absent employees)");

        // Correctness: all results computed
        Assert.Equal(count, results.Count);
        // Performance gate: engine alone must finish well under production budget
        Assert.True(elapsedMs < 30_000,
            $"Engine took {elapsedMs}ms for {count} employees — exceeds 30s gate");
        // Memory gate: < 50 KB allocated per employee on average
        Assert.True(allocPerEmployee < 50.0,
            $"Allocated {allocPerEmployee:F1} KB/employee — exceeds 50 KB gate");
    }

    [Fact]
    public void Performance_1000Employees_Baseline_Under3Seconds()
    {
        const int count = 1_000;
        var engine = Engine();
        var runId = Guid.NewGuid();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < count; i++)
        {
            var input = BuildInput(i);
            engine.Calculate("perf-tenant", runId, "Apr 2025", 1, "snap", input, TaxSvc);
        }
        sw.Stop();

        output.WriteLine($"1,000 employees: {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 3_000,
            $"1k employees took {sw.ElapsedMilliseconds}ms — exceeds 3s gate");
    }

    [Fact]
    public void Performance_SingleEmployee_Under5Milliseconds()
    {
        var engine = Engine();
        var input = BuildInput(42);
        var runId = Guid.NewGuid();

        // Warm up JIT
        engine.Calculate("t", runId, "Apr 2025", 1, "snap", input, TaxSvc);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
            engine.Calculate("t", runId, "Apr 2025", 1, "snap", input, TaxSvc);
        sw.Stop();

        var avgMs = (double)sw.ElapsedMilliseconds / 100;
        output.WriteLine($"Single employee avg (100 runs): {avgMs:F3}ms");
        Assert.True(avgMs < 5.0, $"Single calculation avg {avgMs:F3}ms — exceeds 5ms gate");
    }
}
