using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Domain.Primitives;

namespace Karamchari.Recruitment.Domain.Candidates;

/// <summary>
/// Aggregate root representing the global identity of a Candidate within a Tenant.
/// Contains PII, preventing fragmentation across multiple applications.
/// </summary>
public sealed class CandidateProfile : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    
    // Future-proofing: links to internal employee if converted or internal mobility
    public string? InternalEmployeeId { get; private set; }
    
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastInteractionUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // Tracks right to be forgotten or PII lockdown
    public bool IsAnonymized { get; private set; }

    private CandidateProfile() { }

    public static CandidateProfile Register(
        string tenantId,
        string firstName,
        string lastName,
        string email,
        string? phone = null,
        string? internalEmployeeId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required for identity resolution.");

        return new CandidateProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            PhoneNumber = phone,
            InternalEmployeeId = internalEmployeeId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastInteractionUtc = DateTimeOffset.UtcNow
        };
    }

    public void LogInteraction()
    {
        LastInteractionUtc = DateTimeOffset.UtcNow;
    }

    public void Anonymize()
    {
        FirstName = "Anonymized";
        LastName = "Candidate";
        Email = $"{Id}@anonymized.local";
        PhoneNumber = null;
        IsAnonymized = true;
    }
}
