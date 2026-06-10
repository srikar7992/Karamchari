// -----------------------------------------------------------------------
// <copyright file="EndpointCatalogEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.EndpointRegistry;

/// <summary>
/// One mapped endpoint as recorded in the endpoint catalog: route, owning module,
/// authorization posture, and request/response shapes derived from endpoint metadata.
/// </summary>
/// <param name="Route">The route pattern (raw text), e.g. <c>/api/v1/assets/{assetId:guid}</c>.</param>
/// <param name="HttpMethods">HTTP methods accepted by the endpoint; empty when the endpoint matches any method (e.g. SignalR hubs).</param>
/// <param name="Module">Owning module derived from the handler's declaring type, e.g. <c>AssetManagement</c>.</param>
/// <param name="Name">The endpoint name from <c>WithName(...)</c>, when present.</param>
/// <param name="RequiresAuthorization">Whether the endpoint carries authorization metadata.</param>
/// <param name="AuthorizationPolicies">Named authorization policies; contains <c>(default)</c> for policy-less <c>RequireAuthorization()</c>.</param>
/// <param name="AllowsAnonymous">Whether the endpoint explicitly allows anonymous access.</param>
/// <param name="RequestType">Request body CLR type name, when the endpoint accepts a body.</param>
/// <param name="ResponseType">Declared response CLR type name, when response metadata is present.</param>
public sealed record EndpointCatalogEntry(
    string Route,
    IReadOnlyList<string> HttpMethods,
    string Module,
    string? Name,
    bool RequiresAuthorization,
    IReadOnlyList<string> AuthorizationPolicies,
    bool AllowsAnonymous,
    string? RequestType,
    string? ResponseType);
