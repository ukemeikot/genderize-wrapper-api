namespace GenderClassifyApi.Services;

/// <summary>
/// Configuration values used when calling the Genderize API.
/// </summary>
public sealed class GenderizeOptions
{
    public const string SectionName = "Genderize";

    /// <summary>
    /// The absolute base URL of the Genderize API.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// The outbound HTTP timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 3;

    /// <summary>
    /// The optional API key used for higher Genderize limits.
    /// </summary>
    public string? ApiKey { get; init; }
}
