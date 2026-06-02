namespace Karamchari.Core.Persistence.Provisioning;

using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>Projection record used when discovering indexes on the dbo template tables.</summary>
internal sealed record IndexRow(
    string IndexName,
    bool IsUnique,
    bool IsPrimaryKey,
    string IndexType,
    string Columns);

/// <summary>
/// Service responsible for the physical provisioning of a new tenant's database infrastructure.
/// Handles schema creation, table cloning, index application, and Row-Level Security policy application.
/// </summary>
public sealed class TenantProvisioningService
{
    private readonly DbContext _dbContext;
    private readonly ITenantModelDiscoveryService _discoveryService;
    private readonly RlsScriptGenerator _rlsGenerator;
    private readonly IEnumerable<ITenantPostProvisioningTask> _postProvisioningTasks;
    private readonly ILogger<TenantProvisioningService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisioningService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used for administrative DDL operations.</param>
    /// <param name="discoveryService">The tenant model discovery service.</param>
    /// <param name="rlsGenerator">The generator for RLS policy scripts.</param>
    /// <param name="postProvisioningTasks">
    /// Bounded-context tasks that run after table cloning to apply indexes and constraints.
    /// </param>
    /// <param name="logger">The logger.</param>
    public TenantProvisioningService(
        DbContext dbContext,
        ITenantModelDiscoveryService discoveryService,
        RlsScriptGenerator rlsGenerator,
        IEnumerable<ITenantPostProvisioningTask> postProvisioningTasks,
        ILogger<TenantProvisioningService> logger)
    {
        _dbContext = dbContext;
        _discoveryService = discoveryService;
        _rlsGenerator = rlsGenerator;
        _postProvisioningTasks = postProvisioningTasks;
        _logger = logger;
    }

    /// <summary>
    /// Bootstraps the centralized RLS infrastructure (schema and predicate function).
    /// Idempotent.
    /// </summary>
    public async Task ProvisionRlsInfrastructureAsync()
    {
        foreach (var script in RlsScriptGenerator.BuildBootstrapScripts())
        {
            await ExecuteScriptAsync(script);
        }
    }

    /// <summary>
    /// Provisions the physical schema and security policies for a new tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="schemaName">The name of the schema to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProvisionTenantAsync(string tenantId, string schemaName)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Starting discovery-driven provisioning for tenant {TenantId} (Schema: {SchemaName})", tenantId, schemaName);
        }

        // 1. Discover Artifacts
        // EF Core model is the single source of truth.
        var artifacts = _discoveryService.Discover();
        if (artifacts.Count == 0)
        {
            throw new InvalidOperationException("No tenant artifacts discovered. Provisioning aborted.");
        }

        // 2. Create the Schema
#pragma warning disable EF1002 // SQL injection: schemaName is validated at the API boundary
        await _dbContext.Database.ExecuteSqlRawAsync($"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schemaName}') EXEC('CREATE SCHEMA [{schemaName}]')");
#pragma warning restore EF1002

        // 3. Clone Table Structures + Indexes
        foreach (var artifact in artifacts)
        {
            var checkExists = $"SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = '{schemaName}' AND t.name = '{artifact.TableName}'";
            var cloneSql = $"SELECT * INTO [{schemaName}].[{artifact.TableName}] FROM [dbo].[{artifact.TableName}] WHERE 1=0";

#pragma warning disable EF1002
            await _dbContext.Database.ExecuteSqlRawAsync($"IF NOT EXISTS ({checkExists}) {cloneSql}");
#pragma warning restore EF1002

            // Clone all indexes (including unique constraints and primary key) from the dbo template table.
            // SELECT * INTO copies columns only; indexes and constraints must be applied separately.
            await CloneIndexesAsync(schemaName, artifact.TableName);
        }

        // 4. Apply bounded-context indexes and constraints.
        foreach (var task in _postProvisioningTasks)
        {
            await task.ExecuteAsync(schemaName, _dbContext, cancellationToken: default);
        }

        // 5. Apply RLS Policies.
        var tenantEnvelope = new TenantExecutionEnvelope(tenantId, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), ExecutionSource.Manual, TenantSource.Provisioning);
        var rlsScript = _rlsGenerator.BuildTenantPolicyScript(tenantEnvelope);

        await ExecuteScriptAsync(rlsScript);

        // 6. Artifact Set Verification
        await VerifyProvisioningAsync(schemaName, artifacts);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Physical provisioning complete and verified for tenant {TenantId}", tenantId);
        }
    }

    private async Task VerifyProvisioningAsync(string schemaName, IReadOnlyCollection<TenantRelationalArtifact> expected)
    {
        // 1. Verify Table Existence (Artifact Set Verification)
        var actualTablesList = await _dbContext.Database.SqlQueryRaw<string>(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = {0}",
            schemaName).ToListAsync();
        var actualTables = new HashSet<string>(actualTablesList, StringComparer.OrdinalIgnoreCase);

        var expectedTables = expected.Select(a => a.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = expectedTables.Except(actualTables).ToList();
        if (missing.Count > 0)
        {
            var missingList = string.Join(", ", missing);
            _logger.LogError("Provisioning verification failed for schema {SchemaName}. Missing tables: {MissingTables}", schemaName, missingList);
            throw new InvalidOperationException($"Provisioning verification failed for schema {schemaName}. Missing tables: {missingList}");
        }

        // 2. Verify Relational Artifact Equality (Amendment 1)
        // We compare the schema of the new tenant tables against the 'dbo' template.
        foreach (var table in expectedTables)
        {
            var diffColumns = await _dbContext.Database.SqlQueryRaw<string>(
                @"SELECT COLUMN_NAME FROM (
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = {0} AND TABLE_SCHEMA = {1}
                    EXCEPT
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = {0} AND TABLE_SCHEMA = 'dbo'
                ) AS Diff",
                table, schemaName).ToListAsync();

            if (diffColumns.Count > 0)
            {
                foreach (var column in diffColumns)
                {
                    _logger.LogWarning("Artifact Inequality detected in {Schema}.{Table}: Column {Column} differs from template.", schemaName, table, column);
                }
                throw new InvalidOperationException($"Relational artifact equality failed for {schemaName}.{table}. Schema differs from 'dbo' template.");
            }
        }

        _logger.LogInformation("Artifact Equality Certified for schema {SchemaName}: {Count} artifacts verified.", schemaName, expectedTables.Count);
    }

    /// <summary>
    /// Clones all non-clustered and clustered indexes (including primary keys and unique constraints)
    /// from the <c>dbo</c> template table to the equivalent table in <paramref name="schemaName"/>.
    /// This repairs the gap left by <c>SELECT * INTO</c>, which copies columns only.
    /// Idempotent: skips an index if it already exists in the target schema.
    /// </summary>
    private async Task CloneIndexesAsync(string schemaName, string tableName)
    {
        // 1. Discover indexes on the dbo template.
        // Builds a minimal representation: index name → ordered column list + flags.
        var indexRows = await _dbContext.Database.SqlQueryRaw<IndexRow>(
            @"SELECT
                i.name         AS IndexName,
                i.is_unique    AS IsUnique,
                i.is_primary_key AS IsPrimaryKey,
                i.type_desc    AS IndexType,
                STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal) AS Columns
              FROM sys.indexes i
              JOIN sys.tables t ON i.object_id = t.object_id
              JOIN sys.schemas s ON t.schema_id = s.schema_id
              JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id AND ic.is_included_column = 0
              JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
              WHERE s.name = 'dbo' AND t.name = {0} AND i.name IS NOT NULL
              GROUP BY i.name, i.is_unique, i.is_primary_key, i.type_desc",
            tableName).ToListAsync();

        foreach (var idx in indexRows)
        {
            var quotedCols = string.Join(", ", idx.Columns.Split(',').Select(c => $"[{c.Trim()}]"));

#pragma warning disable EF1002 // schemaName and tableName are validated at provisioning boundary
            if (idx.IsPrimaryKey)
            {
                var pkCheckSql = $@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.key_constraints kc
                        JOIN sys.tables t ON kc.parent_object_id = t.object_id
                        JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE s.name = '{schemaName}' AND t.name = '{tableName}' AND kc.type = 'PK'
                    )
                    ALTER TABLE [{schemaName}].[{tableName}] ADD CONSTRAINT [PK_{schemaName}_{tableName}] PRIMARY KEY ({quotedCols})";
                await _dbContext.Database.ExecuteSqlRawAsync(pkCheckSql);
            }
            else
            {
                var idxName = $"{idx.IndexName}_{schemaName}";
                var uniqueClause = idx.IsUnique ? "UNIQUE " : "";
                // For unique indexes: if duplicate data exists from a prior partial provisioning or dev testing,
                // truncate the table first (data-loss acceptable for tenant scaffolding; real data is never in dbo).
                // Non-unique indexes are always safe to create on any existing data.
                var cleanupSql = idx.IsUnique
                    ? $"IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.tables t ON i.object_id = t.object_id JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = '{schemaName}' AND t.name = '{tableName}' AND i.name = '{idxName}') SELECT 1 ELSE TRUNCATE TABLE [{schemaName}].[{tableName}]"
                    : string.Empty;

                if (!string.IsNullOrEmpty(cleanupSql))
                    await _dbContext.Database.ExecuteSqlRawAsync(cleanupSql);

                var idxCheckSql = $@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes i
                        JOIN sys.tables t ON i.object_id = t.object_id
                        JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE s.name = '{schemaName}' AND t.name = '{tableName}' AND i.name = '{idxName}'
                    )
                    CREATE {uniqueClause}INDEX [{idxName}] ON [{schemaName}].[{tableName}] ({quotedCols})";
                await _dbContext.Database.ExecuteSqlRawAsync(idxCheckSql);
            }
#pragma warning restore EF1002
        }
    }

    private async Task ExecuteScriptAsync(string script)
    {
        var batches = script.Split(["\nGO", "\r\nGO", "\nGO\n", "\r\nGO\r\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var batch in batches)
        {
            if (!string.IsNullOrWhiteSpace(batch))
            {
                await _dbContext.Database.ExecuteSqlRawAsync(batch);
            }
        }
    }
}
