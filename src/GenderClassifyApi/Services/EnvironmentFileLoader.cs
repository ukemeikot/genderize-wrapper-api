namespace GenderClassifyApi.Services;

/// <summary>
/// Loads optional environment files before ASP.NET Core builds its configuration pipeline.
/// </summary>
public static class EnvironmentFileLoader
{
    /// <summary>
    /// Loads values from <c>.env</c> and then from an environment-specific file like
    /// <c>.env.production</c> or <c>.env.staging</c>. Existing OS environment variables take precedence.
    /// </summary>
    public static void Load()
    {
        var contentRoot = Directory.GetCurrentDirectory();
        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            Environments.Production;

        var loadedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        LoadFile(Path.Combine(contentRoot, ".env"), loadedKeys);
        LoadFile(Path.Combine(contentRoot, $".env.{environmentName.ToLowerInvariant()}"), loadedKeys);
    }

    private static void LoadFile(string path, ISet<string> loadedKeys)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');

            var existingValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(existingValue) && !loadedKeys.Contains(key))
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
            loadedKeys.Add(key);
        }
    }
}
