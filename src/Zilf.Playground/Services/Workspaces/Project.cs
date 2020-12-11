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
        public IReadOnlyCollection<ProjectFile> Files => files;
        public ICollection<string> Includes => includes;

        public event Action? FilesChanged;

        [DisallowNull]
        public ProjectFile? MainFile
        {
            get => mainFile;

            set
            {
                if (!files.Contains(value))
                    throw new ArgumentException("File is not part of project", nameof(value));

                mainFile = value;
            }
        }

        public ProjectFile AddFile(string path)
        {
            if (files.Any(f => f.Path == path))
                throw new ArgumentException("Path is already in use", nameof(path));

            var result = new ProjectFile(path);
            files.Add(result);

            if (mainFile == null)
                mainFile = result;

            FilesChanged?.Invoke();

            return result;
        }

        public bool RemoveFile(string path)
        {
            var file = files.FirstOrDefault(f => f.Path == path);

            if (file == null)
                return false;

            files.Remove(file);

            if (mainFile == file)
                mainFile = files.FirstOrDefault();

            FilesChanged?.Invoke();

            return true;
        }

        public IFileSystem CopyToFileSystem()
        {
            var result = new InMemoryFileSystem();

            foreach (var f in files)
                result.SetText(f.Path, f.Content);

            return result;
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
