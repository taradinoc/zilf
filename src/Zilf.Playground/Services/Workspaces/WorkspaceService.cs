#nullable enable

using System;
using System.Linq;

namespace Zilf.Playground.Services.Workspaces
{
    public sealed class WorkspaceService
    {
        private Project _project;

        public WorkspaceService()
        {
            _project = new();
            _project.FilesChanged += ProjectFilesChanged;
        }

        public Project Project
        {
            get => _project;

            set
            {
                if (value != _project)
                {
                    if (_project != null)
                        _project.FilesChanged -= ProjectFilesChanged;

                    _project = value;

                    if (value != null)
                        value.FilesChanged += ProjectFilesChanged;

                    StatusChanged?.Invoke();
                }
            }
        }

        private void ProjectFilesChanged()
        {
            StatusChanged?.Invoke();
        }

        public string? StatusText
        {
            get
            {
                var nonLibraryFileCount = Project.Files.Count(f => !Project.Includes.Any(i => f.Path.StartsWith(i + "/")));
                return nonLibraryFileCount == 0 ? null : nonLibraryFileCount.ToString();
            }
        }

        public event Action? StatusChanged;
    }
}
