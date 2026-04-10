using System.Text.Json.Serialization;

namespace GenderClassifyApi.Models;

/// <summary>
/// Represents the standard error response envelope returned by the API.
/// </summary>
public sealed record ErrorResponse(
    [property: JsonPropertyName("message")] string Message)
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "error";
}
