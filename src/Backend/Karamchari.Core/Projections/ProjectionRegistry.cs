namespace Karamchari.Core.Projections;

internal sealed class ProjectionRegistry : IProjectionRegistry
{
    private readonly IEnumerable<IProjectionRebuilder> _rebuilders;

    public ProjectionRegistry(IEnumerable<IProjectionRebuilder> rebuilders)
    {
        _rebuilders = rebuilders;
    }

    public IReadOnlyCollection<ProjectionMetadata> GetAllMetadata()
    {
        return _rebuilders.Select(r => r.Metadata).ToList();
    }

    public IProjectionRebuilder? GetRebuilder(string projectionName)
    {
        return _rebuilders.FirstOrDefault(r => r.Metadata.ProjectionName == projectionName);
    }
}
