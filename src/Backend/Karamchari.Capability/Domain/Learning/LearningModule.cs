using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Learning;

/// <summary>
/// Aggregate root representing a structured learning course or assessment.
/// Can link to external SCORM/LMS systems.
/// </summary>
public sealed class LearningModule : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int EstimatedMinutes { get; private set; }
    
    public string? ExternalProviderId { get; private set; } // e.g. LinkedIn Learning Course ID
    public string? ContentUrl { get; private set; }
    
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private LearningModule() { }

    public static LearningModule Create(
        string tenantId, 
        string title, 
        string desc, 
        int minutes, 
        string? providerId = null, 
        string? url = null)
    {
        return new LearningModule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Description = desc,
            EstimatedMinutes = minutes,
            ExternalProviderId = providerId,
            ContentUrl = url,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
