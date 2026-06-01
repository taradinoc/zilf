/* Copyright 2010-2023 Tara McGrew
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Zilf.Common;

namespace Zilf.Playground.Services.Workspaces
{
    public sealed class Project
    {
        private readonly List<ProjectFile> files = new();
        private readonly List<string> includes = new();
        private ProjectFile? mainFile;

        public Guid Guid { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Untitled Project";
        public IReadOnlyCollection<ProjectFile> Files => files;
        public ICollection<string> Includes => includes;

        public event Action? FilesChanged;

        public ProjectFile MainFile
        {
            get
            {
                if (mainFile != null)
                    return mainFile;

                if (files.Count > 0)
                    return files[0];

                throw new InvalidOperationException("Project has no main file");
            }

            set
            {
                if (!files.Contains(value))
                    throw new ArgumentException("File is not part of project", nameof(value));

                if (mainFile != value)
                {
                    mainFile = value;
                    FilesChanged?.Invoke();
                }
            }
        }

        public ProjectFile AddFile(string path) => AddFile(path, notify: true);

        private ProjectFile AddFile(string path, bool notify)
        {
            if (files.Any(f => f.Path == path))
                throw new ArgumentException("Path is already in use", nameof(path));

            var result = new ProjectFile(path);
            result.ContentChanged += OnFileContentChanged;
            files.Add(result);

            mainFile ??= result;

            if (notify)
            {
                FilesChanged?.Invoke();
            }

            return result;
        }

        public bool RemoveFile(string path)
        {
            var file = files.FirstOrDefault(f => f.Path == path);

            if (file == null)
                return false;

            file.ContentChanged -= OnFileContentChanged;
            files.Remove(file);

            if (mainFile == file)
                mainFile = files.FirstOrDefault();

            FilesChanged?.Invoke();

            return true;
        }

        private void OnFileContentChanged()
        {
            FilesChanged?.Invoke();
        }

        public void NotifyChanged()
        {
            FilesChanged?.Invoke();
        }

        public IEnumerable<string> GetIncludePaths()
        {
            var allFilePaths = from f in files
                               let p = f.Path
                               select p[..p.LastIndexOf('/')];

            return from p in allFilePaths.Distinct()
                   let rank = includes.Contains(p) ? 1 : 0
                   orderby rank, p
                   select p;
        }

        public async Task ImportFromZipArchiveAsync(ZipArchive archive)
        {
            // First, try to read project metadata
            var metadataEntry = archive.GetEntry("project.json");
            string? mainFilePath = null;
            List<string> includes = new();

            if (metadataEntry != null)
            {
                using var metadataStream = metadataEntry.Open();
                using var reader = new StreamReader(metadataStream);
                var metadataJson = await reader.ReadToEndAsync();
                var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadataJson);

                if (metadata != null)
                {
                    if (metadata.TryGetValue("MainFile", out var mainFileElement))
                        mainFilePath = mainFileElement.GetString();

                    if (metadata.TryGetValue("Includes", out var includesElement))
                    {
                        includes = JsonSerializer.Deserialize<List<string>>(includesElement.GetRawText()) ?? new();
                    }
                }
            }

            // Extract all files from ZIP
            var zippedFiles = new List<(string Path, string Content)>();

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName == "project.json")
                    continue;

                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                var content = await reader.ReadToEndAsync();

                System.Diagnostics.Debug.WriteLine("Importing file from ZIP: " + entry.FullName);

                zippedFiles.Add((entry.FullName, content));
            }

            // Verify files are present
            if (zippedFiles.Count == 0)
            {
                throw new InvalidOperationException("No files found in ZIP archive");
            }

            // Clear existing project files
            foreach (var oldFile in this.files)
            {
                oldFile.ContentChanged -= OnFileContentChanged;
            }

            this.files.Clear();

            // Add extracted files to project
            foreach (var (path, content) in zippedFiles)
            {
                var file = AddFile(path, notify: false);
                file.Content = content;
            }

            // Restore includes from metadata
            foreach (var include in includes)
            {
                if (!this.includes.Contains(include))
                    this.includes.Add(include);
            }

            // Set main file if specified
            if (!string.IsNullOrEmpty(mainFilePath))
            {
                var mainFile = this.files.FirstOrDefault(f => f.Path == mainFilePath);
                if (mainFile != null)
                {
                    this.mainFile = mainFile;
                }
            }
            else if (this.files.Count > 0)
            {
                // If no main file specified, use first non-library file
                var nonLibraryFiles = this.files
                    .Where(f => !this.includes.Any(inc => f.Path.StartsWith(inc + "/")))
                    .OrderBy(f => f.Path)
                    .ToList();

                if (nonLibraryFiles.Count > 0)
                {
                    this.mainFile = nonLibraryFiles.First();
                }
                else
                {
                    // Fallback to any file if all are library files
                    this.mainFile = this.files.First();
                }
            }

            // Notify that project has changed
            FilesChanged?.Invoke();
        }
    }
}
