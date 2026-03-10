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

using System;
using System.IO;
using System.Text;

namespace Zilf.Cli
{
    internal sealed class ProjectTemplateService
    {
        private readonly IHostFileSystem hostFileSystem;

        public ProjectTemplateService(IHostFileSystem hostFileSystem)
        {
            this.hostFileSystem = hostFileSystem;
        }

        public string CreateProject(string projectPath)
        {
            string fullProjectPath;
            try
            {
                fullProjectPath = Path.GetFullPath(projectPath);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new InvalidOperationException($"Error: Invalid project path: {ex.Message}", ex);
            }

            var trimmedPath = fullProjectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var projectName = Path.GetFileName(trimmedPath);

            if (string.IsNullOrWhiteSpace(projectName))
                throw new InvalidOperationException("Error: Could not determine project name from the specified path.");

            if (hostFileSystem.FileExists(fullProjectPath))
                throw new InvalidOperationException($"Error: A file already exists at '{fullProjectPath}'.");

            if (hostFileSystem.DirectoryExists(fullProjectPath))
                throw new InvalidOperationException($"Error: Directory '{fullProjectPath}' already exists.");

            var sampleDir = PathResolution.FindNearbyDirectory(PathResolution.GetProgramDirectory(), ["sample"], hostFileSystem);
            var templatePath = sampleDir != null
                ? Path.Combine(sampleDir, "empty", "empty.zil")
                : null;

            if (templatePath == null || !hostFileSystem.FileExists(templatePath))
                throw new InvalidOperationException("Error: Template file not found: sample/empty/empty.zil");

            var outputFile = Path.Combine(fullProjectPath, projectName + ".zil");
            if (hostFileSystem.FileExists(outputFile))
                throw new InvalidOperationException($"Error: A file already exists at '{outputFile}'.");

            try
            {
                hostFileSystem.CreateDirectory(fullProjectPath);
                var content = hostFileSystem.ReadAllText(templatePath);
                content = content.Replace("EMPTY GAME", projectName.ToUpperInvariant(), StringComparison.Ordinal);
                hostFileSystem.WriteAllText(outputFile, content, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("I/O error: " + ex.Message, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("Access error: " + ex.Message, ex);
            }

            return outputFile;
        }
    }
}