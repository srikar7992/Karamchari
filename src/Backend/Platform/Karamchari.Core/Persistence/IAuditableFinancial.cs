namespace Karamchari.Core.Persistence;

/// <summary>
/// Marker interface for EF entities that require an immutable financial audit trail.
/// The Governance module's AuditInterceptor intercepts SaveChanges and writes
/// a FinancialAuditEntry row for every Add/Modify/Delete on a marked entity.
/// </summary>
public interface IAuditableFinancial { }
