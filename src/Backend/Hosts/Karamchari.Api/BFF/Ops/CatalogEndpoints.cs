using Karamchari.Core.Contracts.EventCatalog;
using Microsoft.AspNetCore.Mvc;

namespace Karamchari.Api.BFF.Ops;

/// <summary>
/// Event catalog endpoints — answer "what events exist, who owns them, who consumes them?"
/// No authentication on catalog reads so integration teams can discover the event model without
/// requiring a tenant JWT. Override with RequireAuthorization() if needed per deployment policy.
/// </summary>
public static class CatalogEndpoints
{
    public static WebApplication MapCatalogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/catalog/events");

        group.MapGet("/", ListEvents).WithName("Catalog.Events.List");
        group.MapGet("/{eventName}", GetEvent).WithName("Catalog.Events.Get");
        group.MapGet("/publisher/{publisher}", GetByPublisher).WithName("Catalog.Events.ByPublisher");
        group.MapGet("/consumer/{consumer}", GetByConsumer).WithName("Catalog.Events.ByConsumer");

        return app;
    }

    /// <summary>
    /// Lists all integration events in the platform.
    /// Supports filtering by publisher, consumer, status, and phase.
    /// </summary>
    /// <example>GET /api/v1/catalog/events?publisher=HR&amp;status=Active</example>
    private static IResult ListEvents(
        [FromQuery] string? publisher,
        [FromQuery] string? consumer,
        [FromQuery] string? status,
        [FromQuery] int? phase,
        [FromQuery] bool activeOnly = false)
    {
        var entries = KaramchariEventCatalog.All.AsEnumerable();

        if (activeOnly || string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            entries = entries.Where(e => e.Status == EventStatus.Active);
        else if (!string.IsNullOrWhiteSpace(status) &&
                 Enum.TryParse<EventStatus>(status, ignoreCase: true, out var parsedStatus))
            entries = entries.Where(e => e.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(publisher))
            entries = entries.Where(e => e.Publisher.Equals(publisher, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(consumer))
            entries = entries.Where(e => e.Consumers.Contains(consumer, StringComparer.OrdinalIgnoreCase));

        if (phase.HasValue)
            entries = entries.Where(e => e.AddedInPhase == phase.Value);

        var sorted = entries
            .OrderBy(e => e.Publisher)
            .ThenBy(e => e.EventName)
            .ThenBy(e => e.Version)
            .ToList();

        return Results.Ok(new
        {
            TotalCount = sorted.Count,
            ActiveCount = sorted.Count(e => e.Status == EventStatus.Active),
            DeprecatedCount = sorted.Count(e => e.Status == EventStatus.Deprecated),
            Events = sorted.Select(e => ToDto(e)),
        });
    }

    /// <summary>
    /// Returns the catalog entry for a specific event by name.
    /// If multiple versions exist, returns all versions ordered descending.
    /// </summary>
    private static IResult GetEvent(
        string eventName,
        [FromQuery] int? version)
    {
        if (version.HasValue)
        {
            var single = KaramchariEventCatalog.Find(eventName, version.Value);
            if (single is null) return Results.NotFound(new { error = $"Event '{eventName}' version {version} not found." });
            return Results.Ok(ToDto(single));
        }

        // Return all versions
        var entries = KaramchariEventCatalog.All
            .Where(e => e.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Version)
            .Select(e => ToDto(e))
            .ToList();

        if (entries.Count == 0)
            return Results.NotFound(new { error = $"Event '{eventName}' not found in catalog." });

        return Results.Ok(new
        {
            EventName = eventName,
            VersionCount = entries.Count,
            Versions = entries,
        });
    }

    /// <summary>All events published by the given bounded context.</summary>
    private static IResult GetByPublisher(string publisher)
    {
        var entries = KaramchariEventCatalog.GetByPublisher(publisher)
            .OrderBy(e => e.EventName)
            .ThenBy(e => e.Version)
            .Select(e => ToDto(e))
            .ToList();

        return Results.Ok(new
        {
            Publisher = publisher,
            EventCount = entries.Count,
            Events = entries,
        });
    }

    /// <summary>All events consumed by the given bounded context.</summary>
    private static IResult GetByConsumer(string consumer)
    {
        var entries = KaramchariEventCatalog.GetByConsumer(consumer)
            .OrderBy(e => e.Publisher)
            .ThenBy(e => e.EventName)
            .Select(e => ToDto(e))
            .ToList();

        return Results.Ok(new
        {
            Consumer = consumer,
            EventCount = entries.Count,
            Events = entries,
        });
    }

    private static object ToDto(EventCatalogEntry e) => new
    {
        e.EventName,
        e.FullTypeName,
        e.Version,
        e.Publisher,
        e.Consumers,
        Status = e.Status.ToString(),
        e.Description,
        e.AddedInPhase,
        e.DeprecatedInFavorOf,
    };
}
