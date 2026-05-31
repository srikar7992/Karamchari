using System.Security.Claims;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Api.Seeding;

/// <summary>
/// Seeds documented developer login users for the local tenants so a newly onboarded
/// engineer can authenticate immediately — no manual data creation, no tribal knowledge.
///
/// Creates, for each local tenant (dev, acme, contoso, globex), four personas:
///   admin@{tenant}.local     (role: Admin)
///   manager@{tenant}.local   (role: Manager)
///   employee@{tenant}.local  (role: Employee)
///   readonly@{tenant}.local  (role: ReadOnly)
/// all with password <see cref="DefaultPassword"/> and a <c>tenant_id</c> claim.
///
/// Idempotent (skips users that already exist). Invoked only from the
/// <c>--provision-dev-tenants</c> bootstrap path, which runs in Development/Local.
/// Documented in <c>docs/audits/api-certification/AUTHENTICATION_GUIDE.md</c>.
/// </summary>
public static class DevDataSeeder
{
    /// <summary>Documented default password for every seeded developer account.</summary>
    public const string DefaultPassword = "Dev@Pass123!";

    private static readonly string[] Tenants = { "dev", "acme", "contoso", "globex" };

    private static readonly (string Prefix, string Role)[] Personas =
    {
        ("admin", Roles.Admin),
        ("manager", Roles.Manager),
        ("employee", Roles.Employee),
        ("readonly", Roles.ReadOnly),
    };

    /// <summary>Ensures the documented roles and developer users exist.</summary>
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var (_, role) in Personas)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var created = 0;
        foreach (var tenant in Tenants)
        {
            foreach (var (prefix, role) in Personas)
            {
                var email = $"{prefix}@{tenant}.local";
                if (await userManager.FindByEmailAsync(email) is not null)
                {
                    continue;
                }

                var user = new IdentityUser<Guid>
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };

                var result = await userManager.CreateAsync(user, DefaultPassword);
                if (!result.Succeeded)
                {
                    logger.LogWarning(
                        "DevDataSeeder: could not create {Email}: {Errors}",
                        email,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                await userManager.AddClaimAsync(user, new Claim(TenantConstants.JwtClaimType, tenant));
                await userManager.AddToRoleAsync(user, role);
                created++;
            }
        }

        logger.LogInformation(
            "DevDataSeeder: ensured {Total} developer users across {TenantCount} tenants ({Created} newly created). Default password is documented in AUTHENTICATION_GUIDE.md.",
            Tenants.Length * Personas.Length,
            Tenants.Length,
            created);
    }
}
