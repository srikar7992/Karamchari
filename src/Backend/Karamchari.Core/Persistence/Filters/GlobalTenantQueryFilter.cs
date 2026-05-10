using System.Globalization;
using System.Linq.Expressions;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Filters;

public static class GlobalTenantQueryFilter
{
    private static readonly ILogger _logger = LoggerFactory.Create(builder => { }).CreateLogger("GlobalTenantQueryFilter");

    public static void ApplyGlobalFilters(ModelBuilder modelBuilder, string currentTenantId)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTenantId);

        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.ClrType.GetInterfaces().Any(i => i == typeof(ITenantOwned)))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var filterExpression = BuildFilterExpression(entityType.ClrType, currentTenantId);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filterExpression);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Applied global tenant query filter to {EntityType} for TenantId={TenantId}",
                    entityType.ClrType.Name,
                    currentTenantId);
            }
        }
    }

    public static bool IsEntityTenantFiltered(Type entityType, IModel model)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(model);

        var entityTypeBase = model.FindEntityType(entityType);
        if (entityTypeBase is null)
        {
            return false;
        }

        return entityTypeBase.ClrType.GetInterfaces().Any(i => i == typeof(ITenantOwned));
    }

    public static IEnumerable<Type> GetTenantFilteredEntities(IModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.GetEntityTypes()
            .Where(e => e.ClrType.GetInterfaces().Any(i => i == typeof(ITenantOwned)))
            .Select(e => e.ClrType);
    }

    public static void RemoveGlobalFilters(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.ClrType.GetInterfaces().Any(i => i == typeof(ITenantOwned)))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(null);
        }
    }

    private static LambdaExpression BuildFilterExpression(Type entityType, string tenantId)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var tenantIdProperty = Expression.Property(parameter, nameof(ITenantOwned.TenantId));
        var tenantIdConstant = Expression.Constant(tenantId, typeof(string));

        var equalityExpression = Expression.Equal(tenantIdProperty, tenantIdConstant);

        return Expression.Lambda(equalityExpression, parameter);
    }
}
