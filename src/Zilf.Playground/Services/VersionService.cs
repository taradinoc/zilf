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

using System;
using System.Reflection;

namespace Zilf.Playground.Services
{
    /// <summary>
    /// Provides version and build information for the ZILF Playground.
    /// </summary>
    public class VersionService
    {
        private readonly Lazy<string> version;
        private readonly Lazy<DateTime?> buildTimestamp;

        public VersionService()
        {
            version = new Lazy<string>(GetVersionString);
            buildTimestamp = new Lazy<DateTime?>(GetBuildTimestamp);
        }

        /// <summary>
        /// Gets the version string from the assembly's InformationalVersion attribute.
        /// </summary>
        public string Version => version.Value;

        /// <summary>
        /// Gets the build timestamp from the assembly, or null if not available.
        /// </summary>
        public DateTime? BuildTimestamp => buildTimestamp.Value;

        /// <summary>
        /// Gets a formatted build timestamp string suitable for display, or null if not available.
        /// </summary>
        public string BuildTimestampFormatted => BuildTimestamp?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? string.Empty;

        private static string GetVersionString()
        {
            var assembly = typeof(VersionService).Assembly;
            var versionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return versionAttr?.InformationalVersion ?? "Unknown";
        }

        private static DateTime? GetBuildTimestamp()
        {
            var ts = BuildInfo.BuildTimestampUtc;
            
            // DateTime.MinValue means "unknown" (see BuildInfo.Placeholder.cs)
            if (ts == DateTime.MinValue)
            {
                return null;
            }
            
            return ts;
        }
    }
}
