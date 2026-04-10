using System.Net.Http.Json;
using GenderClassifyApi.Models;
using Microsoft.Extensions.Options;

namespace GenderClassifyApi.Services;

/// <summary>
/// Retrieves gender predictions from Genderize.io using an injected <see cref="HttpClient"/>.
/// </summary>
public sealed class GenderizeService : IGenderizeService
{
    private const string ServiceUnavailableMessage = "Unable to reach the gender prediction service";
    private readonly HttpClient _httpClient;
    private readonly GenderizeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenderizeService"/> class.
    /// </summary>
    public GenderizeService(HttpClient httpClient, IOptions<GenderizeOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <summary>
    /// Calls the upstream API and returns the raw prediction payload.
    /// </summary>
    public async Task<GenderizeApiResponse> GetGenderPredictionAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                BuildRequestUri(name),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new GenderizeUnavailableException(ServiceUnavailableMessage);
            }

            var payload = await response.Content.ReadFromJsonAsync<GenderizeApiResponse>(cancellationToken: cancellationToken);

            return payload ?? throw new GenderizeUnavailableException(ServiceUnavailableMessage);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GenderizeUnavailableException(ServiceUnavailableMessage, exception);
        }
        catch (HttpRequestException exception)
        {
            throw new GenderizeUnavailableException(ServiceUnavailableMessage, exception);
        }
    }

    private string BuildRequestUri(string name)
    {
        var queryParts = new List<string>
        {
            $"name={Uri.EscapeDataString(name)}"
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            queryParts.Add($"apikey={Uri.EscapeDataString(_options.ApiKey)}");
        }

        return $"?{string.Join("&", queryParts)}";
    }
}
