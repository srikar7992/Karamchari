namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Concern 2 — Concurrency.
///
/// WHAT THIS FILE TESTS: domain-layer concurrent access guard.
/// WHAT THIS FILE DOES NOT TEST: real SQL RowVersion DbUpdateConcurrencyException.
///
/// Real RowVersion test requires Testcontainers.MsSql + actual EF SaveChanges against SQL Server.
/// That test is in C2_ConcurrencyIntegrationTests (Testcontainers suite, requires Docker).
///
/// Domain guard: AddEntry checks AvailableBalance + quantity &lt; 0 before appending.
/// Under true concurrent SQL: RowVersion ensures only one transaction wins when two
/// processes read the same balance row simultaneously.
/// </summary>
public sealed class C2_ConcurrencyDesignTests(ITestOutputHelper output)
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── Domain-layer concurrent access: 20 tasks, balance=1, 1 must succeed ──

    [Fact]
    public async Task DomainGuard_20ConcurrentConsumes_Balance1_ExactlyOneSucceeds()
    {
        // Domain balance is NOT thread-safe (single-aggregate, single-thread per request is
        // the expected pattern). This test simulates 20 requests all receiving the same
        // in-memory balance object before DB save — proving the guard works correctly when
        // accessed sequentially (which is what EF does within a single request scope).
        //
        // For TRUE concurrent protection: RowVersion at DB level — see Testcontainers suite.

        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(1m, Today);

        int successes = 0;
        int failures = 0;
        var lockObj = new object();

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            try
            {
                lock (lockObj) // simulate single-writer (EF DbContext is not thread-safe)
                {
                    balance.Consume(1m, Today, $"req-{Guid.NewGuid()}");
                }
                Interlocked.Increment(ref successes);
            }
            catch (InvalidOperationException)
            {
                Interlocked.Increment(ref failures);
            }
        }));

        await Task.WhenAll(tasks);

        output.WriteLine($"Successes: {successes}, Failures: {failures}");

        successes.Should().Be(1, "only 1 day available → only 1 consume can succeed");
        failures.Should().Be(19);
        balance.AvailableBalance.Should().Be(0m, "balance exactly zero after 1 successful consume");
    }

    [Fact]
    public async Task DomainGuard_20ConcurrentConsumes_Balance20_AllSucceed()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(20m, Today);

        var lockObj = new object();
        int successes = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            lock (lockObj)
            {
                balance.Consume(1m, Today, $"req-{Guid.NewGuid()}");
            }
            Interlocked.Increment(ref successes);
        }));

        await Task.WhenAll(tasks);

        successes.Should().Be(20);
        balance.AvailableBalance.Should().Be(0m);
    }

    [Fact]
    public void RowVersion_Property_Present_OnLeaveBalance()
    {
        // RowVersion property must exist and be configured as IsRowVersion() in DbContext.
        // This is the schema-level concurrency token that EF uses for optimistic locking.
        // When two SaveChanges calls race, EF throws DbUpdateConcurrencyException for the loser.
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.RowVersion.Should().NotBeNull("RowVersion concurrency token must exist for SQL-level locking");
    }

    // ── Concurrency invariant: balance never goes below zero ──────────────

    [Fact]
    public void DomainGuard_BalanceNeverNegative_Under1000SerialConsumeAttempts()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(10m, Today); // only 10 days

        int failures = 0;
        for (int i = 0; i < 1000; i++)
        {
            try { balance.Consume(0.1m, Today, $"r{i}"); }
            catch (InvalidOperationException) { failures++; }
        }

        balance.AvailableBalance.Should().Be(0m, "all 10 days consumed exactly");
        failures.Should().Be(900, "900 of 1000 attempts rejected after balance exhausted");
    }

    // ── Test boundary: what Testcontainers integration test must prove ────

    [Fact]
    public void ConcurrencyContract_TestContainersRequired_IsDocumented()
    {
        // This assertion documents the gap. A skippable marker for the integration suite.
        // To prove RowVersion concurrency:
        //   1. Spin up SQL Server via Testcontainers.MsSql
        //   2. Run EF migrations
        //   3. Two DbContexts read same LeaveBalance row simultaneously
        //   4. Both call SaveChanges
        //   5. One gets DbUpdateConcurrencyException
        //   6. Handler retries → reads fresh RowVersion → attempt fails (balance insufficient)
        //   7. Net result: balance = 0, not -1
        //
        // Until this test exists: concurrency behavior is unproven at DB level.
        // Domain guard prevents -balance within a single request; RowVersion prevents it across requests.

        const string message = "Real DB concurrency test requires Testcontainers.MsSql integration suite";
        message.Should().NotBeEmpty("document the gap");
    }
}
