using System.Reflection;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Startup-time cache mapping MassTransit URNs to CLR types.
/// Built once at construction from all assemblies loaded into the AppDomain.
/// Eliminates per-cycle reflection scanning; safe for concurrent reads after construction.
/// </summary>
public sealed class OutboxMessageTypeRegistry
{
    private readonly Dictionary<string, Type> _cache;

    /// <summary>
    /// Initializes the registry by scanning all non-dynamic assemblies
    /// currently loaded into <see cref="AppDomain.CurrentDomain"/>.
    /// Must be constructed after all bounded-context assemblies are loaded
    /// (i.e., after the DI container is built, or as a singleton resolved on first use).
    /// </summary>
    public OutboxMessageTypeRegistry()
    {
        _cache = Build(AppDomain.CurrentDomain.GetAssemblies());
    }

    /// <summary>
    /// Resolves the primary MassTransit URN from a semicolon-delimited
    /// <c>MessageType</c> string to a CLR <see cref="Type"/>.
    /// Returns <c>null</c> when the URN is not registered.
    /// </summary>
    public Type? Resolve(string messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var primaryUrn = messageType.Split(';', StringSplitOptions.TrimEntries)[0];
        return _cache.TryGetValue(primaryUrn, out var type) ? type : null;
    }

    /// <summary>Count of registered URN-to-type mappings, for diagnostics.</summary>
    public int Count => _cache.Count;

    private static Dictionary<string, Type> Build(IEnumerable<Assembly> assemblies)
    {
        // MassTransit URN format: "urn:message:Namespace:TypeName"
        // where the type's dot-separated FQTN is split at the last namespace separator.
        const string prefix = "urn:message:";
        var result = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var asm in assemblies)
        {
            if (asm.IsDynamic) continue;

            try
            {
                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.Namespace is null) continue;
                    // urn:message:My.Namespace:TypeName
                    var urn = $"{prefix}{type.Namespace}:{type.Name}";
                    result.TryAdd(urn, type);
                }
            }
            catch (ReflectionTypeLoadException) { /* assembly partially loaded — skip */ }
            catch (NotSupportedException) { /* dynamic or unsupported assembly */ }
            catch (FileNotFoundException) { /* missing dependency — skip */ }
        }

        return result;
    }
}
