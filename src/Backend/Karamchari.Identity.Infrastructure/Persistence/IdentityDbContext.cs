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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<SecurityAuditEntry> SecurityAuditEntries => Set<SecurityAuditEntry>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<SigningKeyMetadata> SigningKeys => Set<SigningKeyMetadata>();

    /// <summary>Configures the identity model.</summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");

        builder.Entity<SigningKeyMetadata>(sk =>
        {
            sk.ToTable("SigningKeys");
            sk.HasKey(x => x.KeyId);
            sk.Property(x => x.KeyId).HasMaxLength(64);
            sk.Property(x => x.Secret).IsRequired();
            sk.HasIndex(x => x.ActivatedAt);
        });

        builder.Entity<SecurityAuditEntry>(sae =>
        {
            sae.ToTable("SecurityAuditEntries");
            sae.HasKey(x => x.Id);
            sae.Property(x => x.EventName).IsRequired().HasMaxLength(128);
            sae.Property(x => x.UserId).HasMaxLength(64);
            sae.Property(x => x.TenantId).HasMaxLength(64);
            sae.Property(x => x.IpAddress).HasMaxLength(64);
            sae.Property(x => x.Details).IsRequired();
            sae.HasIndex(x => x.TenantId);
            sae.HasIndex(x => x.UserId);
            sae.HasIndex(x => x.TimestampUtc);
        });

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
