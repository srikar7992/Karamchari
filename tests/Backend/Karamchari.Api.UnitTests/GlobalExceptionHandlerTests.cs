using FluentAssertions;
using Karamchari.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Karamchari.Api.UnitTests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnInternalServerError_ForGenericException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new Exception("Test exception");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody);

        problemDetails!.Title.Should().Be("Server Error");
        problemDetails.Detail.Should().Be("Test exception");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnBadRequest_ForValidationException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var failures = new[] { new FluentValidation.Results.ValidationFailure("Prop", "Error") };
        var exception = new FluentValidation.ValidationException(failures);

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        problemDetails!.Title.Should().Be("Validation Error");
        // Extensions are deserialized as JsonElement
    }
}
