// -----------------------------------------------------------------------
// <copyright file="EmployeeCompensationRecordTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compensation.Contracts.IntegrationEvents;
using Karamchari.Compensation.Domain;

namespace Karamchari.Compensation.Tests.Domain;

public sealed class EmployeeCompensationRecordTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private static EmployeeCompensationRecord CreateInitialRecord(
        decimal annualCTC = 800_000m,
        string currency = "INR")
        => EmployeeCompensationRecord.CreateInitial(
            tenantId: "tenant-001",
            employeeId: Guid.NewGuid(),
            annualCTC: annualCTC,
            currencyCode: currency);

    private static EmployeeCompensationRevisedIntegrationEventV1 MakeRevisionEvent(
        Guid? revisionId = null,
        Guid? employeeId = null,
        decimal previousCTC = 800_000m,
        decimal newCTC = 880_000m,
        decimal incrementPercent = 10m,
        CompensationRevisionReason reason = CompensationRevisionReason.MeritIncrease,
        string? referenceId = null)
        => new EmployeeCompensationRevisedIntegrationEventV1(
            RevisionId: revisionId ?? Guid.NewGuid(),
            TenantId: "tenant-001",
            EmployeeId: employeeId ?? Guid.NewGuid(),
            PreviousAnnualCTC: previousCTC,
            NewAnnualCTC: newCTC,
            IncrementPercent: incrementPercent,
            Reason: reason,
            ReferenceId: referenceId,
            EffectiveDate: new DateOnly(2026, 4, 1),
            OccurredOnUtc: DateTimeOffset.UtcNow);

    // ── CreateInitial ────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeCompensationRecord_CreateInitial_SetsCurrentCTCAndCurrency()
    {
        var record = CreateInitialRecord(annualCTC: 800_000m, currency: "INR");

        record.Should().NotBeNull();
        record.Id.Should().NotBeEmpty();
        record.TenantId.Should().Be("tenant-001");
        record.CurrentAnnualCTC.Should().Be(800_000m);
        record.CurrencyCode.Should().Be("INR");
        record.Revisions.Should().BeEmpty();
        record.LastRevisedOnUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── ApplyRevision ────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeCompensationRecord_ApplyRevision_AddsRevisionEntry()
    {
        var record = CreateInitialRecord(annualCTC: 800_000m);
        var evt = MakeRevisionEvent(previousCTC: 800_000m, newCTC: 880_000m, incrementPercent: 10m);

        record.ApplyRevision(evt);

        record.Revisions.Should().HaveCount(1);
        var revision = record.Revisions.First();
        revision.ExternalRevisionId.Should().Be(evt.RevisionId);
        revision.PreviousAnnualCTC.Should().Be(800_000m);
        revision.NewAnnualCTC.Should().Be(880_000m);
        revision.IncrementPercent.Should().Be(10m);
        revision.Reason.Should().Be(CompensationRevisionReason.MeritIncrease.ToString());
        revision.EffectiveDate.Should().Be(evt.EffectiveDate);
    }

    [Fact]
    public void EmployeeCompensationRecord_ApplyRevision_UpdatesCurrentCTC()
    {
        var record = CreateInitialRecord(annualCTC: 800_000m);
        var evt = MakeRevisionEvent(previousCTC: 800_000m, newCTC: 960_000m, incrementPercent: 20m);

        record.ApplyRevision(evt);

        record.CurrentAnnualCTC.Should().Be(960_000m);
        record.LastRevisedOnUtc.Should().Be(evt.OccurredOnUtc);
    }

    [Fact]
    public void EmployeeCompensationRecord_ApplyRevision_DuplicateExternalRevisionId_IsIdempotent()
    {
        // Design intent: re-delivering the same integration event must be safe.
        // The aggregate should skip the revision if ExternalRevisionId already exists.
        var record = CreateInitialRecord(annualCTC: 800_000m);
        var sharedRevisionId = Guid.NewGuid();

        var firstEvent = MakeRevisionEvent(
            revisionId: sharedRevisionId,
            previousCTC: 800_000m,
            newCTC: 880_000m);

        var duplicateEvent = MakeRevisionEvent(
            revisionId: sharedRevisionId,  // same ID — duplicate delivery
            previousCTC: 800_000m,
            newCTC: 880_000m);

        record.ApplyRevision(firstEvent);
        record.ApplyRevision(duplicateEvent);

        // Idempotency contract: only one revision entry, CTC stays at 880 000
        record.Revisions.Should().HaveCount(1,
            "duplicate ExternalRevisionId must be deduplicated");
        record.CurrentAnnualCTC.Should().Be(880_000m);
    }

    [Fact]
    public void EmployeeCompensationRecord_ApplyRevision_MultipleRevisions_AppendHistory()
    {
        var record = CreateInitialRecord(annualCTC: 800_000m);

        var first = MakeRevisionEvent(
            previousCTC: 800_000m, newCTC: 880_000m, incrementPercent: 10m,
            reason: CompensationRevisionReason.MeritIncrease);

        var second = MakeRevisionEvent(
            previousCTC: 880_000m, newCTC: 968_000m, incrementPercent: 10m,
            reason: CompensationRevisionReason.Promotion);

        record.ApplyRevision(first);
        record.ApplyRevision(second);

        record.Revisions.Should().HaveCount(2);
        record.CurrentAnnualCTC.Should().Be(968_000m);

        var revList = record.Revisions.ToList();
        revList[0].NewAnnualCTC.Should().Be(880_000m);
        revList[1].NewAnnualCTC.Should().Be(968_000m);
        revList[1].Reason.Should().Be(CompensationRevisionReason.Promotion.ToString());
    }
}
