// -----------------------------------------------------------------------
// <copyright file="ValueComparers.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Karamchari.Core.Persistence;

/// <summary>
/// Provides standard <see cref="ValueComparer{T}"/> implementations for complex types
/// mapped to JSON columns. Prevents EF Core "no value comparer" warnings and ensures
/// correct change detection for mutable collections.
/// </summary>
public static class ValueComparers
{
    /// <summary>
    /// A comparer for <see cref="Dictionary{TKey, TValue}"/> that performs a deep equality
    /// check and creates shallow copies for snapshots.
    /// </summary>
    public static ValueComparer<Dictionary<string, decimal>> DictionaryComparer => new(
        (c1, c2) => CompareDictionaries(c1, c2),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
        c => new Dictionary<string, decimal>(c));

    /// <summary>
    /// A generic comparer for <see cref="List{T}"/> that performs a sequence equality
    /// check and creates copies for snapshots.
    /// </summary>
    public static ValueComparer<List<T>> ListComparer<T>() => new(
        (c1, c2) => CompareSequences(c1, c2),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
        c => c.ToList());

    /// <summary>
    /// A generic comparer for <see cref="IReadOnlyList{T}"/> that performs a sequence equality
    /// check and creates copies for snapshots.
    /// </summary>
    public static ValueComparer<IReadOnlyList<T>> ReadOnlyListComparer<T>() => new(
        (c1, c2) => CompareSequences(c1, c2),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
        c => (IReadOnlyList<T>)c.ToList());

    /// <summary>
    /// A generic comparer for <see cref="IReadOnlyCollection{T}"/> that performs a sequence equality
    /// check and creates copies for snapshots.
    /// </summary>
    public static ValueComparer<IReadOnlyCollection<T>> ReadOnlyCollectionComparer<T>() => new(
        (c1, c2) => CompareSequences(c1, c2),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
        c => (IReadOnlyCollection<T>)c.ToList());

    /// <summary>
    /// A comparer for <see cref="IReadOnlyDictionary{TKey, TValue}"/> that performs a deep equality
    /// check and creates shallow copies for snapshots.
    /// </summary>
    public static ValueComparer<IReadOnlyDictionary<string, decimal>> ReadOnlyDictionaryComparer => new(
        (c1, c2) => CompareDictionaries(c1 != null ? c1.ToDictionary(k => k.Key, v => v.Value) : null, c2 != null ? c2.ToDictionary(k => k.Key, v => v.Value) : null),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
        c => (IReadOnlyDictionary<string, decimal>)new Dictionary<string, decimal>(c));

    /// <summary>
    /// A generic comparer for complex objects that performs equality check via JSON serialization.
    /// Useful for single value objects mapped to JSON columns.
    /// </summary>
    public static ValueComparer<T> ObjectComparer<T>() => new(
        (l, r) => JsonSerializer.Serialize(l, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(r, (JsonSerializerOptions?)null),
        v => v == null ? 0 : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
        v => v == null ? default! : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

    private static bool CompareSequences<T>(IEnumerable<T>? s1, IEnumerable<T>? s2)
    {
        if (ReferenceEquals(s1, s2)) return true;
        if (s1 == null || s2 == null) return false;
        return s1.SequenceEqual(s2);
    }

    private static bool CompareDictionaries(Dictionary<string, decimal>? d1, Dictionary<string, decimal>? d2)
    {
        if (ReferenceEquals(d1, d2)) return true;
        if (d1 == null || d2 == null) return false;
        if (d1.Count != d2.Count) return false;
        return d1.OrderBy(kvp => kvp.Key).SequenceEqual(d2.OrderBy(kvp => kvp.Key));
    }
}
