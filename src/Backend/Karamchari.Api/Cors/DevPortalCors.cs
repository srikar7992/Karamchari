namespace Karamchari.Api.Cors;

/// <summary>
/// Development-only CORS for the Next.js portal at <c>http://localhost:3000</c>.
///
/// Production traffic is fronted by APIM at the same origin as the SPA, so we
/// don't need a production CORS policy at all. If we ever do, that policy must
/// be a separate code path with an explicit allowlist read from configuration —
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
    public const string PolicyName = "KaramchariDevPortal";

    public static IServiceCollection AddKaramchariDevCors(this IServiceCollection services) =>
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                    .WithHeaders(
                        "Content-Type",
                        "Accept",
                        "X-Tenant-Id",
                        "X-Karamchari-Gateway")
                    .WithExposedHeaders("Content-Type", "Content-Length");
                // Intentionally NO .AllowCredentials() — we authenticate via headers, not cookies.
            });
        });

    public static IApplicationBuilder UseKaramchariDevCors(this IApplicationBuilder app) =>
        app.UseCors(PolicyName);
}
