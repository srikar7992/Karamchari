using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Karamchari.Identity.Infrastructure.Configuration;
using Karamchari.Identity.Infrastructure.Persistence;
using Karamchari.Identity.Infrastructure.Security;
using Karamchari.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Karamchari.Identity.Infrastructure;

/// <summary>
/// Service collection extensions for registering Identity services.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>Adds the Karamchari Identity system to the service collection.</summary>
    public static IServiceCollection AddKaramchariIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<IdentityDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("KaramchariDb")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:KaramchariDb must be configured before IdentityDbContext can be resolved.");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        }, ServiceLifetime.Scoped, ServiceLifetime.Singleton);

        services.AddDbContextFactory<IdentityDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("KaramchariDb")
                ?? throw new InvalidOperationException("ConnectionStrings:KaramchariDb must be configured.");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        }, ServiceLifetime.Singleton);

        services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        services.AddScoped<ISecurityAuditService, PersistentSecurityAuditService>();
        services.AddScoped<Karamchari.Identity.Services.IPermissionResolver, Karamchari.Identity.Services.PermissionResolver>();
        services.AddSingleton<DatabaseSigningKeyResolver>();
        services.AddMemoryCache();

        // JWT bearer is configured via a singleton IPostConfigureOptions implementation
        // (ConfigureJwtBearerOptions). PostConfigure runs AFTER the framework's automatic
        // binding of the "Authentication:Schemes:Bearer" config section, so our validation
        // parameters always win. It also captures the singleton DatabaseSigningKeyResolver
        // exactly once, removing the previous per-request services.BuildServiceProvider()
        // anti-pattern that built a brand-new DI container on every token validation.
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        // Default ALL flows to JWT bearer. AddIdentity() above registers the Identity
        // application cookie and sets DefaultScheme = ApplicationScheme; for a token API
        // we must explicitly pin authenticate/challenge/forbid to Bearer so anonymous
        // requests return 401 (not a 302 redirect to a login page).
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        // Belt-and-suspenders: if the Identity application cookie is ever challenged on an
        // API path, return 401/403 instead of redirecting to /Account/Login.
        services.ConfigureApplicationCookie(cookie =>
        {
            cookie.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            cookie.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        return services;
    }
}

/// <summary>
/// Configures the JWT bearer handler. Implemented as a singleton options configurator so the
/// singleton <see cref="DatabaseSigningKeyResolver"/> is captured once at startup rather than
/// rebuilding a DI container on every token validation.
/// </summary>
internal sealed class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly DatabaseSigningKeyResolver _resolver;
    private readonly JwtOptions _jwt;

    /// <summary>Initializes a new instance of the <see cref="ConfigureJwtBearerOptions"/> class.</summary>
    public ConfigureJwtBearerOptions(DatabaseSigningKeyResolver resolver, IOptions<JwtOptions> jwt)
    {
        _resolver = resolver;
        _jwt = jwt.Value;
    }

    /// <summary>Post-configures the named JwtBearer options (runs after framework config binding).</summary>
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        Configure(options);
    }

    /// <summary>Applies the Karamchari JWT validation parameters and events.</summary>
    private void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var keys = new List<SecurityKey>();

                // Primary source: signing keys persisted in the database (supports rotation).
                try
                {
                    keys.AddRange(_resolver.GetValidationKeysAsync().GetAwaiter().GetResult());
                }
                catch
                {
                    // DB unavailable / not yet seeded — fall back to configured keys below.
                }

                // Fallback / dev source: configured secret + named signing keys.
                if (!string.IsNullOrEmpty(_jwt.Secret))
                {
                    keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)) { KeyId = _jwt.ActiveKeyId });
                }

                foreach (var kv in _jwt.SigningKeys)
                {
                    keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(kv.Value)) { KeyId = kv.Key });
                }

                return keys;
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti) || await blacklist.IsRevokedAsync(jti))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    }
}
