namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;

/// <summary>
/// RF#1 — Concurrency: simulate parallel leave applications against a single balance.
/// Domain-layer concurrency: balance guard proves invariant holds under concurrent access.
/// Real RowVersion/DB-level concurrency requires SQL Server integration test (Testcontainers).
/// </summary>
public sealed class RF1_ConcurrencyTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Domain-layer: 20 tasks compete for balance = 1.
    /// Only 1 can succeed (guard throws for rest).
    /// Balance never goes below 0 regardless of race ordering.
    /// Note: this is single-DB-connection serialised domain test.
    /// Real concurrent SQL test requires RowVersion + Testcontainers.
    /// </summary>
    [Fact]
    public async Task ConcurrentConsume_20Tasks_Balance1_OnlyOneSucceeds()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(1m, Today);

        var lockObj = new object();
        int successCount = 0;
        int failureCount = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            lock (lockObj) // serialize access to domain object (mimics DB row lock)
            {
                try
                {
                    balance.Consume(1m, Today, $"req-{Guid.NewGuid()}");
                    successCount++;
                }
                catch (InvalidOperationException)
                {
                    failureCount++;
                }
            }
        }));

        await Task.WhenAll(tasks);

        successCount.Should().Be(1, "only one consume can succeed when balance = 1");
        failureCount.Should().Be(19, "remaining 19 are blocked by guard");
        balance.AvailableBalance.Should().Be(0m, "balance depleted exactly once");
        balance.AvailableBalance.Should().BeGreaterOrEqualTo(0m, "balance never goes negative");
    }

    [Fact]
    public async Task ConcurrentAccrue_1000Tasks_AllSucceed_InvariantHolds()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        var lockObj = new object();
        const int taskCount = 1000;

        var tasks = Enumerable.Range(0, taskCount).Select(i => Task.Run(() =>
        {
            lock (lockObj)
            {
                balance.Accrue(1m, Today);
            }
        }));

        await Task.WhenAll(tasks);

        balance.AvailableBalance.Should().Be(taskCount, "all 1000 accruals recorded");
        balance.AvailableBalance.Should().Be(balance.Entries.Sum(e => e.Quantity), "invariant holds");
    }

    [Fact]
    public async Task ConcurrentConsumeAndRestore_Mixed_InvariantAlwaysHolds()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(100m, Today);
        var lockObj = new object();
        var random = new Random(42);

        var tasks = Enumerable.Range(0, 200).Select(i => Task.Run(() =>
        {
            lock (lockObj)
            {
                if (i % 2 == 0)
                {
                    if (balance.AvailableBalance >= 1m)
                        balance.Consume(1m, Today, $"r{i}");
                }
                else
                {
                    balance.Restore(1m, Today, $"r{i}");
                }
            }
        }));

        await Task.WhenAll(tasks);

        balance.AvailableBalance.Should().Be(
            balance.Entries.Sum(e => e.Quantity),
            "ledger invariant must hold regardless of concurrent order");
    }

    /// <summary>
    /// Documents concurrency gap requiring SQL Server integration test.
    /// With RowVersion, two reads at balance=1 both see balance=1 but second SaveChanges throws.
    /// This cannot be replicated with InMemory EF provider.
    /// </summary>
    [Fact]
    public void ConcurrencyDocument_RowVersionPresent_RequiresSqlServerForRealTest()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.RowVersion.Should().NotBeNull(
            "RowVersion concurrency token must be present for SQL Server-level optimistic concurrency. " +
            "Full concurrency test requires Testcontainers.MsSql integration test.");
    }
}
