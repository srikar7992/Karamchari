using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Karamchari.Core.Persistence;

/// <summary>
/// Marker interface for the financial audit SaveChanges interceptor.
/// Modules reference this Core interface rather than the Governance concrete class,
/// eliminating the Payroll → Governance ProjectReference dependency.
/// </summary>
public interface IFinancialAuditInterceptor : ISaveChangesInterceptor { }
