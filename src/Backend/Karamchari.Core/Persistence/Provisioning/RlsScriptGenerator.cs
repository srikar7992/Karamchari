using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Loads the RLS SQL templates embedded in <c>Karamchari.Core</c> and produces
/// concrete, per-tenant scripts ready for execution by the tenant provisioning
/// service.
///
/// All identifier inputs (tenant id, schema name, table names) are re-validated
/// at the SQL boundary even though they come from validated types Ã¢â‚¬â€ defence in
/// depth, since we're about to embed them in DDL.
/// </summary>
public sealed partial class RlsScriptGenerator
{
    private const string SecuritySchemaResource = "Karamchari.Core.Persistence.Sql.Rls.00_security_schema.sql";
    private const string PredicateFunctionResource = "Karamchari.Core.Persistence.Sql.Rls.01_predicate_function.sql";
    private const string TenantPolicyTemplateResource = "Karamchari.Core.Persistence.Sql.Rls.02_tenant_policy.template.sql";

    [GeneratedRegex(@"^tenant_[a-z0-9_]{1,64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex SchemaNameRegex();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9_-]{0,49}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex TenantIdRegex();

    private readonly ITenantModelDiscoveryService _discoveryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RlsScriptGenerator"/> class.
    /// </summary>
    /// <param name="discoveryService">The tenant model discovery service.</param>
    public RlsScriptGenerator(ITenantModelDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    /// <summary>
    /// One-time database bootstrap: creates the <c>security</c> schema and the
    /// shared <c>fn_tenant_access</c> predicate function. Idempotent Ã¢â‚¬â€ safe to
    /// re-run on every deploy.
    /// </summary>
    public static IReadOnlyList<string> BuildBootstrapScripts() =>
    [
        ReadEmbeddedResource(SecuritySchemaResource),
        ReadEmbeddedResource(PredicateFunctionResource),
    ];

    /// <summary>
    /// Per-tenant policy script. Run after the tenant's schema and tables exist.
    /// Returns a single batch string containing the full policy DDL with all
    /// registered tables' FILTER + BLOCK predicates.
    /// </summary>
    /// <param name="tenant">The tenant execution envelope whose policy should be generated.</param>
    public string BuildTenantPolicyScript(TenantExecutionEnvelope tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        // Re-validate at the SQL boundary.
        if (!TenantIdRegex().IsMatch(tenant.TenantId))
        {
            throw new InvalidOperationException(
                $"Tenant id '{tenant.TenantId}' failed boundary validation; refusing to emit DDL.");
        }

        if (!SchemaNameRegex().IsMatch(tenant.SchemaName))
        {
            throw new InvalidOperationException(
                $"Tenant schema '{tenant.SchemaName}' failed boundary validation; refusing to emit DDL.");
        }

        var artifacts = _discoveryService.Discover();
        if (artifacts.Count == 0)
        {
            throw new InvalidOperationException(
                "No tenant artifacts discovered. Ensure DbContexts are registered.");
        }

        var policyName = $"TenantPolicy_{tenant.TenantId.Replace('-', '_')}";
        var template = ReadEmbeddedResource(TenantPolicyTemplateResource);
        var tableList = BuildTableList(tenant.SchemaName, artifacts);

        var dropScript = $"IF EXISTS (SELECT * FROM sys.security_policies WHERE name = '{policyName}' AND schema_id = SCHEMA_ID('security')) DROP SECURITY POLICY [security].[{policyName}]";

        var script = template
            .Replace("{{TableList}}", tableList, StringComparison.Ordinal)
            .Replace("{{PolicyName}}", policyName, StringComparison.Ordinal);

        return $"{dropScript}\nGO\n{script}";
    }

    private static string BuildTableList(string schemaName, IReadOnlyCollection<TenantRelationalArtifact> artifacts)
    {
        // Each table contributes a set of ADD predicates. We use dynamic SQL within
        // the script to check if the TenantId column exists before applying RLS,
        // allowing us to handle tables that are isolated by schema but lack
        // a local TenantId column (e.g. some owned collections).
        var sb = new StringBuilder();
        const string tenantIdColumn = "TenantId";

        foreach (var artifact in artifacts)
        {
            // Re-validate names at the SQL boundary.
            if (!TenantTable.TableNameIsValid(artifact.TableName))
            {
                throw new InvalidOperationException(
                    $"Discovered table name '{artifact.TableName}' failed boundary validation; refusing to emit DDL.");
            }

            var fullTableName = $"[{schemaName}].[{artifact.TableName}]";
            sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('{fullTableName}') AND name = '{tenantIdColumn}')");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"    EXEC('ALTER SECURITY POLICY [security].[{{{{PolicyName}}}}] ADD FILTER PREDICATE [security].[fn_tenant_access]([{tenantIdColumn}]) ON {fullTableName},");
            sb.AppendLine($"        ADD BLOCK  PREDICATE [security].[fn_tenant_access]([{tenantIdColumn}]) ON {fullTableName} AFTER INSERT,");
            sb.AppendLine($"        ADD BLOCK  PREDICATE [security].[fn_tenant_access]([{tenantIdColumn}]) ON {fullTableName} BEFORE UPDATE,");
            sb.AppendLine($"        ADD BLOCK  PREDICATE [security].[fn_tenant_access]([{tenantIdColumn}]) ON {fullTableName} AFTER UPDATE,");
            sb.AppendLine($"        ADD BLOCK  PREDICATE [security].[fn_tenant_access]([{tenantIdColumn}]) ON {fullTableName} BEFORE DELETE');");
            sb.AppendLine("END");
        }

        return sb.ToString();
    }

    private static void AppendPredicate(StringBuilder sb, ref bool first, string kind, string targetClause, string column)
    {
        if (!first)
        {
            sb.Append(',').AppendLine();
        }

        sb.Append("    ADD ").Append(kind).Append(" PREDICATE [security].[fn_tenant_access]([")
          .Append(column).Append("]) ").Append(targetClause);

        first = false;
    }

    private static string ReadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(RlsScriptGenerator).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded SQL resource '{resourceName}' not found. " +
                "Check that Karamchari.Core.csproj embeds Persistence/Sql/**/*.sql.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
