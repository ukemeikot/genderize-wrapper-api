namespace GenderClassifyApi.Validators;

/// <summary>
/// Validates the incoming <c>name</c> query parameter according to the assessment rules.
/// </summary>
public sealed class NameParameterValidator
{
    private const string MissingNameMessage = "Name parameter is required";
    private const string InvalidNameTypeMessage = "Name must be a valid string";

    /// <summary>
    /// Validates the request query and returns either a normalized name or an error result.
    /// </summary>
    /// <param name="request">The incoming HTTP request.</param>
    /// <param name="rawName">The model-bound query value.</param>
    /// <returns>The validation result.</returns>
    public NameParameterValidationResult Validate(HttpRequest request, string? rawName)
    {
        if (HasArrayLikeNameQuery(request))
        {
            return NameParameterValidationResult.Invalid(StatusCodes.Status422UnprocessableEntity, InvalidNameTypeMessage);
        }

        if (!request.Query.TryGetValue("name", out var values))
        {
            return NameParameterValidationResult.Invalid(StatusCodes.Status400BadRequest, MissingNameMessage);
        }

        if (values.Count != 1)
        {
            return NameParameterValidationResult.Invalid(StatusCodes.Status422UnprocessableEntity, InvalidNameTypeMessage);
        }

        var name = rawName?.Trim() ?? values[0]?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return NameParameterValidationResult.Invalid(StatusCodes.Status400BadRequest, MissingNameMessage);
        }

        return NameParameterValidationResult.Valid(name);
    }

    private static bool HasArrayLikeNameQuery(HttpRequest request)
    {
        return request.Query.Keys.Any(key =>
            key.Equals("name[]", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("name[", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Represents the result of validating the name query parameter.
/// </summary>
public sealed record NameParameterValidationResult(
    bool IsValid,
    string? Name,
    int StatusCode,
    string? ErrorMessage)
{
    public static NameParameterValidationResult Valid(string name) =>
        new(true, name, StatusCodes.Status200OK, null);

    public static NameParameterValidationResult Invalid(int statusCode, string message) =>
        new(false, null, statusCode, message);
}
