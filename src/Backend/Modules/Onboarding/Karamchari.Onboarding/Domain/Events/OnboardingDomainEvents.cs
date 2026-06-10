// -----------------------------------------------------------------------
// <copyright file="OnboardingDomainEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Onboarding.Domain.Events;

public sealed record OnboardingCaseCreatedEvent(
    string TenantId,
    OnboardingCaseId CaseId,
    Guid EmployeeId,
    OnboardingCaseType CaseType,
    DateTimeOffset ExpectedStartDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record OnboardingTaskCompletedEvent(
    string TenantId,
    OnboardingCaseId CaseId,
    OnboardingTaskId TaskId,
    string TaskTitle,
    Guid CompletedByEmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record OnboardingDocumentSubmittedEvent(
    string TenantId,
    OnboardingCaseId CaseId,
    OnboardingDocumentId DocumentId,
    DocumentType DocumentType,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record OnboardingCaseCompletedEvent(
    string TenantId,
    OnboardingCaseId CaseId,
    Guid EmployeeId,
    OnboardingCaseType CaseType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record OnboardingCaseCancelledEvent(
    string TenantId,
    OnboardingCaseId CaseId,
    Guid EmployeeId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
