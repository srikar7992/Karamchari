// -----------------------------------------------------------------------
// <copyright file="EmployeeCompensationRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compensation.Contracts.IntegrationEvents;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

/// <summary>
/// Current and historical compensation for an employee.
/// Owned by the Compensation BC. Updated by consuming EmployeeCompensationRevisedIntegrationEvent.
/// HR-visibility only: this aggregate must be behind the compensation:read RBAC claim.
/// </summary>
public sealed class EmployeeCompensationRecord : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<CompensationRevision> _revisions = [];
    private EmployeeCompensationRecord() { /* EF materialization */ }

    private EmployeeCompensationRecord(
        Guid id,
        string tenantId,
        Guid employeeId,
        decimal currentAnnualCTC,
        string currencyCode) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        CurrentAnnualCTC = currentAnnualCTC;
        CurrencyCode = currencyCode;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal CurrentAnnualCTC { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrencyCode { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastRevisedOnUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<CompensationRevision> Revisions => _revisions.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static EmployeeCompensationRecord CreateInitial(
        string tenantId,
        Guid employeeId,
        decimal annualCTC,
        string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentOutOfRangeException.ThrowIfNegative(annualCTC);

        var record = new EmployeeCompensationRecord(Guid.NewGuid(), tenantId, employeeId, annualCTC, currencyCode);
        record.LastRevisedOnUtc = DateTimeOffset.UtcNow;
        return record;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ApplyRevision(EmployeeCompensationRevisedIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (_revisions.Any(r => r.ExternalRevisionId == @event.RevisionId))
            return;

        var revision = new CompensationRevision(
            Guid.NewGuid(), Id,
            @event.RevisionId,
            @event.PreviousAnnualCTC,
            @event.NewAnnualCTC,
            @event.IncrementPercent,
            @event.Reason.ToString(),
            @event.ReferenceId,
            @event.EffectiveDate,
            @event.OccurredOnUtc);
        _revisions.Add(revision);

        CurrentAnnualCTC = @event.NewAnnualCTC;
        LastRevisedOnUtc = @event.OccurredOnUtc;
    }
}

/// <summary>Immutable record of one compensation revision. Append-only.</summary>
public sealed class CompensationRevision : Entity<Guid>
{
    internal CompensationRevision(
        Guid id,
        Guid compensationRecordId,
        Guid externalRevisionId,
        decimal previousAnnualCTC,
        decimal newAnnualCTC,
        decimal incrementPercent,
        string reason,
        string? referenceId,
        DateOnly effectiveDate,
        DateTimeOffset occurredOnUtc) : base(id)
    {
        CompensationRecordId = compensationRecordId;
        ExternalRevisionId = externalRevisionId;
        PreviousAnnualCTC = previousAnnualCTC;
        NewAnnualCTC = newAnnualCTC;
        IncrementPercent = incrementPercent;
        Reason = reason;
        ReferenceId = referenceId;
        EffectiveDate = effectiveDate;
        OccurredOnUtc = occurredOnUtc;
    }

    private CompensationRevision() { /* EF materialization */ }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CompensationRecordId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ExternalRevisionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PreviousAnnualCTC { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NewAnnualCTC { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal IncrementPercent { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;
    public string? ReferenceId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }
}
