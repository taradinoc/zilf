/* Copyright 2010-2026 Tara McGrew
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

using System.Reflection;
using System.Text.RegularExpressions;

namespace Zilf.Emit
{
    public static partial class Metadata
    {
        public static string GetCreatorString()
        {
            string version = typeof(Metadata).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
               ?.InformationalVersion ?? "0.0.0";

            var match = GetVersionRegex().Match(version);
            if (!match.Success)
            {
                return "ZILF";
            }

            int major = int.Parse(match.Groups[1].Value);
            int minor = int.Parse(match.Groups[2].Value);
            int micro = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            char suffix = match.Groups[4].Success ? match.Groups[4].Value switch
            {
                "a" => 'a',
                "b" => 'b',
                "rc" => 'c',
                _ => '?'
            } : '~';

            const string SBase62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            return "ZILF" + SBase62[major] + SBase62[minor] + SBase62[micro] + suffix;
        }

        [GeneratedRegex(@"^(\d+)\.(\d+)(?:\.(\d+))?(a|b|rc)?")]
        private static partial Regex GetVersionRegex();
    }
}
