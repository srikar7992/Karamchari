using System.Diagnostics;
using Xunit;

namespace Karamchari.Forecasting.IntegrationTests.Infrastructure;

/// <summary>
/// Skips the test when Docker is unavailable (local machine without Docker Desktop).
/// Always runs in CI (CI=true or GITHUB_ACTIONS=true).
/// </summary>
internal sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!IsContinuousIntegration() && !IsDockerAvailable())
        {
            Skip = "Docker is required. Start Docker Desktop or run in CI.";
        }
    }

    private static bool IsContinuousIntegration() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsDockerAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info", "--format", "{{.ServerVersion}}" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            return process is not null && process.WaitForExit(5_000) && process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception) { return false; }
        catch (InvalidOperationException) { return false; }
    }
}
