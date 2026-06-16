// -----------------------------------------------------------------------
// <copyright file="DevPortalCors.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.Cors;

/// <summary>
/// Development-only CORS for the Next.js portal at <c>http://localhost:3000</c>.
///
/// Production traffic is fronted by APIM at the same origin as the SPA, so we
/// don't need a production CORS policy at all. If we ever do, that policy must
/// be a separate code path with an explicit allowlist read from configuration â€”
/// never enable a permissive policy in production.
///
/// Notes:
/// - We do NOT enable credentials. Authentication is via headers
///   (X-Tenant-Id + X-Karamchari-Gateway in dev, JWT bearer in prod). Sending
///   cookies would break preflight when paired with wildcard origins and adds
///   no value.
/// - Header allowlist explicitly includes the two tenant headers; preflight
///   responses must echo them or the browser will reject the actual request.
/// </summary>
public static class DevPortalCors
{
    /// <summary>
    /// Named CORS policy used by the development portal.
    /// </summary>
    public const string PolicyName = "KaramchariDevPortal";

    /// <summary>
    /// Registers the development-only portal CORS policy.
    /// </summary>
    public static IServiceCollection AddKaramchariDevCors(this IServiceCollection services) =>
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:3000", "http://127.0.0.1:3000",
                        "http://localhost:5191", "http://127.0.0.1:5191")
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                    .WithHeaders(
                        "Content-Type",
                        "Accept",
                        "Authorization",
                        "X-Tenant-Id",
                        "X-Karamchari-Gateway")
                    .WithExposedHeaders("Content-Type", "Content-Length");
                // Intentionally NO .AllowCredentials() â€” we authenticate via headers, not cookies.
            });
        });

    /// <summary>
    /// Applies the development portal CORS policy to the request pipeline.
    /// </summary>
    public static IApplicationBuilder UseKaramchariDevCors(this IApplicationBuilder app) =>
        app.UseCors(PolicyName);
}
