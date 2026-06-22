using System.Text.Json;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Extensibility.Domain;

public sealed class CustomEntityRecord : AggregateRoot<Guid>, ITenantOwned
{
    private CustomEntityRecord() { }
    private CustomEntityRecord(Guid id, string tenantId, Guid definitionId,
        Guid? ownerEntityId, string payload) : base(id)
    {
        TenantId = tenantId;
        DefinitionId = definitionId;
        OwnerEntityId = ownerEntityId;
        Payload = payload;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid DefinitionId { get; private set; }
    public Guid? OwnerEntityId { get; private set; }
    public string Payload { get; private set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private const int MaxPayloadBytes = 65_536; // 64 KB

    public static CustomEntityRecord Create(string tenantId, Guid definitionId,
        Guid? ownerEntityId, Dictionary<string, object?> fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var payload = JsonSerializer.Serialize(fields);
        if (System.Text.Encoding.UTF8.GetByteCount(payload) > MaxPayloadBytes)
            throw new InvalidOperationException($"Payload exceeds maximum size of {MaxPayloadBytes / 1024} KB.");
        return new CustomEntityRecord(Guid.NewGuid(), tenantId, definitionId, ownerEntityId, payload);
    }

    public void Update(Dictionary<string, object?> fields)
    {
        var payload = JsonSerializer.Serialize(fields);
        if (System.Text.Encoding.UTF8.GetByteCount(payload) > MaxPayloadBytes)
            throw new InvalidOperationException($"Payload exceeds maximum size of {MaxPayloadBytes / 1024} KB.");
        Payload = payload;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Dictionary<string, object?> GetFields() =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(Payload) ?? [];
}
