using Karamchari.Identity.Contracts;
using Karamchari.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Karamchari.Identity;

/// <summary>
/// Exposes authentication and identity management endpoints.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>Maps the identity endpoints to the web application.</summary>
    public static WebApplication MapIdentityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/identity");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);
        group.MapPost("/logout", Logout).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        UserManager<IdentityUser<Guid>> userManager)
    {
        var user = new IdentityUser<Guid> { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        // Add tenant claim
        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim(Karamchari.Core.Multitenancy.TenantConstants.JwtClaimType, request.TenantId));

        return Results.Ok(new { UserId = user.Id });
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        HttpContext context,
        UserManager<IdentityUser<Guid>> userManager,
        IJwtTokenService jwtService,
        IRefreshTokenService refreshService,
        ISecurityAuditService auditService)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        var ip = context.Connection.RemoteIpAddress?.ToString();

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            auditService.LogAuthFailure(request.Email, "Invalid credentials", null, ip);
            return Results.Unauthorized();
        }

        var claims = await userManager.GetClaimsAsync(user);
        var tenantId = claims.FirstOrDefault(c => c.Type == Karamchari.Core.Multitenancy.TenantConstants.JwtClaimType)?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            auditService.LogAuthFailure(request.Email, "Missing tenant assignment", null, ip);
            return Results.Problem("User has no associated tenant.", statusCode: 403);
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(tenantId, user.Id, user.Email!, roles, []);

        var deviceInfo = context.Request.Headers.UserAgent.ToString();
        var (_, refreshToken) = await refreshService.CreateTokenAsync(user.Id, tenantId, deviceInfo);

        auditService.LogAuthSuccess(user.Email!, user.Id, tenantId, deviceInfo);

        return Results.Ok(new LoginResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(60)));
    }

    private static async Task<IResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        HttpContext context,
        UserManager<IdentityUser<Guid>> userManager,
        IJwtTokenService jwtService,
        IRefreshTokenService refreshService)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var (token, newRaw) = await refreshService.RotateTokenAsync(request.RefreshToken, ipAddress);

        if (token == null || newRaw == null)
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user == null) return Results.Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(token.TenantId, user.Id, user.Email!, roles, []);

        return Results.Ok(new LoginResponse(accessToken, newRaw, DateTime.UtcNow.AddMinutes(60)));
    }

    private static async Task<IResult> Logout(
        [FromBody] RefreshTokenRequest request,
        HttpContext context,
        IRefreshTokenService refreshService)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        await refreshService.RevokeTokenAsync(request.RefreshToken, ipAddress);
        return Results.NoContent();
    }
}
