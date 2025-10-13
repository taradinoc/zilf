/* Copyright 2010-2025 Tara McGrew
 *
 * This file is part of ZILF.
 *
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace Zilf.Playground.Services
{
    /// <summary>
    /// Manages fetching release information using build-time generated data.
    /// </summary>
    public sealed partial class ReleaseService
    {
        private readonly IJSRuntime jsRuntime;


        private List<Release>? cachedReleases;
        private DateTime? cacheTime;
        private readonly TimeSpan cacheExpiry = TimeSpan.FromMinutes(30);
        private static readonly string ReleaseInfoUrl = "release-info.json";
        private static readonly Dictionary<string, string> PlatformNames = new()
        {
            { "win-x86", "Windows (x86)" },
            { "win-x64", "Windows (x64)" },
            { "linux-arm", "Linux (ARM)" },
            { "linux-arm64", "Linux (ARM64)" },
            { "linux-x64", "Linux (x64)" },
            { "osx-x64", "macOS (Intel)" },
            { "osx-arm64", "macOS (Apple Silicon)" }
        };
        private readonly HttpClient httpClient;

        public ReleaseService(IJSRuntime jsRuntime, HttpClient httpClient)
        {
            this.jsRuntime = jsRuntime;
            this.httpClient = httpClient; // BaseAddress is configured in Program.cs
        }

        /// <summary>
        /// Gets the latest release information using build-time generated data.
        /// </summary>

        public async Task<List<Release>> GetReleasesAsync()
        {
            if (cachedReleases != null && cacheTime.HasValue &&
                DateTime.UtcNow - cacheTime.Value < cacheExpiry)
            {
                return cachedReleases;
            }

            // 1. Try GitHub tags API (CORS-allowed)
            try
            {
                var ghRelease = await FetchLatestGitHubTagAsync();
                if (ghRelease != null)
                {
                    cachedReleases = [ghRelease];
                    cacheTime = DateTime.UtcNow;
                    return cachedReleases;
                }
            }
            catch { }

            // 2. Fallback to local JSON file
            return await GetReleasesFromJsonAsync();
        }

        private static readonly string GitHubTagsApiUrl = "https://api.github.com/repos/taradinoc/zilf/tags";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1869:Cache and reuse 'JsonSerializerOptions' instances", Justification = "This is only called once")]
        private async Task<Release?> FetchLatestGitHubTagAsync()
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, GitHubTagsApiUrl);
            req.Headers.UserAgent.ParseAdd("ZILF-Playground/1.0");
            req.Headers.Accept.ParseAdd("application/vnd.github.v3+json");
            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            var tags = JsonSerializer.Deserialize<List<GitHubTag>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (tags == null || tags.Count == 0)
                return null;

            // Parse and sort tags by version precedence
            var parsed = tags
                .Select(t => (Tag: t, Version: VersionInfo.TryParse(t.Name)))
                .Where(x => x.Version != null)
                .OrderByDescending(x => x.Version, VersionInfo.Comparer as IComparer<VersionInfo?>)
                .ToList();
            if (parsed.Count == 0)
                return null;

            var latest = parsed[0];
            // Load release-info.json for RIDs and package templates
            ReleaseInfoJson? info = null;
            try
            {
                var infoJson = await httpClient.GetStringAsync(ReleaseInfoUrl);
                info = JsonSerializer.Deserialize<ReleaseInfoJson>(infoJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { }

            var assets = new List<ReleaseAssetLink>();
            int assetId = 1;
            if (info != null && !string.IsNullOrEmpty(info.BaseUrl))
            {
                // Replace version in baseUrl and filenames
                string tag = latest.Tag.Name;
                string baseUrl = info.BaseUrl!;
                // Replace the old version in baseUrl with the new tag if present
                if (!string.IsNullOrEmpty(info.Version) && baseUrl.Contains(info.Version))
                    baseUrl = baseUrl.Replace(info.Version, tag);
                // Build filenames per new pattern using parsed version info
                var v = latest.Version!;
                foreach (var kvp in info.PlatformPackages)
                {
                    var rid = kvp.Key;
                    var template = kvp.Value;
                    // Determine extension per RID using the template if possible
                    string ext = ".tar.gz";
                    if (template.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        ext = ".zip";
                    else if (template.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                        ext = ".tar.gz";
                    else if (rid.StartsWith("win-", StringComparison.OrdinalIgnoreCase))
                        ext = ".zip";

                    // Compose filename: zilf-{major}.{minor}.{micro}[-alpha|beta|candidateN]-{rid}{ext}
                    string pre = v.PreType switch
                    {
                        null => string.Empty,
                        "a" => $"-alpha{v.PreNum}",
                        "b" => $"-beta{v.PreNum}",
                        "rc" => $"-candidate{v.PreNum}",
                        _ => string.Empty
                    };
                    string filename = $"zilf-{v.Major}.{v.Minor}.{v.Micro}{pre}-{rid}{ext}";
                    assets.Add(new ReleaseAssetLink
                    {
                        Id = assetId++,
                        Name = PlatformNames.GetValueOrDefault(rid, rid) + " binaries",
                        Url = baseUrl + filename,
                        DirectAssetUrl = baseUrl + filename,
                        LinkType = "package"
                    });
                }
            }
            return new Release
            {
                TagName = latest.Tag.Name,
                Name = $"ZILF {latest.Tag.Name}",
                Description = "Latest release from GitHub tags.",
                ReleasedAt = null,
                UpcomingRelease = false,
                Assets = new ReleaseAssets { Links = assets },
                // If this is a prerelease version, it might not have its own release page to link to
                // Links = new ReleaseLinks { Self = $"https://foss.heptapod.net/zilf/zilf/-/releases/{latest.Tag.Name}" }
                Links = new ReleaseLinks { Self = $"https://foss.heptapod.net/zilf/zilf/-/releases" }
            };
        }

        private class GitHubTag
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";
        }

        private partial class VersionInfo
        {
            public int Major;
            public int Minor;
            public int Micro;
            public string? PreType;
            public int PreNum;

            public static readonly IComparer<VersionInfo> Comparer = new VersionComparer();

            public static VersionInfo? TryParse(string tag)
            {
                // Regex: (?<major>\d+)\.(?<minor>\d+)(?:\.(?<micro>\d+))?(?:(?<prerelease_type>a|b|rc)(?<prerelease_num>\d+))?
                var re = VersionRegex();
                var m = re.Match(tag);
                if (!m.Success) return null;
                var v = new VersionInfo
                {
                    Major = int.Parse(m.Groups["major"].Value),
                    Minor = int.Parse(m.Groups["minor"].Value),
                    Micro = m.Groups["micro"].Success ? int.Parse(m.Groups["micro"].Value) : 0,
                    PreType = m.Groups["prerelease_type"].Success ? m.Groups["prerelease_type"].Value : null,
                    PreNum = m.Groups["prerelease_num"].Success ? int.Parse(m.Groups["prerelease_num"].Value) : 0
                };
                return v;
            }

            private class VersionComparer : IComparer<VersionInfo>
            {
                private static readonly Dictionary<string, int> Precedence = new()
                {
                    { "rc", 2 },
                    { "b", 1 },
                    { "a", 0 }
                };
                public int Compare(VersionInfo? x, VersionInfo? y)
                {
                    if (x == null && y == null) return 0;
                    if (x == null) return -1;
                    if (y == null) return 1;
                    int c = x.Major.CompareTo(y.Major);
                    if (c != 0) return c;
                    c = x.Minor.CompareTo(y.Minor);
                    if (c != 0) return c;
                    c = x.Micro.CompareTo(y.Micro);
                    if (c != 0) return c;
                    int px = x.PreType is null ? 3 : Precedence.GetValueOrDefault(x.PreType, -1);
                    int py = y.PreType is null ? 3 : Precedence.GetValueOrDefault(y.PreType, -1);
                    c = px.CompareTo(py);
                    if (c != 0) return c;
                    return x.PreNum.CompareTo(y.PreNum);
                }
            }

            [GeneratedRegex(@"^(?<major>\d+)\.(?<minor>\d+)(?:\.(?<micro>\d+))?(?:(?<prerelease_type>a|b|rc)(?<prerelease_num>\d+))?$", RegexOptions.IgnoreCase, "en-US")]
            private static partial Regex VersionRegex();
        }

        private async Task<List<Release>> GetReleasesFromJsonAsync()
        {
            ReleaseInfoJson? info = null;
            try
            {
                // Fetch release-info.json from wwwroot
                var json = await httpClient.GetStringAsync(ReleaseInfoUrl);
                info = JsonSerializer.Deserialize<ReleaseInfoJson>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                // Swallow and use fallback below
            }

            if (info == null)
            {
                // Fallback to a minimal default that points to the releases page
                return [new Release
                {
                    TagName = null,
                    Name = "ZILF",
                    Description = "Release information is currently unavailable. Please check the releases page.",
                    ReleasedAt = null,
                    UpcomingRelease = false,
                    Assets = new ReleaseAssets { Links = new List<ReleaseAssetLink>() },
                    Links = new ReleaseLinks { Self = "https://foss.heptapod.net/zilf/zilf/-/releases" }
                }];
            }

            var assets = new List<ReleaseAssetLink>();
            int assetId = 1;
            foreach (var kvp in info.PlatformPackages)
            {
                var rid = kvp.Key;
                var packageName = kvp.Value;
                var platformName = PlatformNames.GetValueOrDefault(rid, rid);
                assets.Add(new ReleaseAssetLink
                {
                    Id = assetId++,
                    Name = $"{platformName} binaries",
                    Url = info.BaseUrl + packageName,
                    DirectAssetUrl = info.BaseUrl + packageName,
                    LinkType = "package"
                });
            }

            var release = new Release
            {
                TagName = info.Version,
                Name = $"ZILF {info.Version}",
                Description = "Current build version",
                ReleasedAt = DateTime.UtcNow, // Could parse from info if available
                UpcomingRelease = false,
                Assets = new ReleaseAssets
                {
                    Links = assets
                },
                Links = new ReleaseLinks
                {
                    Self = info.ReleasesPageUrl
                }
            };

            return [release];
        }
        // Helper class for deserializing release-info.json
        public class ReleaseInfoJson
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("packageVersion")]
            public string? PackageVersion { get; set; }

            [JsonPropertyName("baseUrl")]
            public string? BaseUrl { get; set; }

            [JsonPropertyName("releasesPageUrl")]
            public string? ReleasesPageUrl { get; set; }

            [JsonPropertyName("platformPackages")]
            public Dictionary<string, string> PlatformPackages { get; set; } = new();
        }

        /// <summary>
        /// Gets the latest release.
        /// </summary>
        public async Task<Release?> GetLatestReleaseAsync()
        {
            var releases = await GetReleasesAsync();
            return releases.FirstOrDefault();
        }

        /// <summary>
        /// Detects the current platform from the browser.
        /// </summary>
        public async Task<Platform> DetectPlatformAsync()
        {
            try
            {
                var userAgent = await jsRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
                var platform = await jsRuntime.InvokeAsync<string>("eval", "navigator.platform");

                // More comprehensive Windows detection
                if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
                    platform.Contains("Win", StringComparison.OrdinalIgnoreCase) ||
                    userAgent.Contains("win64", StringComparison.OrdinalIgnoreCase) ||
                    userAgent.Contains("wow64", StringComparison.OrdinalIgnoreCase))
                {
                    return Platform.Windows;
                }
                else if (userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase) ||
                         platform.Contains("Mac", StringComparison.OrdinalIgnoreCase) ||
                         userAgent.Contains("Darwin", StringComparison.OrdinalIgnoreCase))
                {
                    return Platform.MacOS;
                }
                else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
                         platform.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
                         userAgent.Contains("X11", StringComparison.OrdinalIgnoreCase))
                {
                    return Platform.Linux;
                }

                return Platform.Unknown;
            }
            catch
            {
                return Platform.Unknown;
            }
        }

        private async Task<bool> IsLocalhostAsync()
        {
            try
            {
                var host = await jsRuntime.InvokeAsync<string>("eval", "location.hostname");
                return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.Equals("127.0.0.1");
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> TryGetTokenFromJsAsync()
        {
            try
            {
                // Try query string first: ?hp_token=...
                var qsToken = await jsRuntime.InvokeAsync<string?>("eval", "new URLSearchParams(location.search).get('hp_token')");
                if (!string.IsNullOrWhiteSpace(qsToken)) return qsToken;

                // Then localStorage: hpToken
                var lsToken = await jsRuntime.InvokeAsync<string?>("eval", "localStorage.getItem('hpToken')");
                return string.IsNullOrWhiteSpace(lsToken) ? null : lsToken;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the best matching download asset for the given platform.
        /// </summary>
        public static ReleaseAssetLink? FindAssetForPlatform(Release release, Platform platform)
        {
            if (release.Assets?.Links == null)
                return null;

            var links = release.Assets.Links;

            return platform switch
            {
                Platform.Windows => links.FirstOrDefault(l =>
                    l.DirectAssetUrl?.Contains("win-x64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("win-x64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.DirectAssetUrl?.Contains("win64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("win64", StringComparison.OrdinalIgnoreCase) == true),
                Platform.MacOS => links.FirstOrDefault(l =>
                    l.DirectAssetUrl?.Contains("osx-x64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("osx-x64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.DirectAssetUrl?.Contains("osx-arm64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("osx-arm64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.DirectAssetUrl?.Contains("macos", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("macos", StringComparison.OrdinalIgnoreCase) == true),
                Platform.Linux => links.FirstOrDefault(l =>
                    l.DirectAssetUrl?.Contains("linux-x64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("linux-x64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.DirectAssetUrl?.Contains("linux-arm64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("linux-arm64", StringComparison.OrdinalIgnoreCase) == true ||
                    l.DirectAssetUrl?.Contains("linux-arm", StringComparison.OrdinalIgnoreCase) == true ||
                    l.Url?.Contains("linux-arm", StringComparison.OrdinalIgnoreCase) == true),
                _ => null
            };
        }
    }

    public enum Platform
    {
        Unknown,
        Windows,
        MacOS,
        Linux
    }

    public record Release
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("released_at")]
        public DateTime? ReleasedAt { get; init; }

        [JsonPropertyName("upcoming_release")]
        public bool UpcomingRelease { get; init; }

        [JsonPropertyName("assets")]
        public ReleaseAssets? Assets { get; init; }

        [JsonPropertyName("_links")]
        public ReleaseLinks? Links { get; init; }
    }

    public record ReleaseAssets
    {
        [JsonPropertyName("sources")]
        public List<ReleaseAsset>? Sources { get; init; }

        [JsonPropertyName("links")]
        public List<ReleaseAssetLink>? Links { get; init; }
    }

    public record ReleaseAsset
    {
        [JsonPropertyName("format")]
        public string? Format { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }

    public record ReleaseAssetLink
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("direct_asset_url")]
        public string? DirectAssetUrl { get; init; }

        [JsonPropertyName("link_type")]
        public string? LinkType { get; init; }
    }

    public record ReleaseLinks
    {
        [JsonPropertyName("self")]
        public string? Self { get; init; }
    }
}