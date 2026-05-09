using Karamchari.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext for identity and authentication data.
/// </summary>
public sealed class IdentityDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    /// <summary>Initializes a new instance of the <see cref="IdentityDbContext"/> class.</summary>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    /// <summary>Configures the identity model.</summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");

        builder.Entity<RefreshToken>(rt =>
        {
            rt.ToTable("RefreshTokens");
            rt.HasKey(x => x.Id);
            rt.HasIndex(x => x.TokenHash).IsUnique();
            rt.Property(x => x.TokenHash).IsRequired().HasMaxLength(256);
            rt.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            rt.HasIndex(x => x.UserId);
        });

        builder.Entity<RevokedToken>(rt =>
        {
            rt.ToTable("RevokedTokens");
            rt.HasKey(x => x.Jti);
            rt.Property(x => x.Jti).HasMaxLength(64);
        });
    }
}
