// -----------------------------------------------------------------------
// <copyright file="PersistentSecurityAuditService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Identity.Domain;
using Karamchari.Identity.Infrastructure.Persistence;
using Karamchari.Identity.Services;
using Microsoft.Extensions.Logging;

namespace Karamchari.Identity.Infrastructure.Security;

/// <summary>
/// Implementation of ISecurityAuditService that persists audit entries to the database.
/// Also logs events to the standard logging infrastructure for real-time observability.
/// </summary>
public sealed class PersistentSecurityAuditService : ISecurityAuditService
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<PersistentSecurityAuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentSecurityAuditService"/> class.
    /// </summary>
    /// <param name="db">The identity DB context.</param>
    /// <param name="logger">The logger.</param>
    public PersistentSecurityAuditService(IdentityDbContext db, ILogger<PersistentSecurityAuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void LogAuthSuccess(string email, Guid userId, string tenantId, string? deviceInfo = null)
    {
        var details = $"Email: {email}, Device: {deviceInfo ?? "Unknown"}";
        var entry = SecurityAuditEntry.Create("AUTH_SUCCESS", userId.ToString(), tenantId, null, details);

        PersistAndLog(entry, LogLevel.Information);
    }

    /// <inheritdoc/>
    public void LogAuthFailure(string email, string reason, string? tenantId = null, string? ip = null)
    {
        var details = $"Email: {email}, Reason: {reason}";
        var entry = SecurityAuditEntry.Create("AUTH_FAILURE", null, tenantId, ip, details, false);

        PersistAndLog(entry, LogLevel.Warning);
    }

    /// <inheritdoc/>
    public void LogTokenRevocation(string jti, string reason, string userId)
    {
        var details = $"JTI: {jti}, Reason: {reason}";
        var entry = SecurityAuditEntry.Create("TOKEN_REVOKED", userId, null, null, details);

        PersistAndLog(entry, LogLevel.Information);
    }

    /// <inheritdoc/>
    public void LogSuspiciousActivity(string userId, string activity, string details)
    {
        var entry = SecurityAuditEntry.Create($"SUSPICIOUS_{activity}", userId, null, null, details, false);

        PersistAndLog(entry, LogLevel.Critical);
    }

    private void PersistAndLog(SecurityAuditEntry entry, LogLevel level)
    {
        _logger.Log(level, "{EventName}: {Details} (User: {UserId}, Tenant: {TenantId})",
            entry.EventName, entry.Details, entry.UserId ?? "N/A", entry.TenantId ?? "N/A");

        _db.SecurityAuditEntries.Add(entry);

        // Note: In a high-traffic system, we might want to batch these or use a background worker.
        // For Karamchari's current scale and deterministic requirements, synchronous save is acceptable
        // within the identity context which is already performing DB operations for tokens.
        _db.SaveChanges();
    }
}
