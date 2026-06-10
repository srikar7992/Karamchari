// -----------------------------------------------------------------------
// <copyright file="OnboardingCase.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Onboarding.Domain.Events;

namespace Karamchari.Onboarding.Domain;

/// <summary>
/// Aggregate root for an employee's onboarding journey — new hire, crossboard, or offboard.
/// A case owns tasks and documents; completion is automatic when all non-skipped tasks are done.
/// </summary>
public sealed class OnboardingCase : AggregateRoot<OnboardingCaseId>, ITenantOwned
{
    private readonly List<OnboardingTask> _tasks = [];
    private readonly List<OnboardingDocument> _documents = [];

    private OnboardingCase() { TenantId = string.Empty; }

    private OnboardingCase(
        OnboardingCaseId id,
        string tenantId,
        Guid employeeId,
        string employeeName,
        OnboardingCaseType caseType,
        DateTimeOffset expectedStartDate,
        string? department,
        string? role,
        OnboardingTemplateId? templateId) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        CaseType = caseType;
        ExpectedStartDate = expectedStartDate;
        Department = department;
        Role = role;
        TemplateId = templateId;
        Status = OnboardingCaseStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public OnboardingCaseType CaseType { get; private set; }
    public OnboardingCaseStatus Status { get; private set; }
    public DateTimeOffset ExpectedStartDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Department { get; private set; }
    public string? Role { get; private set; }
    public OnboardingTemplateId? TemplateId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<OnboardingTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyList<OnboardingDocument> Documents => _documents.AsReadOnly();

    public static OnboardingCase Initiate(
        string tenantId,
        Guid employeeId,
        string employeeName,
        OnboardingCaseType caseType,
        DateTimeOffset expectedStartDate,
        string? department,
        string? role,
        OnboardingTemplateId? templateId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeName);

        var @case = new OnboardingCase(
            new OnboardingCaseId(Guid.NewGuid()),
            tenantId, employeeId, employeeName, caseType,
            expectedStartDate, department, role, templateId);

        @case.RaiseDomainEvent(new OnboardingCaseCreatedEvent(
            tenantId, @case.Id, employeeId, caseType, expectedStartDate));

        return @case;
    }

    public void Start()
    {
        if (Status != OnboardingCaseStatus.Pending)
            throw new InvalidOperationException($"Case already {Status}.");
        Status = OnboardingCaseStatus.InProgress;
    }

    public OnboardingTask AddTask(
        string title,
        TaskCategory category,
        string? assignedToEmployeeId,
        DateTimeOffset? dueDate,
        string? description = null)
    {
        if (Status == OnboardingCaseStatus.Completed || Status == OnboardingCaseStatus.Cancelled)
            throw new InvalidOperationException("Cannot add task to a closed case.");

        var task = new OnboardingTask(
            new OnboardingTaskId(Guid.NewGuid()),
            Id, title, category, assignedToEmployeeId, dueDate, description);
        _tasks.Add(task);
        return task;
    }

    public void CompleteTask(OnboardingTaskId taskId, Guid completedByEmployeeId, string? notes = null)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException("Task not found.");

        task.Complete(completedByEmployeeId, notes);

        RaiseDomainEvent(new OnboardingTaskCompletedEvent(
            TenantId, Id, taskId, task.Title, completedByEmployeeId));

        TryAutoComplete();
    }

    public void SkipTask(OnboardingTaskId taskId, string reason)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException("Task not found.");
        task.Skip(reason);
        TryAutoComplete();
    }

    public OnboardingDocument AddRequiredDocument(DocumentType documentType)
    {
        if (_documents.Any(d => d.DocumentType == documentType && d.Status != DocumentStatus.Rejected))
            throw new InvalidOperationException($"{documentType} already submitted or under review.");

        var doc = new OnboardingDocument(new OnboardingDocumentId(Guid.NewGuid()), Id, documentType);
        _documents.Add(doc);
        return doc;
    }

    public void SubmitDocument(
        OnboardingDocumentId documentId,
        string fileName,
        string storageReference)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new InvalidOperationException("Document record not found.");

        doc.Submit(fileName, storageReference);

        RaiseDomainEvent(new OnboardingDocumentSubmittedEvent(
            TenantId, Id, documentId, doc.DocumentType, EmployeeId));
    }

    public void VerifyDocument(OnboardingDocumentId documentId, string verifiedByEmployeeId)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new InvalidOperationException("Document record not found.");
        doc.Verify(verifiedByEmployeeId);
    }

    public void RejectDocument(OnboardingDocumentId documentId, string reason)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new InvalidOperationException("Document record not found.");
        doc.Reject(reason);
    }

    public void Cancel(string reason)
    {
        if (Status == OnboardingCaseStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed case.");
        Status = OnboardingCaseStatus.Cancelled;
        RaiseDomainEvent(new OnboardingCaseCancelledEvent(TenantId, Id, EmployeeId, reason));
    }

    /// <summary>Completion progress as a percentage (0-100).</summary>
    public int ProgressPercent()
    {
        var actionable = _tasks.Where(t => t.Status != TaskStatus.Skipped).ToList();
        if (actionable.Count == 0) return 100;
        var done = actionable.Count(t => t.Status == TaskStatus.Completed);
        return (int)Math.Round((double)done / actionable.Count * 100);
    }

    private void TryAutoComplete()
    {
        if (Status != OnboardingCaseStatus.InProgress) return;
        var allDone = _tasks.All(t => t.Status is TaskStatus.Completed or TaskStatus.Skipped);
        if (!allDone) return;

        Status = OnboardingCaseStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new OnboardingCaseCompletedEvent(TenantId, Id, EmployeeId, CaseType));
    }
}
