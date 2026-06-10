// -----------------------------------------------------------------------
// <copyright file="OnboardingDocument.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Onboarding.Domain;

/// <summary>
/// A required compliance document to be submitted by the employee.
/// Owned by OnboardingCase aggregate.
/// </summary>
public sealed class OnboardingDocument : Entity<OnboardingDocumentId>
{
    private OnboardingDocument() { }

    internal OnboardingDocument(
        OnboardingDocumentId id,
        OnboardingCaseId caseId,
        DocumentType documentType) : base(id)
    {
        CaseId = caseId;
        DocumentType = documentType;
        Status = DocumentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public OnboardingCaseId CaseId { get; private set; } = null!;
    public DocumentType DocumentType { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? FileName { get; private set; }
    public string? StorageReference { get; private set; }
    public string? VerifiedByEmployeeId { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }

    internal void Submit(string fileName, string storageReference)
    {
        if (Status == DocumentStatus.Verified)
            throw new InvalidOperationException("Document already verified.");

        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageReference);

        FileName = fileName;
        StorageReference = storageReference;
        Status = DocumentStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
        RejectionReason = null;
    }

    internal void Verify(string verifiedByEmployeeId)
    {
        if (Status != DocumentStatus.Submitted)
            throw new InvalidOperationException("Only submitted documents can be verified.");

        Status = DocumentStatus.Verified;
        VerifiedByEmployeeId = verifiedByEmployeeId;
        VerifiedAt = DateTimeOffset.UtcNow;
    }

    internal void Reject(string reason)
    {
        if (Status != DocumentStatus.Submitted)
            throw new InvalidOperationException("Only submitted documents can be rejected.");

        Status = DocumentStatus.Rejected;
        RejectionReason = reason;
    }
}
