// -----------------------------------------------------------------------
// <copyright file="MeritMatrixTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compensation.Domain;

namespace Karamchari.Compensation.Tests.Domain;

public sealed class MeritMatrixTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private static MeritMatrix CreateMatrix()
        => MeritMatrix.Create(
            tenantId: "tenant-001",
            name: "2026 Merit Matrix",
            reviewYear: 2026,
            currencyCode: "INR");

    private static void AddExceedsCell(MeritMatrix matrix,
        decimal compaMin = 80m, decimal compaMax = 100m)
        => matrix.AddCell(
            rating: MeritRating.ExceedsExpectations,
            compaRatioMin: compaMin,
            compaRatioMax: compaMax,
            minIncrementPercent: 8m,
            midIncrementPercent: 10m,
            maxIncrementPercent: 12m);

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void MeritMatrix_Create_IsNotPublished()
    {
        var matrix = CreateMatrix();

        matrix.Should().NotBeNull();
        matrix.Id.Should().NotBeEmpty();
        matrix.IsPublished.Should().BeFalse();
        matrix.Cells.Should().BeEmpty();
        matrix.ReviewYear.Should().Be(2026);
    }

    // ── AddCell ──────────────────────────────────────────────────────────────

    [Fact]
    public void MeritMatrix_AddCell_WhenUnpublished_AddsCell()
    {
        var matrix = CreateMatrix();

        AddExceedsCell(matrix);

        matrix.Cells.Should().HaveCount(1);
        matrix.Cells.First().Rating.Should().Be(MeritRating.ExceedsExpectations);
        matrix.Cells.First().CompaRatioMin.Should().Be(80m);
        matrix.Cells.First().CompaRatioMax.Should().Be(100m);
        matrix.Cells.First().MinIncrementPercent.Should().Be(8m);
        matrix.Cells.First().MidIncrementPercent.Should().Be(10m);
        matrix.Cells.First().MaxIncrementPercent.Should().Be(12m);
    }

    [Fact]
    public void MeritMatrix_AddCell_WhenPublished_ThrowsDomainException()
    {
        var matrix = CreateMatrix();
        AddExceedsCell(matrix);
        matrix.Publish();

        Action act = () => AddExceedsCell(matrix, compaMin: 100m, compaMax: 120m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*published*");
    }

    // ── Publish ──────────────────────────────────────────────────────────────

    [Fact]
    public void MeritMatrix_Publish_WithNoCells_ThrowsDomainException()
    {
        var matrix = CreateMatrix();

        Action act = () => matrix.Publish();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void MeritMatrix_Publish_WithCells_SetsIsPublishedTrue()
    {
        var matrix = CreateMatrix();
        AddExceedsCell(matrix);

        matrix.Publish();

        matrix.IsPublished.Should().BeTrue();
    }

    // ── Lookup ───────────────────────────────────────────────────────────────

    [Fact]
    public void MeritMatrix_Lookup_MatchingRatingAndCompaRatio_ReturnsCell()
    {
        var matrix = CreateMatrix();
        // Cell: ExceedsExpectations, compa-ratio [80, 100)
        AddExceedsCell(matrix, compaMin: 80m, compaMax: 100m);
        matrix.Publish();

        // compa-ratio 90 falls inside [80, 100)
        var cell = matrix.Lookup(MeritRating.ExceedsExpectations, compaRatio: 90m);

        cell.Should().NotBeNull();
        cell!.Rating.Should().Be(MeritRating.ExceedsExpectations);
        cell.MinIncrementPercent.Should().Be(8m);
    }

    [Fact]
    public void MeritMatrix_Lookup_NoMatchingCell_ReturnsNull()
    {
        var matrix = CreateMatrix();
        // Only ExceedsExpectations cell — no MeetsExpectations cell
        AddExceedsCell(matrix, compaMin: 80m, compaMax: 100m);
        matrix.Publish();

        var cell = matrix.Lookup(MeritRating.MeetsExpectations, compaRatio: 90m);

        cell.Should().BeNull();
    }

    [Fact]
    public void MeritMatrix_Lookup_MatchingRatingButOutsideCompaRange_ReturnsNull()
    {
        var matrix = CreateMatrix();
        // Cell covers [80, 100) only
        AddExceedsCell(matrix, compaMin: 80m, compaMax: 100m);
        matrix.Publish();

        // compa-ratio 110 is above the range
        var cell = matrix.Lookup(MeritRating.ExceedsExpectations, compaRatio: 110m);

        cell.Should().BeNull();
    }

    [Fact]
    public void MeritMatrix_Lookup_CompaRatioAtUpperBound_ReturnsNull()
    {
        // Lookup uses c.CompaRatioMin <= ratio < c.CompaRatioMax (exclusive upper bound)
        var matrix = CreateMatrix();
        AddExceedsCell(matrix, compaMin: 80m, compaMax: 100m);
        matrix.Publish();

        // compa-ratio exactly at 100 should NOT match (exclusive upper bound)
        var cell = matrix.Lookup(MeritRating.ExceedsExpectations, compaRatio: 100m);

        cell.Should().BeNull("upper bound is exclusive per the Lookup implementation");
    }
}
