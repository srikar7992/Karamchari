using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Karamchari.Identity.Infrastructure.Configuration;
using Karamchari.Identity.Infrastructure.Persistence;
using Karamchari.Identity.Infrastructure.Security;
using Karamchari.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped, ServiceLifetime.Singleton);

        services.AddDbContextFactory<IdentityDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("KaramchariDb")
                ?? throw new InvalidOperationException("ConnectionStrings:KaramchariDb must be configured.");
            options.UseSqlServer(connectionString);
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
        services.AddSingleton<DatabaseSigningKeyResolver>();
        services.AddMemoryCache();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
        if (jwtOptions != null)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var resolver = services.BuildServiceProvider().GetRequiredService<DatabaseSigningKeyResolver>();
                        return resolver.GetValidationKeysAsync().GetAwaiter().GetResult();
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
            });
        }

        return services;
    }
}
