// -----------------------------------------------------------------------
// <copyright file="EndpointCatalogBuilder.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Karamchari.Api.EndpointRegistry;

/// <summary>
/// Builds the endpoint catalog by reflecting over the endpoint metadata of every
/// mapped <see cref="RouteEndpoint"/>. There is no hand-maintained list: the catalog
/// is always derived from what the application actually mapped at startup.
/// </summary>
public static class EndpointCatalogBuilder
{
    private const string BffNamespacePrefix = "Karamchari.Api.BFF.";
    private const string RootNamespacePrefix = "Karamchari.";

    /// <summary>
    /// Produces one catalog entry per mapped route endpoint, ordered by route then method.
    /// </summary>
    /// <param name="dataSources">The endpoint data sources of the running application.</param>
    /// <returns>The catalog entries.</returns>
    public static IReadOnlyList<EndpointCatalogEntry> Build(IEnumerable<EndpointDataSource> dataSources)
    {
        ArgumentNullException.ThrowIfNull(dataSources);

        return dataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(CreateEntry)
            .OrderBy(entry => entry.Route, StringComparer.Ordinal)
            .ThenBy(entry => string.Join(',', entry.HttpMethods), StringComparer.Ordinal)
            .ToList();
    }

    private static EndpointCatalogEntry CreateEntry(RouteEndpoint endpoint)
    {
        var route = endpoint.RoutePattern.RawText ?? endpoint.DisplayName ?? "(unknown)";
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];
        var name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;

        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        var policies = authorizeData
            .Select(data => string.IsNullOrEmpty(data.Policy) ? "(default)" : data.Policy)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new EndpointCatalogEntry(
            Route: route,
            HttpMethods: methods,
            Module: ResolveModule(endpoint, route),
            Name: name,
            RequiresAuthorization: authorizeData.Count > 0,
            AuthorizationPolicies: policies,
            AllowsAnonymous: endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null,
            RequestType: ResolveRequestType(endpoint),
            ResponseType: ResolveResponseType(endpoint));
    }

    private static string ResolveModule(RouteEndpoint endpoint, string route)
    {
        var declaringType = endpoint.Metadata.GetMetadata<MethodInfo>()?.DeclaringType;
        var ns = declaringType?.Namespace;

        if (ns is not null)
        {
            if (ns.StartsWith(BffNamespacePrefix, StringComparison.Ordinal))
            {
                var remainder = ns[BffNamespacePrefix.Length..];
                var separator = remainder.IndexOf('.', StringComparison.Ordinal);
                return separator < 0 ? remainder : remainder[..separator];
            }

            if (ns.StartsWith(RootNamespacePrefix, StringComparison.Ordinal))
            {
                var remainder = ns[RootNamespacePrefix.Length..];
                var separator = remainder.IndexOf('.', StringComparison.Ordinal);
                return separator < 0 ? remainder : remainder[..separator];
            }
        }

        // Endpoints without a handler MethodInfo (hubs, health checks, framework endpoints)
        // are classified by route prefix.
        if (route.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
        {
            return "Hubs";
        }

        if (route.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return "Health";
        }

        return "Framework";
    }

    private static string? ResolveRequestType(RouteEndpoint endpoint)
    {
        var accepts = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        if (accepts?.RequestType is not null)
        {
            return accepts.RequestType.Name;
        }

        var method = endpoint.Metadata.GetMetadata<MethodInfo>();
        var bodyParameter = method?.GetParameters()
            .FirstOrDefault(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);

        return bodyParameter?.ParameterType.Name;
    }

    private static string? ResolveResponseType(RouteEndpoint endpoint)
    {
        return endpoint.Metadata
            .GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .FirstOrDefault(metadata => metadata.Type is not null && metadata.Type != typeof(void))
            ?.Type?.Name;
    }
}
