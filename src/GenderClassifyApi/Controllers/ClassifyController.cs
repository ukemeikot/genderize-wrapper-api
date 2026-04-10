using GenderClassifyApi.Models;
using GenderClassifyApi.Services;
using GenderClassifyApi.Validators;
using Microsoft.AspNetCore.Mvc;

namespace GenderClassifyApi.Controllers;

/// <summary>
/// Handles name classification requests and returns enriched prediction responses.
/// </summary>
[ApiController]
[Route("api")]
public sealed class ClassifyController : ControllerBase
{
    private const string NoPredictionMessage = "No prediction available for the provided name";
    private readonly IGenderizeService _genderizeService;
    private readonly NameParameterValidator _nameParameterValidator;

    public ClassifyController(
        IGenderizeService genderizeService,
        NameParameterValidator nameParameterValidator)
    {
        _genderizeService = genderizeService;
        _nameParameterValidator = nameParameterValidator;
    }

    /// <summary>
    /// Classifies the provided name using Genderize.io and returns a processed response payload.
    /// </summary>
    /// <param name="name">The name query parameter supplied by the client.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>A success payload or a structured error response.</returns>
    [HttpGet("classify")]
    [ProducesResponseType(typeof(ClassifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Classify([FromQuery] string? name, CancellationToken cancellationToken)
    {
        var validationResult = _nameParameterValidator.Validate(Request, name);
        if (!validationResult.IsValid)
        {
            return StatusCode(
                validationResult.StatusCode,
                new ErrorResponse(validationResult.ErrorMessage!));
        }

        var normalizedName = validationResult.Name!;
        var genderizeResponse = await _genderizeService.GetGenderPredictionAsync(normalizedName, cancellationToken);

        if (string.IsNullOrWhiteSpace(genderizeResponse.Gender) || genderizeResponse.Count == 0)
        {
            return NotFound(new ErrorResponse(NoPredictionMessage));
        }

        var response = new ClassifyResponse(
            new ClassifyResponseData(
                genderizeResponse.Name ?? normalizedName,
                genderizeResponse.Gender,
                genderizeResponse.Probability,
                genderizeResponse.Count,
                genderizeResponse.Probability >= 0.7 && genderizeResponse.Count >= 100,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")));

        return Ok(response);
    }
}
