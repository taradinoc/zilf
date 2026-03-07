using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ZilfPub;

/// <summary>
/// Provides access to Bootswatch themes via the Bootswatch API.
/// </summary>
internal static class BootswatchThemeService
{
    private const string BootswatchApiUrl = "https://bootswatch.com/api/5.json";

    /// <summary>
    /// Fetches the list of available themes from the Bootswatch API.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The Bootswatch API response containing theme information.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public static async Task<BootswatchApiResponse> FetchThemesAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var response = await client.GetFromJsonAsync(
            BootswatchApiUrl,
            BootswatchThemeJsonContext.Default.BootswatchApiResponse,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException("Failed to deserialize Bootswatch API response.");
        }

        return response;
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
        using var client = new HttpClient();
        return await client.GetStringAsync(theme.CssMin, cancellationToken);
    }
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
internal sealed partial class BootswatchThemeJsonContext : JsonSerializerContext
{
}
