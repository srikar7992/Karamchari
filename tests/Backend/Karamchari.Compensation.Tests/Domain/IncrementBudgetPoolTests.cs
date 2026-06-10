// -----------------------------------------------------------------------
// <copyright file="IncrementBudgetPoolTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compensation.Domain;

namespace Karamchari.Compensation.Tests.Domain;

public sealed class IncrementBudgetPoolTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private static IncrementBudgetPool CreatePool(decimal totalBudget = 500_000m)
        => IncrementBudgetPool.Create(
            tenantId: "tenant-001",
            departmentId: Guid.NewGuid(),
            departmentName: "Engineering",
            reviewYear: 2026,
            totalBudget: totalBudget,
            currencyCode: "INR");

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void IncrementBudgetPool_Create_StatusIsDraft()
    {
        var pool = CreatePool();

        pool.Should().NotBeNull();
        pool.Id.Should().NotBeEmpty();
        pool.Status.Should().Be(BudgetStatus.Draft);
        pool.AllocatedBudget.Should().Be(0m);
        pool.TotalBudget.Should().Be(500_000m);
        pool.RemainingBudget.Should().Be(500_000m);
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    [Fact]
    public void IncrementBudgetPool_Approve_FromDraft_StatusBecomesApproved()
    {
        var pool = CreatePool();

        pool.Approve();

        pool.Status.Should().Be(BudgetStatus.Approved);
    }

    [Fact]
    public void IncrementBudgetPool_Approve_FromApproved_ThrowsException()
    {
        var pool = CreatePool();
        pool.Approve(); // already approved

        Action act = () => pool.Approve();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*approve*");
    }

    // ── Allocate ─────────────────────────────────────────────────────────────

    [Fact]
    public void IncrementBudgetPool_Allocate_FromApproved_DecreasesBudget()
    {
        var pool = CreatePool(totalBudget: 500_000m);
        pool.Approve();

        pool.Allocate(100_000m);

        pool.AllocatedBudget.Should().Be(100_000m);
        pool.RemainingBudget.Should().Be(400_000m);
        pool.Status.Should().Be(BudgetStatus.Allocated);
    }

    [Fact]
    public void IncrementBudgetPool_Allocate_ExceedsBudget_ThrowsDomainException()
    {
        var pool = CreatePool(totalBudget: 500_000m);
        pool.Approve();

        Action act = () => pool.Allocate(600_000m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds remaining budget*");
    }

    [Fact]
    public void IncrementBudgetPool_Allocate_FromDraft_ThrowsException()
    {
        var pool = CreatePool(); // status = Draft, not Approved

        Action act = () => pool.Allocate(100_000m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*approved*");
    }

    // ── Close ────────────────────────────────────────────────────────────────

    [Fact]
    public void IncrementBudgetPool_Close_FromAllocated_StatusBecomesClosed()
    {
        var pool = CreatePool();
        pool.Approve();
        pool.Allocate(50_000m); // status → Allocated

        pool.Close();

        pool.Status.Should().Be(BudgetStatus.Closed);
    }

    [Fact]
    public void IncrementBudgetPool_Close_FromApproved_StatusBecomesClosed()
    {
        // Closing an approved (but not yet allocated) pool is also valid;
        // the guard only blocks Draft pools.
        var pool = CreatePool();
        pool.Approve();

        pool.Close();

        pool.Status.Should().Be(BudgetStatus.Closed);
    }

    [Fact]
    public void IncrementBudgetPool_Close_FromDraft_ThrowsException()
    {
        var pool = CreatePool(); // Draft — never approved

        Action act = () => pool.Close();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*never approved*");
    }
}
