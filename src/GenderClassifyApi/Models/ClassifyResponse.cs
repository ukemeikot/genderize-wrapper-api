using System.Text.Json.Serialization;

namespace GenderClassifyApi.Models;

/// <summary>
/// Represents the success response envelope.
/// </summary>
public sealed record ClassifyResponse(
    [property: JsonPropertyName("data")] ClassifyResponseData Data)
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "success";
}

/// <summary>
/// Represents the processed classification payload returned to clients.
/// </summary>
public sealed record ClassifyResponseData(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("gender")] string Gender,
    [property: JsonPropertyName("probability")] double Probability,
    [property: JsonPropertyName("sample_size")] int SampleSize,
    [property: JsonPropertyName("is_confident")] bool IsConfident,
    [property: JsonPropertyName("processed_at")] string ProcessedAt);
