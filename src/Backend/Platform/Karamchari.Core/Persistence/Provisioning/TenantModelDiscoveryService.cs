// -----------------------------------------------------------------------
// <copyright file="TenantModelDiscoveryService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Implementation of <see cref="ITenantModelDiscoveryService"/> that uses reflection
/// to discover all <see cref="KaramchariDbContext"/> types and extract their relational metadata.
/// </summary>
public sealed class TenantModelDiscoveryService : ITenantModelDiscoveryService
{
    private readonly IServiceProvider _serviceProvider;

    public TenantModelDiscoveryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyCollection<TenantRelationalArtifact> Discover()
    {
        var artifacts = new List<TenantRelationalArtifact>();

        // We discover all DbContext types that inherit from KaramchariDbContext 
        // in the loaded assemblies. This ensures zero-manual-registration.
        var contextTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(KaramchariDbContext).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        foreach (var type in contextTypes)
        {
            // We resolve the context from the provider.
            // In a provisioning context, these will typically be resolved from a root provider.
            using var scope = _serviceProvider.CreateScope();

            // Reflection over loaded assemblies also surfaces design-time-only contexts
            // (the DesignTimeDbContext nested classes in each module's
            // *DbContextDesignTimeFactory), which derive from KaramchariDbContext but are
            // never registered in the runtime DI container. Resolve permissively and skip
            // any context that has no runtime registration — their tenant model is identical
            // to the real context they shadow, so nothing is lost.
            if (scope.ServiceProvider.GetService(type) is not DbContext context)
            {
                continue;
            }

            var model = context.Model;

            foreach (var entityType in model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                var schema = entityType.GetSchema() ?? KaramchariDbContext.PlaceholderSchema;

                if (tableName == null) continue;

                // We only care about artifacts in the tenant-scoped placeholder schema.
                // Outbox entities pinned to 'dbo' are ignored by this discovery service
                // as they are handled by standard migrations.
                if (schema != KaramchariDbContext.PlaceholderSchema) continue;

                artifacts.Add(new TenantRelationalArtifact
                {
                    DbContextName = type.Name,
                    EntityName = entityType.Name,
                    TableName = tableName,
                    Schema = schema,
                    IsOwned = entityType.IsOwned(),
                    IsOwnedCollection = entityType.IsOwned(), // Simplified: any owned table in __tenant__ schema
                    IsJoinTable = tableName.Contains('_') && !entityType.IsOwned(), // Heuristic for now
                    IsTenantScoped = true
                });
            }
        }

        return artifacts.DistinctBy(a => new { a.Schema, a.TableName }).ToList();
    }
}
