// -----------------------------------------------------------------------
// <copyright file="LeaveInvestigationCase.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Leaves;

public enum InvestigationStatus
{
    Open = 1,
    InReview = 2,
    PendingEvidence = 3,
    Resolved = 4,
    Closed = 5,
}

public enum InvestigationResolution
{
    Upheld = 1,
    Dismissed = 2,
    PartiallyUpheld = 3,
    Escalated = 4,
}

/// <summary>
/// HR investigation case opened when an employee disputes their leave balance, an anomaly is flagged,
/// or a compliance violation is raised. Links to LeaveTimelineEntry, LeaveBalance ledger,
/// LeaveApproval chain, AttendanceRecord, and PayrollLeaveAdvisory for a unified evidence dossier.
/// </summary>
public sealed class LeaveInvestigationCase : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<InvestigationEvidenceLink> _evidenceLinks = [];
    private readonly List<InvestigationNote> _notes = [];

    private LeaveInvestigationCase() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string CaseNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string OpenedBy { get; private set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; private set; }
    public InvestigationStatus Status { get; private set; }
    public InvestigationResolution? Resolution { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }

    public IReadOnlyCollection<InvestigationEvidenceLink> EvidenceLinks => _evidenceLinks.AsReadOnly();
    public IReadOnlyCollection<InvestigationNote> Notes => _notes.AsReadOnly();

    public static LeaveInvestigationCase Open(
        string tenantId,
        Guid employeeId,
        string caseNumber,
        string title,
        string description,
        string openedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(openedBy);

        return new LeaveInvestigationCase
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            CaseNumber = caseNumber,
            Title = title,
            Description = description,
            OpenedBy = openedBy,
            OpenedAt = DateTimeOffset.UtcNow,
            Status = InvestigationStatus.Open,
        };
    }

    public void LinkEvidence(string entityType, Guid entityId, string description)
    {
        if (Status == InvestigationStatus.Closed)
            throw new InvalidOperationException("Cannot modify a closed investigation.");

        _evidenceLinks.Add(new InvestigationEvidenceLink
        {
            Id = Guid.NewGuid(),
            CaseId = Id,
            TenantId = TenantId,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            LinkedAt = DateTimeOffset.UtcNow,
        });
    }

    public void AddNote(string authorId, string content)
    {
        if (Status == InvestigationStatus.Closed)
            throw new InvalidOperationException("Cannot add notes to a closed investigation.");

        _notes.Add(new InvestigationNote
        {
            Id = Guid.NewGuid(),
            CaseId = Id,
            TenantId = TenantId,
            AuthorId = authorId,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }

    public void StartReview() => Transition(InvestigationStatus.InReview);

    public void RequestEvidence() => Transition(InvestigationStatus.PendingEvidence);

    public void Resolve(string resolvedBy, InvestigationResolution resolution, string notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(notes);

        Transition(InvestigationStatus.Resolved);
        Resolution = resolution;
        ResolutionNotes = notes;
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Status != InvestigationStatus.Resolved)
            throw new InvalidOperationException("Investigation must be resolved before closing.");
        Status = InvestigationStatus.Closed;
    }

    private void Transition(InvestigationStatus target)
    {
        if (Status == InvestigationStatus.Closed)
            throw new InvalidOperationException("Investigation is closed.");
        Status = target;
    }
}

public sealed class InvestigationEvidenceLink
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid CaseId { get; init; }
    /// <summary>"LeaveRequest" | "LeaveBalance" | "LeaveApproval" | "AttendanceRecord" | "PayrollAdvisory" | "LeaveTimelineEntry"</summary>
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset LinkedAt { get; init; }
}

public sealed class InvestigationNote
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid CaseId { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
