using System.Text.Json;
using GenderClassifyApi.Models;
using GenderClassifyApi.Services;

namespace GenderClassifyApi.Middleware;

/// <summary>
/// Converts unhandled exceptions into the API's standard JSON error shape.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the next middleware and maps known exceptions to stable HTTP responses.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (GenderizeUnavailableException)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status502BadGateway,
                "Unable to reach the gender prediction service");
        }
        catch (Exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";

        var payload = JsonSerializer.Serialize(new ErrorResponse(message), SerializerOptions);
        await context.Response.WriteAsync(payload);
    }
}
