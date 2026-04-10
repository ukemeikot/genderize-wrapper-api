namespace GenderClassifyApi.Services;

/// <summary>
/// Represents a failure to reach the upstream Genderize service.
/// </summary>
public sealed class GenderizeUnavailableException : Exception
{
    public GenderizeUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
