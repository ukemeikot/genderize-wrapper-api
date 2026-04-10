using System.Text.Json;
using FluentAssertions;
using GenderClassifyApi.Middleware;
using Microsoft.AspNetCore.Http;

namespace GenderClassifyApi.Tests.Middleware;

public sealed class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnStructured500Response_WhenUnhandledExceptionOccurs()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionMiddleware(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/json");
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should().Be("*");

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var payload = await reader.ReadToEndAsync();

        using var document = JsonDocument.Parse(payload);
        document.RootElement.GetProperty("status").GetString().Should().Be("error");
        document.RootElement.GetProperty("message").GetString().Should().Be("An unexpected error occurred");
    }
}
