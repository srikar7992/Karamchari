using System.Collections.Concurrent;

namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Default thread-safe implementation of <see cref="ITenantTableRegistry"/>.
/// Registered as a singleton — bounded contexts populate it during DI setup
/// and the provisioning service reads it at runtime.
/// </summary>
public sealed class TenantTableRegistry : ITenantTableRegistry
{
    private readonly ConcurrentDictionary<string, TenantTable> _tables = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<TenantTable> Tables => [.. _tables.Values];

    public void Register(TenantTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (!_tables.TryAdd(table.TableName, table))
        {
            throw new InvalidOperationException(
                $"Tenant table '{table.TableName}' is already registered. " +
                "RLS coverage must be unique per table — check for duplicate AddKaramchari{Context} calls.");
        }
    }
}
