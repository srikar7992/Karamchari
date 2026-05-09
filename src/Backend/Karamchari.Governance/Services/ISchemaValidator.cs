namespace Karamchari.Governance.Services;

/// <summary>
/// Result of a schema validation check.
/// </summary>
public sealed record SchemaValidationResult(bool IsValid, IReadOnlyCollection<string> Errors);

/// <summary>
/// Executable architecture service for contract governance.
/// Validates JSON payloads against the registered SchemaDefinition.
/// </summary>
public interface ISchemaValidator
{
    /// <inheritdoc/>
    Task<SchemaValidationResult> ValidateAsync(string tenantId, string contractName, string version, string jsonPayload, CancellationToken ct = default);
}
