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

    private readonly ITenantTableRegistry _tableRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="RlsScriptGenerator"/> class.
    /// </summary>
    /// <param name="tableRegistry">The table registry.</param>
    public RlsScriptGenerator(ITenantTableRegistry tableRegistry)
    {
        _tableRegistry = tableRegistry;
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
    /// <param name="tenant">The tenant whose policy should be generated.</param>
    public string BuildTenantPolicyScript(TenantContext tenant)
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

        var tables = _tableRegistry.Tables;
        if (tables.Count == 0)
        {
            throw new InvalidOperationException(
                "TenantTableRegistry is empty. Bounded contexts must register their tables before tenant provisioning.");
        }

        var policyName = $"TenantPolicy_{tenant.TenantId.Replace('-', '_')}";
        var template = ReadEmbeddedResource(TenantPolicyTemplateResource);

        return template
            .Replace("{{PolicyName}}", policyName, StringComparison.Ordinal)
            .Replace("{{TableList}}", BuildTableList(tenant.SchemaName, tables), StringComparison.Ordinal);
    }

    private static string BuildTableList(string schemaName, IReadOnlyCollection<TenantTable> tables)
    {
        // Each table contributes 5 ADD predicates: 1 FILTER + 4 BLOCK (after insert,
        // before/after update, before delete). The list is comma-separated; the trailing
        // semicolon is supplied by the template.
        var sb = new StringBuilder();
        var first = true;

        foreach (var table in tables)
        {
            // Re-validate names at the SQL boundary.
            if (!TenantTable.TableNameIsValid(table.TableName))
            {
                throw new InvalidOperationException(
                    $"Registered table name '{table.TableName}' failed boundary validation; refusing to emit DDL.");
            }

            if (!TenantTable.TableNameIsValid(table.TenantIdColumn))
            {
                throw new InvalidOperationException(
                    $"Registered TenantId column '{table.TenantIdColumn}' failed boundary validation; refusing to emit DDL.");
            }

            AppendPredicate(sb, ref first, "FILTER", "ON [" + schemaName + "].[" + table.TableName + "]", table.TenantIdColumn);
            AppendPredicate(sb, ref first, "BLOCK ", "ON [" + schemaName + "].[" + table.TableName + "] AFTER INSERT", table.TenantIdColumn);
            AppendPredicate(sb, ref first, "BLOCK ", "ON [" + schemaName + "].[" + table.TableName + "] BEFORE UPDATE", table.TenantIdColumn);
            AppendPredicate(sb, ref first, "BLOCK ", "ON [" + schemaName + "].[" + table.TableName + "] AFTER UPDATE", table.TenantIdColumn);
            AppendPredicate(sb, ref first, "BLOCK ", "ON [" + schemaName + "].[" + table.TableName + "] BEFORE DELETE", table.TenantIdColumn);
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
