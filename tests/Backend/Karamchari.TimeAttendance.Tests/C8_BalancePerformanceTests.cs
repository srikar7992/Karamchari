// -----------------------------------------------------------------------
// <copyright file="C8_BalancePerformanceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Concern 8 — O(n) balance calculation fix verification.
/// AvailableBalance now uses _runningBalance (lazy-initialized O(1) cache updated in AddEntry).
/// These tests verify correctness of the cache and that performance is acceptable.
/// </summary>
public sealed class C8_BalancePerformanceTests(ITestOutputHelper output)
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Balance_O1Cache_50kEntries_CompletesUnder2Seconds()
    {
        // Previously: 50k consume+restore = 100k entries → Sum() was 49s.
        // Now: _runningBalance updated in AddEntry → O(1) reads.
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(100_000m, Today);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 50_000; i++)
        {
            b.Consume(1m, Today, $"r{i}");
            b.Restore(1m, Today, $"r{i}");
        }

        sw.Stop();
        output.WriteLine($"50k consume+restore with O(1) cache: {sw.ElapsedMilliseconds}ms");

        sw.ElapsedMilliseconds.Should().BeLessThan(2_000,
            "O(1) cache makes 100k entry balance fast — previously took 49s due to O(n) Sum()");

        b.AvailableBalance.Should().Be(100_000m, "cache must be correct");
        b.VerifyBalanceIntegrity().Should().BeTrue("O(n) integrity check confirms cache correct");
    }

    [Fact]
    public void Balance_Cache_NeverDivergesFromLedgerSum()
    {
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(1_000m, Today);

        for (int i = 0; i < 200; i++)
        {
            b.Consume(2m, Today, $"c{i}");
            b.Restore(1m, Today, $"r{i}");
        }

        b.Expire(100m, Today);
        b.CarryForward(50m, Today);

        b.AvailableBalance.Should().Be(b.Entries.Sum(e => e.Quantity),
            "cache must always equal ledger sum");
    }

    [Fact]
    public void Balance_Cache_LazyInit_EFMaterializationPath_Correct()
    {
        // EF calls parameterless ctor → then populates _entries via OwnsMany.
        // First AvailableBalance read triggers _runningBalance ??= _entries.Sum().
        // Subsequent reads use cached value.
        // Simulate by creating a balance and verifying first access returns correct sum.
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(18m, Today);
        b.Consume(3m, Today, "req1");

        // First read (lazy init)
        var first = b.AvailableBalance;
        // Second read (cache hit)
        var second = b.AvailableBalance;

        first.Should().Be(15m);
        second.Should().Be(15m);
        first.Should().Be(second, "cache returns same value on repeated reads");
    }

    [Fact]
    public void Balance_LOPAdjust_Cache_UpdatedCorrectly()
    {
        // LOPConversion bypasses AddEntry guard — cache must still be updated.
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(10m, Today);

        b.Adjust(-3m, Today, "hr", AdjustmentReason.LOPConversion);

        b.AvailableBalance.Should().Be(7m, "LOP bypass still updates cache correctly");
        b.AvailableBalance.Should().Be(b.Entries.Sum(e => e.Quantity), "invariant holds after LOP");
    }

    [Fact]
    public void Balance_PayrollCorrection_NegativeResult_Cache_Correct()
    {
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(5m, Today);

        b.Adjust(-8m, Today, "payroll", AdjustmentReason.PayrollCorrection);

        b.AvailableBalance.Should().Be(-3m);
        b.AvailableBalance.Should().Be(b.Entries.Sum(e => e.Quantity));
    }

    [Fact]
    public void Balance_AccrualPaused_Cache_NotUpdatedWhenPaused()
    {
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(10m, Today);
        b.PauseAccrual("LOP");

        var before = b.AvailableBalance;
        b.Accrue(5m, Today); // no-op
        var after = b.AvailableBalance;

        before.Should().Be(10m);
        after.Should().Be(10m, "paused accrual skips AddEntry → cache unchanged");
    }

    [Fact]
    public void Balance_MultipleAccruals_Cache_Additive()
    {
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        for (int i = 0; i < 12; i++)
            b.Accrue(1.5m, Today.AddMonths(i));

        b.AvailableBalance.Should().Be(18m);
        b.AvailableBalance.Should().Be(b.Entries.Sum(e => e.Quantity));
    }
}
