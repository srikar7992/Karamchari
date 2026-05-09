namespace Karamchari.TimeAttendance.Tests;

using Xunit;

/// <summary>
/// Verifies that the TimeAttendance test assembly is discoverable by the enterprise test gate.
/// </summary>
public sealed class TimeAttendanceAssemblyTests
{
    /// <summary>
    /// Ensures the project contributes at least one deterministic test to CI.
    /// </summary>
    [Fact]
    public void TestAssembly_ShouldBeDiscoverable()
    {
        Assert.Equal("Karamchari.TimeAttendance.Tests", typeof(TimeAttendanceAssemblyTests).Assembly.GetName().Name);
    }
}
