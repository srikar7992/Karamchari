using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Domain.Statutory;

namespace Karamchari.Api.BFF.Common;

internal static class ProofAllowedExtensions
{
    private static readonly string[] Values = [".pdf", ".jpg", ".jpeg", ".png"];
    public static bool Contains(string ext) => Array.IndexOf(Values, ext) >= 0;
}

/// <summary>Ephemeral repository for stateless tax simulations.</summary>
public class StaticDeclarationRepository : IITDeclarationRepository
{
    private readonly IReadOnlyList<ITDeclaration> _declarations;

    public StaticDeclarationRepository(IEnumerable<ITDeclaration> declarations) =>
        _declarations = declarations.ToList();

    public Task<IReadOnlyList<ITDeclaration>> GetApprovedDeclarationsAsync(Guid employeeId, int financialYear)
        => Task.FromResult<IReadOnlyList<ITDeclaration>>(_declarations);

    public Task<ITDeclaration?> GetLatestAsync(Guid employeeId, int financialYear, string category)
        => Task.FromResult(_declarations.FirstOrDefault(d => d.Category == category));

    public Task<List<ITDeclaration>> GetPendingReviewAsync() => Task.FromResult(new List<ITDeclaration>());
    public Task<ITDeclaration?> GetByIdAsync(Guid id) => Task.FromResult<ITDeclaration?>(null);
    public Task UpsertAsync(ITDeclaration declaration) => Task.CompletedTask;
    public Task SaveChangesAsync() => Task.CompletedTask;
}
