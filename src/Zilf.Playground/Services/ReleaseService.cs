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
using Microsoft.Extensions.Configuration;

namespace Zilf.Playground.Services
{
    /// <summary>
    /// Manages fetching release information using build-time generated data.
    /// </summary>
    public sealed class ReleaseService
    {
        private readonly IJSRuntime jsRuntime;
        

        private List<Release>? cachedReleases;
        private DateTime? cacheTime;
        private readonly TimeSpan cacheExpiry = TimeSpan.FromMinutes(30);
        private static readonly string HeptapodApiLatestUrl = "https://foss.heptapod.net/api/v4/projects/zilf%2Fzilf/releases/permalink/latest";
        private static readonly string HeptapodProxyLatestUrl = "http://localhost:5080/api/proxy/releases/latest";
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
            private readonly string? accessToken;

        public ReleaseService(IJSRuntime jsRuntime, HttpClient httpClient, IConfiguration? configuration = null)
        {
            this.jsRuntime = jsRuntime;
            this.httpClient = httpClient; // BaseAddress is configured in Program.cs
            this.accessToken = configuration?.GetValue<string>("HeptapodAccessToken");
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

                // First try to fetch from Heptapod API (with token if available)
                try
                {
                    var apiReleases = await FetchFromHeptapodApiAsync();
                    if (apiReleases?.Count > 0)
                    {
                        cachedReleases = apiReleases;
                        cacheTime = DateTime.UtcNow;
                        return cachedReleases;
                    }
                }
                catch
                {
                    // Fall through to JSON fallback
                }

                // Fallback to local JSON file
                return await GetReleasesFromJsonAsync();
            }

            private async Task<List<Release>?> FetchFromHeptapodApiAsync()
            {
                var token = accessToken;
                if (string.IsNullOrWhiteSpace(token))
                {
                    token = await TryGetTokenFromJsAsync();
                }

                var apiUrl = await IsLocalhostAsync() ? HeptapodProxyLatestUrl : HeptapodApiLatestUrl;
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    // Prefer GitLab/Heptapod PAT header
                    request.Headers.Add("PRIVATE-TOKEN", token);
                    // Also set Authorization header for OAuth tokens (no harm if ignored)
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                }
                request.Headers.Add("User-Agent", "ZILF-Playground/1.0");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Cache-Control", "no-cache");

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
                // The permalink returns a single release object
                try
                {
                    var latest = JsonSerializer.Deserialize<Release>(json, opts);
                    if (latest != null)
                    {
                        return [latest];
                    }
                }
                catch
                {
                    // Fall through and try list format
                }
            
                // As a fallback, try deserializing as a list (older code paths)
                try
                {
                    var list = JsonSerializer.Deserialize<List<Release>>(json, opts);
                    return list;
                }
                catch
                {
                    return null;
                }
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
         }        /// <summary>
        /// Finds the best matching download asset for the given platform.
        /// </summary>
        public ReleaseAssetLink? FindAssetForPlatform(Release release, Platform platform)
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

    public record ProxyResponse
    {
        [JsonPropertyName("contents")]
        public string? Contents { get; init; }
        
        [JsonPropertyName("status")]
        public ProxyStatus? Status { get; init; }
    }

    public record ProxyStatus
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; init; }
        
        [JsonPropertyName("content_type")]
        public string? ContentType { get; init; }
    }
}