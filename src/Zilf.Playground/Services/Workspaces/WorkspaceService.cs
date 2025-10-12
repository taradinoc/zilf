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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zilf.Playground.Services.Workspaces
{
    public sealed class WorkspaceService
    {
        private Project _project;
        private readonly ProjectStorageService _storageService;
        private CancellationTokenSource? _autoSaveCts;
        private bool _isSaving;

        public WorkspaceService(ProjectStorageService storageService)
        {
            _storageService = storageService;
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
            
            // Trigger auto-save with debounce
            _ = AutoSaveAsync();
        }

        private async Task AutoSaveAsync()
        {
            // Cancel any pending auto-save
            _autoSaveCts?.Cancel();
            _autoSaveCts = new CancellationTokenSource();
            var token = _autoSaveCts.Token;

            try
            {
                // Wait 2 seconds before saving (debounce)
                await Task.Delay(2000, token);
                
                // Don't save if already saving
                if (_isSaving)
                    return;

                _isSaving = true;
                SavingChanged?.Invoke();

                try
                {
                    await _storageService.SaveProjectAsync(Project);
                }
                finally
                {
                    _isSaving = false;
                    SavingChanged?.Invoke();
                }
            }
            catch (TaskCanceledException)
            {
                // Debounce was cancelled, that's fine
            }
        }

        public string? StatusText
        {
            get
            {
                var nonLibraryFileCount = Project.Files.Count(f => !Project.Includes.Any(i => f.Path.StartsWith(i + "/")));
                return nonLibraryFileCount == 0 ? null : nonLibraryFileCount.ToString();
            }
        }

        public bool IsSaving => _isSaving;

        public event Action? StatusChanged;
        public event Action? SavingChanged;
    }
}
