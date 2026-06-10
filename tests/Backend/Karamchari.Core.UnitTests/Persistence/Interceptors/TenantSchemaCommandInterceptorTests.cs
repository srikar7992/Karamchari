// -----------------------------------------------------------------------
// <copyright file="TenantSchemaCommandInterceptorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Data;
using System.Data.Common;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Karamchari.Core.UnitTests.Persistence.Interceptors;

public sealed class TenantSchemaCommandInterceptorTests
{
    [Fact]
    public void NonQueryExecuting_RewritesBracketedPlaceholderSchema()
    {
        using var command = new TestDbCommand
        {
            CommandText = "INSERT INTO [__tenant__].[Employees] ([TenantId]) VALUES (@p0);",
        };
        var interceptor = CreateInterceptor("acme");

        interceptor.NonQueryExecuting(command, eventData: null!, default);

        command.CommandText.Should().Be("INSERT INTO [tenant_acme].[Employees] ([TenantId]) VALUES (@p0);");
    }

    [Fact]
    public void NonQueryExecuting_RewritesBarePlaceholderSchema()
    {
        using var command = new TestDbCommand
        {
            CommandText = "SELECT COUNT(*) FROM __tenant__.Employees;",
        };
        var interceptor = CreateInterceptor("acme-north");

        interceptor.NonQueryExecuting(command, eventData: null!, default);

        command.CommandText.Should().Be("SELECT COUNT(*) FROM tenant_acme_north.Employees;");
    }

    [Fact]
    public void NonQueryExecuting_DoesNotRewritePlaceholderSubstringsInsideOtherIdentifiers()
    {
        const string sql = "SELECT [not__tenant__].[Employees].[Id] FROM [not__tenant__].[Employees];";
        using var command = new TestDbCommand { CommandText = sql };
        var interceptor = CreateInterceptor("acme");

        interceptor.NonQueryExecuting(command, eventData: null!, default);

        command.CommandText.Should().Be(sql);
    }

    [Fact]
    public void NonQueryExecuting_WhenTenantCannotBeResolved_ThrowsBeforeSqlCanExecute()
    {
        using var command = new TestDbCommand
        {
            CommandText = "SELECT * FROM [__tenant__].[Employees];",
        };
        var interceptor = new TenantSchemaCommandInterceptor(
            new ThrowingTenantProvider(),
            NullLogger<TenantSchemaCommandInterceptor>.Instance);

        var act = () => interceptor.NonQueryExecuting(command, eventData: null!, default);

        act.Should().Throw<TenantResolutionException>();
    }

    private static TenantSchemaCommandInterceptor CreateInterceptor(string tenantId) =>
        new(
            new FixedTenantProvider(new TenantExecutionEnvelope(tenantId, "test-corr", "test-req", ExecutionSource.HttpRequest, TenantSource.JwtClaim)),
            NullLogger<TenantSchemaCommandInterceptor>.Instance);

    private sealed class FixedTenantProvider(TenantExecutionEnvelope tenant) : ITenantProvider
    {
        public string GetCurrentTenantId() => tenant.TenantId;

        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
        {
            tenantId = tenant.TenantId;
            return true;
        }

        public TenantExecutionEnvelope GetTenant() => tenant;

        public bool TryGetTenant(out TenantExecutionEnvelope? resolvedTenant)
        {
            resolvedTenant = tenant;
            return true;
        }
    }

    private sealed class ThrowingTenantProvider : ITenantProvider
    {
        public string GetCurrentTenantId() => GetTenant().TenantId;

        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
        {
            tenantId = null;
            return false;
        }

        public TenantExecutionEnvelope GetTenant() =>
            throw new TenantResolutionException("No tenant.", TenantResolutionFailureReason.MissingJwtClaim);

        public bool TryGetTenant(out TenantExecutionEnvelope? tenant)
        {
            tenant = null;
            return false;
        }
    }

    private sealed class TestDbCommand : DbCommand
    {
#pragma warning disable CS8765
        public override string CommandText { get; set; } = string.Empty;
#pragma warning restore CS8765

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection { get; } = new TestDbParameterCollection();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => throw new NotSupportedException();

        public override object? ExecuteScalar() => throw new NotSupportedException();

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    private sealed class TestDbParameterCollection : DbParameterCollection
    {
        public override int Count => 0;

        public override object SyncRoot { get; } = new();

        public override int Add(object value) => throw new NotSupportedException();

        public override void AddRange(Array values) => throw new NotSupportedException();

        public override void Clear()
        {
        }

        public override bool Contains(object value) => false;

        public override bool Contains(string value) => false;

        public override void CopyTo(Array array, int index)
        {
        }

        public override IEnumerator GetEnumerator() => Enumerable.Empty<object>().GetEnumerator();

        public override int IndexOf(object value) => -1;

        public override int IndexOf(string parameterName) => -1;

        public override void Insert(int index, object value) => throw new NotSupportedException();

        public override void Remove(object value) => throw new NotSupportedException();

        public override void RemoveAt(int index) => throw new NotSupportedException();

        public override void RemoveAt(string parameterName) => throw new NotSupportedException();

        protected override DbParameter GetParameter(int index) => throw new IndexOutOfRangeException();

        protected override DbParameter GetParameter(string parameterName) => throw new IndexOutOfRangeException();

        protected override void SetParameter(int index, DbParameter value) => throw new NotSupportedException();

        protected override void SetParameter(string parameterName, DbParameter value) => throw new NotSupportedException();
    }
}
