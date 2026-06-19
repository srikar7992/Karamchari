// -----------------------------------------------------------------------
// <copyright file="EnrollmentWindowTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Tests.Domain;

using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Domain.Events;

public sealed class EnrollmentWindowTests
{
    private const string TenantId = "tenant-1";
    private static readonly DateOnly PlanYearStart = new(2025, 1, 1);
    private static readonly DateOnly PlanYearEnd = new(2025, 12, 31);
    private static readonly DateTimeOffset Now = new(2025, 3, 1, 9, 0, 0, TimeSpan.Zero);

    // ── CreateForNewHire ─────────────────────────────────────────────────────

    [Fact]
    public void CreateForNewHire_opens_window_immediately()
    {
        var window = EnrollmentWindow.CreateForNewHire(TenantId, PlanYearStart, PlanYearEnd, Now);

        Assert.Equal(EnrollmentWindowStatus.Open, window.Status);
        Assert.Equal(EnrollmentWindowType.NewHire, window.WindowType);
    }

    [Fact]
    public void CreateForNewHire_closes_after_default_30_days()
    {
        var window = EnrollmentWindow.CreateForNewHire(TenantId, PlanYearStart, PlanYearEnd, Now);

        Assert.Equal(Now.AddDays(30), window.WindowClosesAt);
    }

    [Fact]
    public void CreateForNewHire_raises_EnrollmentWindowOpenedEvent()
    {
        var window = EnrollmentWindow.CreateForNewHire(TenantId, PlanYearStart, PlanYearEnd, Now);

        var ev = Assert.Single(window.DomainEvents.OfType<EnrollmentWindowOpenedEvent>());
        Assert.Equal(TenantId, ev.TenantId);
        Assert.Equal(EnrollmentWindowType.NewHire, ev.WindowType);
        Assert.Equal(PlanYearStart, ev.PlanYearStart);
    }

    [Fact]
    public void CreateForNewHire_throws_for_null_tenant()
    {
        Assert.Throws<ArgumentException>(() =>
            EnrollmentWindow.CreateForNewHire(string.Empty, PlanYearStart, PlanYearEnd, Now));
    }

    // ── CreateOpenEnrollment ─────────────────────────────────────────────────

    [Fact]
    public void CreateOpenEnrollment_starts_as_Scheduled()
    {
        var opensAt = Now.AddDays(10);
        var closesAt = Now.AddDays(40);
        var window = EnrollmentWindow.CreateOpenEnrollment(
            TenantId, PlanYearStart, PlanYearEnd, opensAt, closesAt);

        Assert.Equal(EnrollmentWindowStatus.Scheduled, window.Status);
        Assert.Equal(EnrollmentWindowType.OpenEnrollment, window.WindowType);
    }

    [Fact]
    public void CreateOpenEnrollment_throws_when_close_before_open()
    {
        Assert.Throws<ArgumentException>(() =>
            EnrollmentWindow.CreateOpenEnrollment(
                TenantId, PlanYearStart, PlanYearEnd,
                windowOpensAt: Now.AddDays(10),
                windowClosesAt: Now.AddDays(5)));
    }

    // ── Open ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Open_transitions_from_Scheduled_to_Open()
    {
        var window = EnrollmentWindow.CreateOpenEnrollment(
            TenantId, PlanYearStart, PlanYearEnd, Now.AddDays(10), Now.AddDays(40));
        window.ClearDomainEvents();

        window.Open();

        Assert.Equal(EnrollmentWindowStatus.Open, window.Status);
        Assert.NotNull(window.OpenedAtUtc);
    }

    [Fact]
    public void Open_raises_EnrollmentWindowOpenedEvent()
    {
        var window = EnrollmentWindow.CreateOpenEnrollment(
            TenantId, PlanYearStart, PlanYearEnd, Now.AddDays(10), Now.AddDays(40));
        window.ClearDomainEvents();

        window.Open();

        Assert.Single(window.DomainEvents.OfType<EnrollmentWindowOpenedEvent>());
    }

    [Fact]
    public void Open_throws_when_already_open()
    {
        var window = EnrollmentWindow.CreateForNewHire(TenantId, PlanYearStart, PlanYearEnd, Now);

        Assert.Throws<InvalidOperationException>(() => window.Open());
    }

    // ── Close ────────────────────────────────────────────────────────────────

    [Fact]
    public void Close_transitions_from_Open_to_Closed()
    {
        var window = EnrollmentWindow.CreateForNewHire(TenantId, PlanYearStart, PlanYearEnd, Now);
        window.ClearDomainEvents();

        window.Close();

        Assert.Equal(EnrollmentWindowStatus.Closed, window.Status);
        Assert.NotNull(window.ClosedAtUtc);
    }

    [Fact]
    public void Close_raises_EnrollmentWindowClosedEvent()
    {
        var window = EnrollmentWindow.CreateForNewHire(TenantId, PlanYearStart, PlanYearEnd, Now);
        window.ClearDomainEvents();

        window.Close();

        Assert.Single(window.DomainEvents.OfType<EnrollmentWindowClosedEvent>());
    }

    [Fact]
    public void Close_throws_when_not_open()
    {
        var window = EnrollmentWindow.CreateOpenEnrollment(
            TenantId, PlanYearStart, PlanYearEnd, Now.AddDays(10), Now.AddDays(40));

        Assert.Throws<InvalidOperationException>(() => window.Close());
    }
}
