using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.Retention;

/// <summary>
/// Defines how long records of a given type must be kept, archived, or anonymized.
/// One policy per RecordType per tenant.
/// </summary>
public sealed class RetentionPolicy : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public RetentionRecordType RecordType { get; private set; }
    public int RetentionPeriodDays { get; private set; }
    public int ArchiveAfterDays { get; private set; }
    public int DeleteAfterDays { get; private set; }
    public int AnonymizeAfterDays { get; private set; }
    public bool Enabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private RetentionPolicy() { }

    public static RetentionPolicy Create(
        string tenantId,
        RetentionRecordType recordType,
        int retentionPeriodDays,
        int archiveAfterDays,
        int deleteAfterDays,
        int anonymizeAfterDays)
    {
        return new RetentionPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecordType = recordType,
            RetentionPeriodDays = retentionPeriodDays,
            ArchiveAfterDays = archiveAfterDays,
            DeleteAfterDays = deleteAfterDays,
            AnonymizeAfterDays = anonymizeAfterDays,
            Enabled = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(int retentionPeriodDays, int archiveAfterDays, int deleteAfterDays, int anonymizeAfterDays)
    {
        RetentionPeriodDays = retentionPeriodDays;
        ArchiveAfterDays = archiveAfterDays;
        DeleteAfterDays = deleteAfterDays;
        AnonymizeAfterDays = anonymizeAfterDays;
    }

    public void Enable() => Enabled = true;
    public void Disable() => Enabled = false;
}
