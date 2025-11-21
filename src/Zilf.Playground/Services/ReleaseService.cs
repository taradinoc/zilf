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
        private static readonly string GitHubReleasesApiUrl = "https://api.github.com/repos/taradinoc/zilf/releases";
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
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
        private static readonly Dictionary<string, string> ManifestOsDisplayNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { "win", "Windows" },
            { "windows", "Windows" },
            { "linux", "Linux" },
            { "osx", "macOS" },
            { "macos", "macOS" }
        };
        private static readonly string[] WindowsArchPreference = ["x64", "arm64", "x86"];
        private static readonly string[] MacArchPreference = ["arm64", "x64"];
        private static readonly string[] LinuxArchPreference = ["x64", "arm64", "arm", "x86"];
        private static readonly string[] WindowsTypePreference = ["msi", "zip", "tar.gz"];
        private static readonly string[] MacTypePreference = ["pkg", "tar.gz", "zip"];
        private static readonly string[] LinuxTypePreference = ["deb", "rpm", "tar.gz", "zip"];
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

            // 1. Try GitHub Releases API (CORS-allowed)
            try
            {
                var ghRelease = await FetchLatestGitHubReleaseAsync();
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1869:Cache and reuse 'JsonSerializerOptions' instances", Justification = "This is only called once")]
        private async Task<Release?> FetchLatestGitHubReleaseAsync()
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, GitHubReleasesApiUrl);
            req.Headers.UserAgent.ParseAdd("ZILF-Playground/1.0");
            req.Headers.Accept.ParseAdd("application/vnd.github.v3+json");
            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, JsonOptions);
            if (releases == null || releases.Count == 0)
                return null;

            // Get the latest non-draft release
            var latest = releases.FirstOrDefault(r => !r.Draft);
            if (latest == null)
                return null;

            var assets = BuildAssetsFromGitHubRelease(latest);
            var version = latest.TagName ?? "Unknown";

            return new Release
            {
                TagName = latest.TagName,
                Name = latest.Name ?? $"ZILF {version}",
                Description = latest.Body ?? $"Latest release packages for ZILF {version}.",
                ReleasedAt = latest.PublishedAt,
                UpcomingRelease = false,
                Assets = new ReleaseAssets { Links = assets },
                Links = new ReleaseLinks { Self = latest.HtmlUrl ?? "https://github.com/taradinoc/zilf/releases" }
            };
        }

        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("published_at")]
            public DateTime? PublishedAt { get; set; }

            [JsonPropertyName("draft")]
            public bool Draft { get; set; }

            [JsonPropertyName("prerelease")]
            public bool Prerelease { get; set; }

            [JsonPropertyName("assets")]
            public List<GitHubAsset>? Assets { get; set; }
        }

        private class GitHubAsset
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; } = "";
        }

        private static List<ReleaseAssetLink> BuildAssetsFromGitHubRelease(GitHubRelease release)
        {
            var assets = new List<ReleaseAssetLink>();
            if (release.Assets == null || release.Assets.Count == 0)
                return assets;

            foreach (var asset in release.Assets)
            {
                var parsed = ParseAssetFilename(asset.Name);
                if (parsed == null)
                    continue;

                var friendlyName = BuildAssetFriendlyName(parsed);
                assets.Add(new ReleaseAssetLink
                {
                    Id = asset.Id,
                    Name = friendlyName,
                    Url = asset.BrowserDownloadUrl,
                    DirectAssetUrl = asset.BrowserDownloadUrl,
                    LinkType = parsed.PackageType == "msi" || parsed.PackageType == "pkg" || parsed.PackageType == "deb" || parsed.PackageType == "rpm" ? "installer" : "package",
                    Os = parsed.Os,
                    Arch = parsed.Arch,
                    PackageType = parsed.PackageType
                });
            }

            return assets;
        }

        private static AssetInfo? ParseAssetFilename(string filename)
        {
            // Format: zilf-<version>-<os>-<arch>.<ext>
            // Example: zilf-0.11.1-linux-arm64.tar.gz
            // Note: version may contain hyphens (e.g., 0.11-candidate1)
            
            var parts = filename.Split('.');
            if (parts.Length < 2)
                return null;

            // Determine package type from extension(s)
            var packageType = parts[^1];
            if (parts.Length >= 3 && parts[^2] == "tar")
                packageType = "tar.gz";

            // Get the base name without extensions
            var baseName = parts.Length >= 3 && parts[^2] == "tar" 
                ? string.Join('.', parts[..^2])
                : string.Join('.', parts[..^1]);

            // Split by hyphens: zilf-<version-parts>-<os>-<arch>
            var segments = baseName.Split('-');
            if (segments.Length < 4) // Need at least: zilf, version, os, arch
                return null;

            if (!segments[0].Equals("zilf", StringComparison.OrdinalIgnoreCase))
                return null;

            // OS and arch are the last two segments
            var arch = segments[^1];
            var os = segments[^2];

            return new AssetInfo
            {
                Os = os,
                Arch = arch,
                PackageType = packageType
            };
        }

        private static string BuildAssetFriendlyName(AssetInfo info)
        {
            var friendlyOs = ManifestOsDisplayNames.GetValueOrDefault(info.Os, info.Os);
            var archPart = $" ({info.Arch})";
            var typePart = info.PackageType switch
            {
                "msi" => " installer",
                "pkg" => " installer",
                "deb" => " package (deb)",
                "rpm" => " package (rpm)",
                "tar.gz" => " package (tar.gz)",
                "zip" => " package (zip)",
                _ => $" package ({info.PackageType})"
            };
            return friendlyOs + archPart + typePart;
        }

        private class AssetInfo
        {
            public string Os { get; set; } = "";
            public string Arch { get; set; } = "";
            public string PackageType { get; set; } = "";
        }

        private async Task<List<Release>> GetReleasesFromJsonAsync()
        {
            ReleaseInfoJson? info = null;
            try
            {
                // Fetch release-info.json from wwwroot
                var json = await httpClient.GetStringAsync(ReleaseInfoUrl);
                info = JsonSerializer.Deserialize<ReleaseInfoJson>(json, JsonOptions);
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
                    Links = new ReleaseLinks { Self = "https://github.com/taradinoc/zilf/releases" }
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
            /// Detects the current platform and architecture from the browser.
            /// </summary>
            public async Task<PlatformInfo> DetectPlatformInfoAsync()
            {
                try
                {
                    var userAgent = await jsRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
                    var platform = await jsRuntime.InvokeAsync<string>("eval", "navigator.platform");

                    // Detect mobile devices and return Unknown (which will show fallback page)
                    if (IsMobileUserAgent(userAgent))
                    {
                        return new PlatformInfo { Platform = Platform.Unknown, Architecture = Architecture.Unknown };
                    }

                    var detectedPlatform = Platform.Unknown;
                    var detectedArch = Architecture.Unknown;

                    // Detect platform
                    if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
                        platform.Contains("Win", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("win64", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("wow64", StringComparison.OrdinalIgnoreCase))
                    {
                        detectedPlatform = Platform.Windows;
                    }
                    else if (userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase) ||
                             platform.Contains("Mac", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("Darwin", StringComparison.OrdinalIgnoreCase))
                    {
                        detectedPlatform = Platform.MacOS;
                    }
                    else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
                             platform.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("X11", StringComparison.OrdinalIgnoreCase))
                    {
                        detectedPlatform = Platform.Linux;
                    }

                    // Detect architecture
                    // Check for ARM first (more specific)
                    if (userAgent.Contains("aarch64", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("arm64", StringComparison.OrdinalIgnoreCase) ||
                        platform.Contains("arm64", StringComparison.OrdinalIgnoreCase) ||
                        (detectedPlatform == Platform.MacOS && platform.Contains("MacIntel") && 
                         userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))) // M1/M2 Macs often report as Intel
                    {
                        detectedArch = Architecture.ARM64;
                    }
                    else if (userAgent.Contains("armv7", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("arm", StringComparison.OrdinalIgnoreCase))
                    {
                        detectedArch = Architecture.ARM;
                    }
                    // Check for x64
                    else if (userAgent.Contains("x86_64", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("x64", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("amd64", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("win64", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("wow64", StringComparison.OrdinalIgnoreCase) ||
                             platform.Contains("x64", StringComparison.OrdinalIgnoreCase) ||
                             platform.Contains("Linux x86_64", StringComparison.OrdinalIgnoreCase) ||
                             (platform.Contains("Linux", StringComparison.OrdinalIgnoreCase) && !platform.Contains("arm", StringComparison.OrdinalIgnoreCase)))
                    {
                        detectedArch = Architecture.X64;
                    }
                    // Check for x86
                    else if (userAgent.Contains("i686", StringComparison.OrdinalIgnoreCase) ||
                             userAgent.Contains("i386", StringComparison.OrdinalIgnoreCase) ||
                             platform.Contains("Win32", StringComparison.OrdinalIgnoreCase))
                    {
                        detectedArch = Architecture.X86;
                    }
                    else
                    {
                        // Default assumptions based on platform
                        detectedArch = detectedPlatform switch
                        {
                            Platform.Windows => Architecture.X64, // Most Windows users are x64 now
                            Platform.Linux => Architecture.X64,   // Most Linux desktop users are x64
                            Platform.MacOS => Architecture.ARM64, // Most Mac users are M1/M2 now
                            _ => Architecture.Unknown
                        };
                    }

                    return new PlatformInfo { Platform = detectedPlatform, Architecture = detectedArch };
                }
                catch
                {
                    return new PlatformInfo { Platform = Platform.Unknown, Architecture = Architecture.Unknown };
                }
            }

            /// <summary>
        /// Detects the current platform from the browser.
        /// </summary>
        public async Task<Platform> DetectPlatformAsync()
        {
                var info = await DetectPlatformInfoAsync();
                return info.Platform;
        }

            private static bool IsMobileUserAgent(string userAgent)
            {
                // Common mobile indicators
                return userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("webOS", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("BlackBerry", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("IEMobile", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("Opera Mini", StringComparison.OrdinalIgnoreCase);
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
            public static ReleaseAssetLink? FindAssetForPlatform(Release release, Platform platform, Architecture arch = Architecture.Unknown)
        {
            if (release.Assets?.Links == null)
                return null;

            var links = release.Assets.Links;

                var metadataMatch = FindByMetadata(links, platform, arch);
            if (metadataMatch != null)
                return metadataMatch;

                // Fallback to URL-based matching if no metadata
                var archSuffix = arch switch
            {
                    Architecture.X64 => "x64",
                    Architecture.ARM64 => "arm64",
                    Architecture.ARM => "arm",
                    Architecture.X86 => "x86",
                _ => null
            };

                return (platform, archSuffix) switch
                {
                    (Platform.Windows, not null) => links.FirstOrDefault(l =>
                        l.DirectAssetUrl?.Contains($"win-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains($"win-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true),
                    (Platform.MacOS, not null) => links.FirstOrDefault(l =>
                        l.DirectAssetUrl?.Contains($"osx-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains($"osx-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true ||
                        l.DirectAssetUrl?.Contains($"macos-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains($"macos-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true),
                    (Platform.Linux, not null) => links.FirstOrDefault(l =>
                        l.DirectAssetUrl?.Contains($"linux-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains($"linux-{archSuffix}", StringComparison.OrdinalIgnoreCase) == true),
                    (Platform.Windows, _) => links.FirstOrDefault(l =>
                        l.DirectAssetUrl?.Contains("win-", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains("win-", StringComparison.OrdinalIgnoreCase) == true),
                    (Platform.MacOS, _) => links.FirstOrDefault(l =>
                        l.DirectAssetUrl?.Contains("osx-", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains("osx-", StringComparison.OrdinalIgnoreCase) == true ||
                        l.DirectAssetUrl?.Contains("macos-", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains("macos-", StringComparison.OrdinalIgnoreCase) == true),
                    (Platform.Linux, _) => links.FirstOrDefault(l =>
                        l.DirectAssetUrl?.Contains("linux-", StringComparison.OrdinalIgnoreCase) == true ||
                        l.Url?.Contains("linux-", StringComparison.OrdinalIgnoreCase) == true),
                    _ => null
                };
        }

            private static ReleaseAssetLink? FindByMetadata(IEnumerable<ReleaseAssetLink> links, Platform platform, Architecture arch)
        {
            var aliases = GetOsAliases(platform);
            if (aliases.Length == 0)
                return null;

            var candidates = links
                .Where(l => l.Os != null && aliases.Any(a => string.Equals(l.Os, a, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (candidates.Count == 0)
                return null;

                // Filter by architecture if known
                if (arch != Architecture.Unknown)
                {
                    var archString = arch switch
                    {
                        Architecture.X64 => "x64",
                        Architecture.ARM64 => "arm64",
                        Architecture.ARM => "arm",
                        Architecture.X86 => "x86",
                        _ => null
                    };

                    if (archString != null)
                    {
                        var archMatches = candidates
                            .Where(l => string.Equals(l.Arch, archString, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (archMatches.Count > 0)
                            candidates = archMatches;
                    }
                }

                candidates.Sort((a, b) => GetLinkScore(b, platform, arch).CompareTo(GetLinkScore(a, platform, arch)));
            return candidates.FirstOrDefault();
        }

            private static int GetLinkScore(ReleaseAssetLink link, Platform platform, Architecture arch)
        {
            var prefs = GetArchPreference(platform);
            var score = 0;
            if (!string.IsNullOrEmpty(link.Arch))
            {
                var idx = Array.IndexOf(prefs, link.Arch.ToLowerInvariant());
                if (idx >= 0)
                    score += 100 - idx;

                    // Bonus points for exact architecture match
                    var archString = arch switch
                    {
                        Architecture.X64 => "x64",
                        Architecture.ARM64 => "arm64",
                        Architecture.ARM => "arm",
                        Architecture.X86 => "x86",
                        _ => null
                    };

                    if (archString != null && string.Equals(link.Arch, archString, StringComparison.OrdinalIgnoreCase))
                        score += 50;
            }

            var typePrefs = GetTypePreference(platform);
            if (!string.IsNullOrEmpty(link.PackageType))
            {
                var idx = Array.IndexOf(typePrefs, link.PackageType.ToLowerInvariant());
                if (idx >= 0)
                    score += 20 - idx;
            }

            // Give preference to native installers on each platform
            if (platform == Platform.Windows && string.Equals(link.PackageType, "msi", StringComparison.OrdinalIgnoreCase))
                score += 5;
            else if (platform == Platform.MacOS && string.Equals(link.PackageType, "pkg", StringComparison.OrdinalIgnoreCase))
                score += 5;
            else if (platform == Platform.Linux && (string.Equals(link.PackageType, "deb", StringComparison.OrdinalIgnoreCase) || 
                                                     string.Equals(link.PackageType, "rpm", StringComparison.OrdinalIgnoreCase)))
                score += 5;

            return score;
        }

        private static string[] GetArchPreference(Platform platform) => platform switch
        {
            Platform.Windows => WindowsArchPreference,
            Platform.MacOS => MacArchPreference,
            Platform.Linux => LinuxArchPreference,
            _ => []
        };

        private static string[] GetTypePreference(Platform platform) => platform switch
        {
            Platform.Windows => WindowsTypePreference,
            Platform.MacOS => MacTypePreference,
            Platform.Linux => LinuxTypePreference,
            _ => []
        };

        private static string[] GetOsAliases(Platform platform) => platform switch
        {
            Platform.Windows => ["win", "windows"],
            Platform.MacOS => ["osx", "macos"],
            Platform.Linux => ["linux"],
            _ => []
        };
    }

    public enum Platform
    {
        Unknown,
        Windows,
        MacOS,
        Linux
    }

        public enum Architecture
        {
            Unknown,
            X64,
            X86,
            ARM,
            ARM64
        }

        public record PlatformInfo
        {
            public Platform Platform { get; init; }
            public Architecture Architecture { get; init; }
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

        [JsonPropertyName("os")]
        public string? Os { get; init; }

        [JsonPropertyName("arch")]
        public string? Arch { get; init; }

        [JsonPropertyName("package_type")]
        public string? PackageType { get; init; }
    }

    public record ReleaseLinks
    {
        [JsonPropertyName("self")]
        public string? Self { get; init; }
    }
}