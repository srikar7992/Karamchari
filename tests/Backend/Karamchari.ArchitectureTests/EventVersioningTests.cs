// -----------------------------------------------------------------------
// <copyright file="EventVersioningTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Karamchari.Core.Contracts;
using Xunit;

namespace Karamchari.ArchitectureTests;

/// <summary>
/// Enforces the event-versioning standard (finding H-1):
/// every integration event is a versioned, immutable record implementing
/// <see cref="IIntegrationEvent"/>. Prevents drift back to unversioned or
/// unmarked contracts. See docs/development/standards/event-versioning-standard.md.
/// </summary>
public sealed class EventVersioningTests
{
    private static readonly Regex VersionSuffix = new(@"IntegrationEventV\d+$", RegexOptions.Compiled);

    private static List<Type> AllContractTypes()
    {
        // Force-load the full referenced assembly graph so no contracts assembly is missed.
        var seen = new HashSet<string>();
        var queue = new Queue<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        var loaded = new List<Assembly>();
        while (queue.Count > 0)
        {
            var asm = queue.Dequeue();
            if (asm.IsDynamic || !seen.Add(asm.FullName!)) continue;
            loaded.Add(asm);
            foreach (var r in asm.GetReferencedAssemblies())
            {
                try { queue.Enqueue(Assembly.Load(r)); } catch { /* unresolved ref: ignore */ }
            }
        }

        var types = new List<Type>();
        foreach (var asm in loaded)
        {
            if (asm.GetName().Name?.StartsWith("Karamchari", StringComparison.Ordinal) != true) continue;
            try { types.AddRange(asm.GetTypes()); }
            catch (ReflectionTypeLoadException ex) { types.AddRange(ex.Types.Where(t => t is not null)!); }
        }
        return types;
    }

    private static bool IsRecord(Type t) =>
        t.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null
        || t.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    [Fact]
    public void EveryIntegrationEventImplementsMarker()
    {
        // A concrete type named *IntegrationEventV{n} must carry the marker.
        var offenders = AllContractTypes()
            .Where(t => t.IsClass && !t.IsAbstract && VersionSuffix.IsMatch(t.Name))
            .Where(t => !typeof(IIntegrationEvent).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToList();

        offenders.Should().BeEmpty(
            "every versioned integration event must implement IIntegrationEvent");
    }

    [Fact]
    public void EveryIntegrationEventIsVersioned()
    {
        var offenders = AllContractTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IIntegrationEvent).IsAssignableFrom(t))
            .Where(t => !VersionSuffix.IsMatch(t.Name))
            .Select(t => t.FullName)
            .ToList();

        offenders.Should().BeEmpty(
            "every integration event type name must end with a version suffix, e.g. ...IntegrationEventV1");
    }

    [Fact]
    public void EveryIntegrationEventIsImmutableRecord()
    {
        var events = AllContractTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IIntegrationEvent).IsAssignableFrom(t))
            .ToList();

        events.Should().NotBeEmpty("the migration should have produced versioned integration events");

        foreach (var t in events)
        {
            IsRecord(t).Should().BeTrue($"{t.FullName} must be a record");

            // No public mutable field, and any public property setter must be init-only.
            t.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly)
                .Should().BeEmpty($"{t.FullName} must have no public mutable fields");

            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var setter = p.GetSetMethod();
                if (setter is null) continue;
                var isInitOnly = setter.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
                isInitOnly.Should().BeTrue($"{t.FullName}.{p.Name} must be init-only (immutable)");
            }
        }
    }
}
