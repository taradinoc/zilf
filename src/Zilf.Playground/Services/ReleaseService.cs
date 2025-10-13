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

        public ReleaseService(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
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

            await Task.Delay(1); // Make this async for consistency

            // Create release info from generated build data
            var assets = new List<ReleaseAssetLink>();
            
            // Map runtime identifiers to platform-friendly names
            var platformNames = new Dictionary<string, string>
            {
                { "win-x86", "Windows (x86)" },
                { "win-x64", "Windows (x64)" },
                { "linux-arm", "Linux (ARM)" },
                { "linux-arm64", "Linux (ARM64)" },
                { "linux-x64", "Linux (x64)" },
                { "osx-x64", "macOS (Intel)" },
                { "osx-arm64", "macOS (Apple Silicon)" }
            };

            int assetId = 1;
            foreach (var package in GeneratedReleaseInfo.PlatformPackages)
            {
                var rid = package.Key;
                var packageName = package.Value;
                var platformName = platformNames.GetValueOrDefault(rid, rid);
                
                assets.Add(new ReleaseAssetLink
                {
                    Id = assetId++,
                    Name = $"{platformName} binaries",
                    Url = GeneratedReleaseInfo.BaseUrl + packageName,
                    DirectAssetUrl = GeneratedReleaseInfo.BaseUrl + packageName,
                    LinkType = "package"
                });
            }

            var release = new Release
            {
                TagName = GeneratedReleaseInfo.Version,
                Name = $"ZILF {GeneratedReleaseInfo.Version}",
                Description = "Current build version",
                ReleasedAt = DateTime.UtcNow, // Current build time
                UpcomingRelease = false,
                Assets = new ReleaseAssets
                {
                    Links = assets
                },
                Links = new ReleaseLinks
                {
                    Self = GeneratedReleaseInfo.ReleasesPageUrl
                }
            };

            cachedReleases = [release];
            cacheTime = DateTime.UtcNow;
            
            return cachedReleases;
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

        /// <summary>
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