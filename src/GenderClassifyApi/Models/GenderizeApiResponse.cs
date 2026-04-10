using System.Text.Json.Serialization;

namespace GenderClassifyApi.Models;

/// <summary>
/// Represents the raw payload returned by Genderize.io.
/// </summary>
public sealed class GenderizeApiResponse
{
    /// <summary>
    /// Gets the name used for the upstream lookup.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the predicted gender.
    /// </summary>
    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    /// <summary>
    /// Gets the upstream confidence score.
    /// </summary>
    [JsonPropertyName("probability")]
    public double Probability { get; init; }

    /// <summary>
    /// Gets the upstream sample count.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }
}
