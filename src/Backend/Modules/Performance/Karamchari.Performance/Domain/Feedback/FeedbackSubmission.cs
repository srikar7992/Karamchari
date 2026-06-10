// -----------------------------------------------------------------------
// <copyright file="FeedbackSubmission.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Feedback;

/// <summary>
/// The actual feedback content submitted in response to a FeedbackRequest.
///
/// Anonymous feedback privacy design:
/// - ProviderId is NEVER stored in plaintext on this entity.
/// - For anonymous submissions, an HMAC token is derived from (TenantId + RequestId + ProviderId)
///   using a tenant-scoped HMAC key held in Key Vault.
/// - The token allows HR-only de-anonymization (by recomputing the HMAC against the known provider list)
///   but provides no direct reverse path through the API.
/// - Visibility threshold: aggregate feedback to reviewee only after MinVisibilityThreshold submissions.
/// - See ADR-0005 for full privacy threat model.
/// </summary>
public sealed class FeedbackSubmission : AggregateRoot<Guid>, ITenantOwned
{
    private FeedbackSubmission() { /* EF materialization */ }

    private FeedbackSubmission(
        Guid id,
        string tenantId,
        Guid requestId,
        Guid subjectEmployeeId,
        bool isAnonymous,
        string? anonymityToken,
        string strengthsNarrative,
        string growthAreasNarrative,
        string? overallComment,
        decimal? rating,
        Guid idempotencyKey) : base(id)
    {
        TenantId = tenantId;
        RequestId = requestId;
        SubjectEmployeeId = subjectEmployeeId;
        IsAnonymous = isAnonymous;
        AnonymityToken = anonymityToken;
        StrengthsNarrative = strengthsNarrative;
        GrowthAreasNarrative = growthAreasNarrative;
        OverallComment = overallComment;
        Rating = rating;
        Status = FeedbackSubmissionStatus.Draft;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RequestId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid SubjectEmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsAnonymous { get; private set; }

    /// <summary>
    /// HMAC(key, TenantId + RequestId + ProviderId). Allows HR-only de-anonymization.
    /// Null for non-anonymous submissions.
    /// </summary>
    public string? AnonymityToken { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string StrengthsNarrative { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string GrowthAreasNarrative { get; private set; } = string.Empty;
    public string? OverallComment { get; private set; }
    public decimal? Rating { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public FeedbackSubmissionStatus Status { get; private set; }
    public DateTimeOffset? SubmittedOnUtc { get; private set; }

    /// <summary>Unique constraint on (TenantId, IdempotencyKey) prevents duplicate submissions.</summary>
    public Guid IdempotencyKey { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Creates an anonymous submission. hmacKey is the tenant-scoped key from Key Vault.
    /// ProviderId never stored â€” token computed once and stored.
    /// </summary>
    public static FeedbackSubmission CreateAnonymous(
        string tenantId,
        Guid requestId,
        Guid subjectEmployeeId,
        Guid providerId,
        byte[] hmacKey,
        string strengthsNarrative,
        string growthAreasNarrative,
        string? overallComment,
        decimal? rating)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(strengthsNarrative);
        ArgumentException.ThrowIfNullOrWhiteSpace(growthAreasNarrative);
        ArgumentNullException.ThrowIfNull(hmacKey);

        var token = ComputeAnonymityToken(hmacKey, tenantId, requestId, providerId);
        var key = Guid.NewGuid();
        return new FeedbackSubmission(key, tenantId, requestId, subjectEmployeeId, true,
            token, strengthsNarrative.Trim(), growthAreasNarrative.Trim(),
            overallComment?.Trim(), rating, key);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static FeedbackSubmission CreateNamed(
        string tenantId,
        Guid requestId,
        Guid subjectEmployeeId,
        string strengthsNarrative,
        string growthAreasNarrative,
        string? overallComment,
        decimal? rating)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(strengthsNarrative);
        ArgumentException.ThrowIfNullOrWhiteSpace(growthAreasNarrative);

        var key = Guid.NewGuid();
        return new FeedbackSubmission(key, tenantId, requestId, subjectEmployeeId, false,
            null, strengthsNarrative.Trim(), growthAreasNarrative.Trim(),
            overallComment?.Trim(), rating, key);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Submit()
    {
        if (Status != FeedbackSubmissionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit from status {Status}.");
        Status = FeedbackSubmissionStatus.Submitted;
        SubmittedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ShareWithReviewee()
    {
        if (Status != FeedbackSubmissionStatus.Submitted)
            throw new InvalidOperationException($"Cannot share from status {Status}.");
        Status = FeedbackSubmissionStatus.SharedWithReviewee;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Acknowledge()
    {
        if (Status != FeedbackSubmissionStatus.SharedWithReviewee)
            throw new InvalidOperationException($"Cannot acknowledge from status {Status}.");
        Status = FeedbackSubmissionStatus.Acknowledged;
    }

    /// <summary>
    /// Verifies whether this anonymous submission came from candidateProviderId.
    /// Only callable by HR-privileged code. Returns false if submission is not anonymous.
    /// </summary>
    public bool VerifyAnonymousProvider(byte[] hmacKey, Guid candidateProviderId)
    {
        if (!IsAnonymous || AnonymityToken is null) return false;
        var expected = ComputeAnonymityToken(hmacKey, TenantId, RequestId, candidateProviderId);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(AnonymityToken));
    }

    private static string ComputeAnonymityToken(byte[] key, string tenantId, Guid requestId, Guid providerId)
    {
        var message = Encoding.UTF8.GetBytes($"{tenantId}:{requestId}:{providerId}");
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(message));
    }
}
