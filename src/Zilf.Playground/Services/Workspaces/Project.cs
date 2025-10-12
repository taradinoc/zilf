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
using System.Linq;
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

        public ProjectFile AddFile(string path)
        {
            if (files.Any(f => f.Path == path))
                throw new ArgumentException("Path is already in use", nameof(path));

            var result = new ProjectFile(path);
            result.ContentChanged += OnFileContentChanged;
            files.Add(result);

            mainFile ??= result;

            FilesChanged?.Invoke();

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

    }
}
