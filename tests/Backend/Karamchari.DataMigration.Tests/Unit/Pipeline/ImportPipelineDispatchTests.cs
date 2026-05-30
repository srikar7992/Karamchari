using FluentAssertions;
using Karamchari.DataMigration.API;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Pipeline;

/// <summary>
/// A minimal record used purely as the generic type parameter for <see cref="ImportPipeline{T}"/>
/// in these tests — keeps tests independent of any real feature record.
/// </summary>
public record TestRecord(string? Id);

public sealed class ImportPipelineDispatchTests
{
    // ------------------------------------------------------------------ helpers

    private static ImportRow MakeRow(int rowNum, params (string key, string value)[] columns)
    {
        var data = columns.ToDictionary(c => c.key, c => c.value);
        return new ImportRow(rowNum, data);
    }

    private static ImportRow MakeSimpleRow(int rowNum)
        => MakeRow(rowNum, ("id", rowNum.ToString()));

    /// <summary>
    /// Builds a fake <see cref="IImportFileParser"/> that streams the supplied rows.
    /// </summary>
    private static IImportFileParser ParserFor(IEnumerable<ImportRow> rows)
    {
        var parser = Substitute.For<IImportFileParser>();
        parser.ParseAsync(Arg.Any<Stream>()).Returns(rows.ToAsyncEnumerable());
        return parser;
    }

    private static ImportJob MakeJob()
        => ImportJob.Create("test", "file.csv", "stored-id", "hash", "tester");

    /// <summary>
    /// Constructs a fully-wired <see cref="ImportPipeline{T}"/> with the given collaborators.
    /// </summary>
    private static ImportPipeline<TestRecord> BuildPipeline(
        IImportMapper<TestRecord> mapper,
        IImportValidator<TestRecord> validator,
        IImportProcessor<TestRecord> processor)
    {
        return new ImportPipeline<TestRecord>(
            mapper,
            validator,
            processor,
            NullLogger<ImportPipeline<TestRecord>>.Instance);
    }

    // ------------------------------------------------------------------ ImportType

    [Fact]
    public void ImportType_ReturnsDeclaredTypeFromProcessor()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        var validator = Substitute.For<IImportValidator<TestRecord>>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        processor.ImportType.Returns("employees");

        var sut = BuildPipeline(mapper, validator, processor);

        sut.ImportType.Should().Be("employees");
    }

    // ------------------------------------------------------------------ PreviewAsync

    [Fact]
    public async Task PreviewAsync_EmptyStream_ReturnsEmptyList()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        var validator = Substitute.For<IImportValidator<TestRecord>>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var parser = ParserFor(Enumerable.Empty<ImportRow>());

        var result = await sut.PreviewAsync(Stream.Null, parser, maxRows: 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewAsync_ReturnsAtMostMaxRows()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(ci => new TestRecord(ci.Arg<ImportRow>().RowNumber.ToString()));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 20).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var result = await sut.PreviewAsync(Stream.Null, parser, maxRows: 5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task PreviewAsync_MapsEachRowViaMapper()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(ci =>
            new TestRecord("mapped-" + ci.Arg<ImportRow>().RowNumber));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var rows = new[] { MakeSimpleRow(1), MakeSimpleRow(2) };
        var parser = ParserFor(rows);

        var result = (await sut.PreviewAsync(Stream.Null, parser, maxRows: 10))
            .Cast<TestRecord>().ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("mapped-1");
        result[1].Id.Should().Be("mapped-2");
    }

    [Fact]
    public async Task PreviewAsync_WhenStreamHasFewerRowsThanMax_ReturnsAllRows()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("x"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 3).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var result = await sut.PreviewAsync(Stream.Null, parser, maxRows: 100);

        result.Should().HaveCount(3);
    }

    // ------------------------------------------------------------------ ValidateAsync

    [Fact]
    public async Task ValidateAsync_AllRowsValid_ReturnsZeroErrors()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("ok"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 5).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (validCount, invalidCount, errors) = await sut.ValidateAsync(Stream.Null, parser);

        validCount.Should().Be(5);
        invalidCount.Should().Be(0);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_SomeRowsInvalid_CollectsErrors()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(ci => new TestRecord(ci.Arg<ImportRow>().RowNumber.ToString()));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Is<TestRecord>(r => r.Id == "2"), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(false, "Row 2 is invalid"));
        validator.ValidateAsync(Arg.Is<TestRecord>(r => r.Id != "2"), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var rows = new[] { MakeSimpleRow(1), MakeSimpleRow(2), MakeSimpleRow(3) };
        var parser = ParserFor(rows);

        var (validCount, invalidCount, errors) = await sut.ValidateAsync(Stream.Null, parser);

        validCount.Should().Be(2);
        invalidCount.Should().Be(1);
        errors.Should().HaveCount(1);
        errors[0].RowNumber.Should().Be(2);
        errors[0].ErrorMessage.Should().Be("Row 2 is invalid");
    }

    [Fact]
    public async Task ValidateAsync_MultipleInvalidRows_CollectsAllErrors()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("data"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(false, "bad data"));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 4).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (validCount, invalidCount, errors) = await sut.ValidateAsync(Stream.Null, parser);

        validCount.Should().Be(0);
        invalidCount.Should().Be(4);
        errors.Should().HaveCount(4);
    }

    [Fact]
    public async Task ValidateAsync_MappingExceptionOnRow_ReportsAsRowError()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Is<ImportRow>(r => r.RowNumber == 2))
              .Throws(new InvalidOperationException("Bad column"));
        mapper.Map(Arg.Is<ImportRow>(r => r.RowNumber != 2))
              .Returns(new TestRecord("ok"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var rows = new[] { MakeSimpleRow(1), MakeSimpleRow(2), MakeSimpleRow(3) };
        var parser = ParserFor(rows);

        var (validCount, invalidCount, errors) = await sut.ValidateAsync(Stream.Null, parser);

        validCount.Should().Be(2);
        invalidCount.Should().Be(1);
        errors.Should().HaveCount(1);
        errors[0].RowNumber.Should().Be(2);
        errors[0].ErrorMessage.Should().Contain("Mapping error");
        errors[0].ErrorMessage.Should().Contain("Bad column");
    }

    [Fact]
    public async Task ValidateAsync_EmptyStream_ReturnsZeroCounts()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        var validator = Substitute.For<IImportValidator<TestRecord>>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var parser = ParserFor(Enumerable.Empty<ImportRow>());

        var (validCount, invalidCount, errors) = await sut.ValidateAsync(Stream.Null, parser);

        validCount.Should().Be(0);
        invalidCount.Should().Be(0);
        errors.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ ProcessAsync

    [Fact]
    public async Task ProcessAsync_EmptyStream_ReturnsZeroCounts()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        var validator = Substitute.For<IImportValidator<TestRecord>>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        var sut = BuildPipeline(mapper, validator, processor);

        var parser = ParserFor(Enumerable.Empty<ImportRow>());

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        success.Should().Be(0);
        failed.Should().Be(0);
        skipped.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_AllValid_CallsProcessorAndCountsSuccess()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("row"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 3).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        success.Should().Be(3);
        failed.Should().Be(0);
        skipped.Should().Be(0);
        await processor.Received().ProcessBatchAsync(Arg.Any<IEnumerable<TestRecord>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_InvalidRowsAreNotPassedToProcessor_CountedAsFailed()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(ci =>
            new TestRecord(ci.Arg<ImportRow>().RowNumber.ToString()));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Is<TestRecord>(r => r.Id == "2"), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(false, "invalid"));
        validator.ValidateAsync(Arg.Is<TestRecord>(r => r.Id != "2"), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = new[] { MakeSimpleRow(1), MakeSimpleRow(2), MakeSimpleRow(3) };
        var parser = ParserFor(rows);

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        success.Should().Be(2);
        failed.Should().Be(1);
        skipped.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_ProcessorExceptionOnBatch_CountsRowsAsFailed_AndContinues()
    {
        // 501 rows: row 500 triggers the mid-stream flush (batch.Count >= 500).
        // ProcessBatchAsync throws on the first call. The per-row catch block catches this,
        // increments failed by 1 (for the row that triggered the flush), and continues.
        // The batch list was NOT cleared — rows 1–499 remain buffered.
        // Row 501 is added to the existing 499-item batch (making 500), triggering a second
        // flush (callCount==2) which succeeds, crediting all 500 as successful.
        // Net result: failed=1, success=501.
        // (The 501-item batch flush on the second call credits all 501 items.)
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("row"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var callCount = 0;
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        processor.When(p => p.ProcessBatchAsync(Arg.Any<IEnumerable<TestRecord>>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("DB error");
            });

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 501).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        failed.Should().Be(1);
        success.Should().Be(501);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAsync_BatchesAt500Rows_FlushesOnBoundary()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("row"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var batchSizes = new List<int>();
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        processor.When(p => p.ProcessBatchAsync(Arg.Any<IEnumerable<TestRecord>>(), Arg.Any<CancellationToken>()))
            .Do(ci => batchSizes.Add(ci.Arg<IEnumerable<TestRecord>>().Count()));

        var sut = BuildPipeline(mapper, validator, processor);

        // 501 rows: expect one batch of exactly 500 flushed mid-stream,
        // then a trailing batch of 1.
        var rows = Enumerable.Range(1, 501).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        batchSizes.Should().HaveCount(2);
        batchSizes[0].Should().Be(500);
        batchSizes[1].Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_ExactlyOneBatch_When500RowsAllValid()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("row"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var batchCount = 0;
        var processor = Substitute.For<IImportProcessor<TestRecord>>();
        processor.When(p => p.ProcessBatchAsync(Arg.Any<IEnumerable<TestRecord>>(), Arg.Any<CancellationToken>()))
            .Do(_ => batchCount++);

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 500).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        // Mid-stream flush happens at exactly 500; the residual flush of an empty list is guarded.
        batchCount.Should().Be(1);
        success.Should().Be(500);
    }

    [Fact]
    public async Task ProcessAsync_MappingException_CountsRowAsFailed_AndContinues()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Is<ImportRow>(r => r.RowNumber == 2))
              .Throws(new InvalidOperationException("mapping error"));
        mapper.Map(Arg.Is<ImportRow>(r => r.RowNumber != 2))
              .Returns(new TestRecord("ok"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = new[] { MakeSimpleRow(1), MakeSimpleRow(2), MakeSimpleRow(3) };
        var parser = ParserFor(rows);

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        failed.Should().Be(1);
        success.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAsync_AllInvalidRows_ProcessorNeverCalled()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("bad"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(false, "all bad"));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 5).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        success.Should().Be(0);
        failed.Should().Be(5);
        await processor.DidNotReceive().ProcessBatchAsync(Arg.Any<IEnumerable<TestRecord>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_SkippedCountAlwaysZero_AsSkipLogicIsNotImplementedInPipeline()
    {
        // The pipeline does not implement skip logic today; skipped is always 0.
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(new TestRecord("row"));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        validator.ValidateAsync(Arg.Any<TestRecord>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 3).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (_, _, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        skipped.Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_CorrectSuccessCount_WhenMixOfValidAndInvalid()
    {
        var mapper = Substitute.For<IImportMapper<TestRecord>>();
        mapper.Map(Arg.Any<ImportRow>()).Returns(ci =>
            new TestRecord(ci.Arg<ImportRow>().RowNumber.ToString()));

        var validator = Substitute.For<IImportValidator<TestRecord>>();
        // Rows 1, 3, 5 valid; rows 2, 4 invalid
        validator.ValidateAsync(Arg.Is<TestRecord>(r => int.Parse(r.Id!) % 2 == 0), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(false, "even row"));
        validator.ValidateAsync(Arg.Is<TestRecord>(r => int.Parse(r.Id!) % 2 != 0), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(true));

        var processor = Substitute.For<IImportProcessor<TestRecord>>();

        var sut = BuildPipeline(mapper, validator, processor);

        var rows = Enumerable.Range(1, 5).Select(MakeSimpleRow).ToList();
        var parser = ParserFor(rows);

        var (success, failed, skipped) = await sut.ProcessAsync(Stream.Null, parser, MakeJob());

        success.Should().Be(3);
        failed.Should().Be(2);
        skipped.Should().Be(0);
    }
}
