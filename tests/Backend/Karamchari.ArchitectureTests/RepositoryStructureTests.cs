using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Karamchari.ArchitectureTests;

/// <summary>
/// Enforces physical repository structure and project naming conventions.
/// Prevents repository entropy by ensuring projects are placed in approved roots.
/// </summary>
public sealed class RepositoryStructureTests
{
    private static readonly string RootPath = GetRepositoryRoot();

    private static readonly string[] ApprovedRoots = 
    {
        "src/Backend/Platform",
        "src/Backend/Modules",
        "src/Backend/Hosts",
        "tests/Backend",
        "tools",
        "docs",
        "scripts"
    };

    [Fact]
    public void NoProjectOutsideApprovedRoots()
    {
        var csprojFiles = Directory.GetFiles(RootPath, "*.csproj", SearchOption.AllDirectories);
        var violations = csprojFiles
            .Select(path => Path.GetRelativePath(RootPath, path).Replace("\\", "/"))
            // Exclude agent worktrees and plugin cache — not production code
            .Where(relPath => !relPath.StartsWith(".claude/", StringComparison.OrdinalIgnoreCase))
            .Where(relPath => !ApprovedRoots.Any(root => relPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(violations.Count == 0, $"The following projects are outside approved roots:\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void AllProjectsFollowNamespaceConvention()
    {
        var csprojFiles = Directory.GetFiles(RootPath, "*.csproj", SearchOption.AllDirectories);
        var violations = csprojFiles
            .Select(path => new { Path = path, FileName = Path.GetFileNameWithoutExtension(path) })
            .Where(x => !x.FileName.StartsWith("Karamchari.", StringComparison.Ordinal))
            // Exceptions for tools if any
            .Where(x => !x.Path.Contains("/tools/"))
            .ToList();

        Assert.True(violations.Count == 0, $"The following projects do not follow the 'Karamchari.*' naming convention:\n{string.Join("\n", violations.Select(v => v.Path))}");
    }

    private static string GetRepositoryRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (current != null && !Directory.Exists(Path.Combine(current, ".git")))
        {
            current = Path.GetDirectoryName(current);
        }
        return current ?? throw new InvalidOperationException("Could not find repository root.");
    }
}
