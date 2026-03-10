using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ZilfPub;

/// <summary>
/// Provides access to Bootswatch themes via the Bootswatch API.
/// </summary>
internal static class BootswatchThemeService
{
    private const string BootswatchApiUrl = "https://bootswatch.com/api/5.json";
    private static readonly TimeSpan ThemeListCacheLifetime = TimeSpan.FromHours(24);
    private static readonly HttpClient HttpClient = new();
    private static readonly string CacheDirectory = GetCacheDirectory();
    private static readonly string ThemeListCachePath = Path.Combine(CacheDirectory, "themes.json");
    private static readonly string ThemeCssCacheDirectory = Path.Combine(CacheDirectory, "css");

    /// <summary>
    /// Fetches the list of available themes from the Bootswatch API.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The Bootswatch API response containing theme information.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public static async Task<BootswatchApiResponse> FetchThemesAsync(CancellationToken cancellationToken = default)
    {
        if (TryReadThemeListCache(requireFresh: true, out var cachedResponse) && cachedResponse is not null)
        {
            return cachedResponse;
        }

        BootswatchApiResponse? staleResponse = null;

        try
        {
            var response = await HttpClient.GetFromJsonAsync(
                BootswatchApiUrl,
                BootswatchThemeJsonContext.Default.BootswatchApiResponse,
                cancellationToken);

            if (response is null)
            {
                throw new InvalidOperationException("Failed to deserialize Bootswatch API response.");
            }

            TryWriteThemeListCache(new BootswatchThemeListCacheEntry
            {
                FetchedAtUtc = DateTimeOffset.UtcNow,
                Response = response,
            });

            return response;
        }
        catch when (TryReadThemeListCache(requireFresh: false, out staleResponse))
        {
            return staleResponse!;
        }
    }

    /// <summary>
    /// Fetches a specific theme by name from the Bootswatch API.
    /// </summary>
    /// <param name="themeName">The name of the theme to fetch (case-insensitive).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The theme information if found; otherwise, null.</returns>
    public static async Task<BootswatchTheme?> FetchThemeByNameAsync(string themeName, CancellationToken cancellationToken = default)
    {
        var apiResponse = await FetchThemesAsync(cancellationToken);
        return apiResponse.Themes.FirstOrDefault(t => 
            string.Equals(t.Name, themeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Downloads the minified CSS content for a specific theme.
    /// </summary>
    /// <param name="theme">The theme to download CSS for.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The CSS content as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the download fails.</exception>
    public static async Task<string> DownloadThemeCssAsync(BootswatchTheme theme, CancellationToken cancellationToken = default)
    {
        if (TryReadThemeCssCache(theme, out var cachedCss) && cachedCss is not null)
        {
            return cachedCss;
        }

        string? staleCss = null;

        try
        {
            var css = await HttpClient.GetStringAsync(theme.CssMin, cancellationToken);
            TryWriteThemeCssCache(theme, css);
            return css;
        }
        catch when (TryReadThemeCssCache(theme, out staleCss))
        {
            return staleCss!;
        }
    }

    private static bool TryReadThemeListCache(bool requireFresh, out BootswatchApiResponse? response)
    {
        response = null;

        if (!File.Exists(ThemeListCachePath))
        {
            return false;
        }

        try
        {
            var cacheEntry = JsonSerializer.Deserialize(
                File.ReadAllText(ThemeListCachePath),
                BootswatchThemeJsonContext.Default.BootswatchThemeListCacheEntry);

            if (cacheEntry is null)
            {
                return false;
            }

            if (requireFresh && DateTimeOffset.UtcNow - cacheEntry.FetchedAtUtc > ThemeListCacheLifetime)
            {
                return false;
            }

            response = cacheEntry.Response;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    private static void TryWriteThemeListCache(BootswatchThemeListCacheEntry cacheEntry)
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            File.WriteAllText(
                ThemeListCachePath,
                JsonSerializer.Serialize(cacheEntry, BootswatchThemeJsonContext.Default.BootswatchThemeListCacheEntry));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool TryReadThemeCssCache(BootswatchTheme theme, out string? css)
    {
        css = null;
        var cachePath = GetThemeCssCachePath(theme);

        if (!File.Exists(cachePath))
        {
            return false;
        }

        try
        {
            css = File.ReadAllText(cachePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryWriteThemeCssCache(BootswatchTheme theme, string css)
    {
        try
        {
            Directory.CreateDirectory(ThemeCssCacheDirectory);
            File.WriteAllText(GetThemeCssCachePath(theme), css);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string GetThemeCssCachePath(BootswatchTheme theme)
    {
        var themeName = SanitizeFileName(theme.Name);
        var urlHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(theme.CssMin)));
        return Path.Combine(ThemeCssCacheDirectory, $"{themeName}-{urlHash}.css");
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "theme";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            if (Array.IndexOf(invalidCharacters, ch) >= 0)
            {
                builder.Append('_');
            }
            else if (char.IsWhiteSpace(ch))
            {
                builder.Append('-');
            }
            else
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.Length == 0 ? "theme" : builder.ToString();
    }

    private static string GetCacheDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var rootDirectory = string.IsNullOrWhiteSpace(localAppData) ? Path.GetTempPath() : localAppData;
        return Path.Combine(rootDirectory, "ZilfPub", "Bootswatch");
    }
}

/// <summary>
/// Represents a cached Bootswatch theme list.
/// </summary>
internal sealed record BootswatchThemeListCacheEntry
{
    /// <summary>
    /// Gets when the theme list was fetched.
    /// </summary>
    [JsonPropertyName("fetchedAtUtc")]
    public required DateTimeOffset FetchedAtUtc { get; init; }

    /// <summary>
    /// Gets the cached Bootswatch API response.
    /// </summary>
    [JsonPropertyName("response")]
    public required BootswatchApiResponse Response { get; init; }
}

/// <summary>
/// Represents the response from the Bootswatch API.
/// </summary>
internal sealed record BootswatchApiResponse
{
    /// <summary>
    /// Gets the version of Bootstrap that these themes are based on.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Gets the list of available themes.
    /// </summary>
    [JsonPropertyName("themes")]
    public required BootswatchTheme[] Themes { get; init; }
}

/// <summary>
/// Represents a single Bootswatch theme.
/// </summary>
internal sealed record BootswatchTheme
{
    /// <summary>
    /// Gets the name of the theme.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the theme.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// Gets the URL to the minified CSS file for this theme.
    /// </summary>
    [JsonPropertyName("cssMin")]
    public required string CssMin { get; init; }

    /// <summary>
    /// Gets the URL to the non-minified CSS file for this theme.
    /// </summary>
    [JsonPropertyName("css")]
    public required string Css { get; init; }
}

/// <summary>
/// Source-generated JSON metadata for Bootswatch API models.
/// </summary>
[JsonSerializable(typeof(BootswatchApiResponse))]
[JsonSerializable(typeof(BootswatchTheme))]
[JsonSerializable(typeof(BootswatchThemeListCacheEntry))]
internal sealed partial class BootswatchThemeJsonContext : JsonSerializerContext
{
}
