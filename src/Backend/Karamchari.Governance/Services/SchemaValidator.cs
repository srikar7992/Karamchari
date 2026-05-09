using System.Text.Json;
using Karamchari.Governance.Domain.Contracts;
using Karamchari.Governance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Governance.Services;

internal sealed class SchemaValidator : ISchemaValidator
{
    private readonly GovernanceDbContext _db;
    private readonly ILogger<SchemaValidator> _logger;

    public SchemaValidator(GovernanceDbContext db, ILogger<SchemaValidator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SchemaValidationResult> ValidateAsync(string tenantId, string contractName, string version, string jsonPayload, CancellationToken ct = default)
    {
        var schemaDef = await _db.SchemaDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.ContractName == contractName && s.Version == version, ct);

        if (schemaDef == null)
        {
            _logger.LogWarning("SCHEMA_MISSING: No registered schema found for {ContractName} v{Version} in tenant {TenantId}.", contractName, version, tenantId);
            return new SchemaValidationResult(false, new[] { $"Unregistered contract: {contractName} v{version}" });
        }

        if (schemaDef.Status is SchemaStatus.Deprecated or SchemaStatus.Archived)
        {
            _logger.LogWarning("SCHEMA_DEPRECATED: Usage of {Status} schema {ContractName} v{Version}.", schemaDef.Status, contractName, version);
            // In a strict mode, this could return IsValid = false. For now, we log and allow.
        }

        try
        {
            // Placeholder for actual JSON Schema evaluation (e.g., using NJsonSchema).
            // For now, we just verify it's valid JSON.
            using var document = JsonDocument.Parse(jsonPayload);
            return new SchemaValidationResult(true, Array.Empty<string>());
        }
        catch (JsonException ex)
        {
            return new SchemaValidationResult(false, new[] { $"Invalid JSON payload: {ex.Message}" });
        }
    }
}
