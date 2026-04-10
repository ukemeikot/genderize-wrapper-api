using GenderClassifyApi.Models;

namespace GenderClassifyApi.Services;

/// <summary>
/// Defines the contract for retrieving gender predictions from the upstream provider.
/// </summary>
public interface IGenderizeService
{
    /// <summary>
    /// Fetches a gender prediction for the supplied name.
    /// </summary>
    /// <param name="name">The name to classify.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The raw upstream payload.</returns>
    Task<GenderizeApiResponse> GetGenderPredictionAsync(string name, CancellationToken cancellationToken = default);
}
