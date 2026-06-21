using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Extensibility.Domain;

public enum CustomFieldType { Text, Number, Date, Boolean, Lookup }

public sealed class CustomFieldDefinition : Entity<Guid>
{
    internal CustomFieldDefinition(Guid id, Guid definitionId, string fieldName,
        CustomFieldType fieldType, bool isRequired, string? defaultValue) : base(id)
    {
        DefinitionId = definitionId;
        FieldName = fieldName;
        FieldType = fieldType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }
    private CustomFieldDefinition() { }
    public Guid DefinitionId { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public CustomFieldType FieldType { get; private set; }
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
}

public sealed class CustomEntityDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<CustomFieldDefinition> _fields = [];
    private CustomEntityDefinition() { }
    private CustomEntityDefinition(Guid id, string tenantId, string entityName, string? description) : base(id)
    {
        TenantId = tenantId;
        EntityName = entityName;
        Description = description;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<CustomFieldDefinition> Fields => _fields.AsReadOnly();

    public static CustomEntityDefinition Create(string tenantId, string entityName, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        return new CustomEntityDefinition(Guid.NewGuid(), tenantId, entityName.Trim(), description);
    }

    public void AddField(string fieldName, CustomFieldType fieldType, bool isRequired = false, string? defaultValue = null)
    {
        if (_fields.Any(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Field '{fieldName}' already exists.");
        _fields.Add(new CustomFieldDefinition(Guid.NewGuid(), Id, fieldName, fieldType, isRequired, defaultValue));
    }

    public void Deactivate() => IsActive = false;
}
