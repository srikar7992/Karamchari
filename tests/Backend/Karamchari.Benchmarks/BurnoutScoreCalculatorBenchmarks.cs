// -----------------------------------------------------------------------
// <copyright file="BurnoutScoreCalculatorBenchmarks.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Karamchari.Intelligence.Services.Scoring;

namespace Karamchari.Benchmarks;

/// <summary>
/// Baseline benchmarks for the pure workforce-intelligence calculators. These are the
/// hot inner loops of nightly scoring runs (executed once per employee per tenant), so
/// they are the first place to look before reaching for Span/ArrayPool optimizations.
/// Run with: dotnet run -c Release --project tests/Backend/Karamchari.Benchmarks
/// </summary>
[MemoryDiagnoser]
public class BurnoutScoreCalculatorBenchmarks
{
    private BurnoutSignalInput _typical = null!;
    private BurnoutSignalInput _highRisk = null!;
    private BurnoutSignalInput[] _tenantBatch = null!;

    /// <summary>Builds representative inputs once per benchmark run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _typical = new BurnoutSignalInput(
            EmployeeId: Guid.NewGuid(),
            ConsecutiveWorkDays: 5,
            OvertimeHours28d: 8m,
            DaysWithoutLeave: 30,
            LateArrivalsCurrentMonth: 1,
            LateArrivalsPreviousMonth: 0,
            LateArrivalsMonthMinus2: 1,
            ShiftSwaps30d: 1,
            HighIntensityShiftRatio: 0.2m,
            EmergencyFillIns90d: 0,
            DataAgeDays: 1,
            IsNewJoiner: false,
            ShiftCategory: "Day");

        _highRisk = new BurnoutSignalInput(
            EmployeeId: Guid.NewGuid(),
            ConsecutiveWorkDays: 14,
            OvertimeHours28d: 60m,
            DaysWithoutLeave: 180,
            LateArrivalsCurrentMonth: 6,
            LateArrivalsPreviousMonth: 4,
            LateArrivalsMonthMinus2: 1,
            ShiftSwaps30d: 8,
            HighIntensityShiftRatio: 0.8m,
            EmergencyFillIns90d: 6,
            DataAgeDays: 1,
            IsNewJoiner: false,
            ShiftCategory: "Night");

        // Simulates one mid-size tenant's nightly scoring sweep.
        _tenantBatch = Enumerable.Range(0, 5_000)
            .Select(i => i % 10 == 0 ? _highRisk : _typical)
            .ToArray();
    }

    /// <summary>Single typical-employee calculation.</summary>
    [Benchmark(Baseline = true)]
    public BurnoutScoreResult SingleTypical() => BurnoutScoreCalculator.Calculate(_typical);

    /// <summary>Single high-risk-employee calculation (all signal branches active).</summary>
    [Benchmark]
    public BurnoutScoreResult SingleHighRisk() => BurnoutScoreCalculator.Calculate(_highRisk);

    /// <summary>5,000-employee tenant sweep — the shape of the nightly scoring loop.</summary>
    [Benchmark]
    public decimal TenantSweep5000()
    {
        var total = 0m;
        foreach (var input in _tenantBatch)
        {
            total += BurnoutScoreCalculator.Calculate(input).Score;
        }

        return total;
    }
}
