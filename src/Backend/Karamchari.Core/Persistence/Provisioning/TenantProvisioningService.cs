namespace Karamchari.Core.Persistence.Provisioning;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Service responsible for the physical provisioning of a new tenant's database infrastructure.
/// Handles schema creation, table cloning, and Row-Level Security policy application.
/// </summary>
public sealed class TenantProvisioningService
{
    private readonly DbContext _dbContext;
    private readonly ITenantTableRegistry _tableRegistry;
    private readonly RlsScriptGenerator _rlsGenerator;
    private readonly ILogger<TenantProvisioningService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisioningService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used for administrative DDL operations.</param>
    /// <param name="tableRegistry">The registry of all tenant-owned tables across all modules.</param>
    /// <param name="rlsGenerator">The generator for RLS policy scripts.</param>
    /// <param name="logger">The logger.</param>
    public TenantProvisioningService(
        DbContext dbContext,
        ITenantTableRegistry tableRegistry,
        RlsScriptGenerator rlsGenerator,
        ILogger<TenantProvisioningService> logger)
    {
        _dbContext = dbContext;
        _tableRegistry = tableRegistry;
        _rlsGenerator = rlsGenerator;
        _logger = logger;
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
            _logger.LogInformation("Starting physical provisioning for tenant {TenantId} (Schema: {SchemaName})", tenantId, schemaName);
        }

        // 1. Create the Schema
        // We use dynamic SQL because schema names cannot be parameterized. 
        // Boundary validation is performed by the caller and within RlsScriptGenerator.
#pragma warning disable EF1002 // SQL injection: schemaName is validated at the API boundary
        await _dbContext.Database.ExecuteSqlRawAsync($"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schemaName}') EXEC('CREATE SCHEMA [{schemaName}]')");
#pragma warning restore EF1002

        // 2. Clone Table Structures
        // In this architecture, 'dbo' serves as the template schema. 
        // We clone every registered tenant table into the new schema.
        foreach (var table in _tableRegistry.Tables)
        {
            var checkExists = $"SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = '{schemaName}' AND t.name = '{table.TableName}'";
            var cloneSql = $"SELECT * INTO [{schemaName}].[{table.TableName}] FROM [dbo].[{table.TableName}] WHERE 1=0";
            
            // Note: In a production app, we would use a more robust migration tool to copy indexes and constraints.
#pragma warning disable EF1002
            await _dbContext.Database.ExecuteSqlRawAsync($"IF NOT EXISTS ({checkExists}) {cloneSql}");
#pragma warning restore EF1002
        }

        // 3. Apply RLS Policies
        // This ensures the new schema is immediately protected by the centralized security function.
        var tenantContext = new TenantContext(tenantId, TenantSource.Provisioning);
        var rlsScript = _rlsGenerator.BuildTenantPolicyScript(tenantContext);
        
        await _dbContext.Database.ExecuteSqlRawAsync(rlsScript);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Physical provisioning complete for tenant {TenantId}", tenantId);
        }
    }
}
