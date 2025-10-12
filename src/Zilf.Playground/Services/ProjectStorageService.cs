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
using System.Text.Json;
using System.Threading.Tasks;
using Zilf.Playground.Services.Workspaces;

namespace Zilf.Playground.Services
{
    /// <summary>
    /// Manages saving and loading projects to/from browser local storage.
    /// </summary>
    public sealed class ProjectStorageService
    {
        private const string StorageKeyPrefix = "zilf_project_";
        private const string ProjectListKey = "zilf_project_list";
        
        private readonly JSInterop jsInterop;

        public ProjectStorageService(JSInterop jsInterop)
        {
            this.jsInterop = jsInterop;
        }

        /// <summary>
        /// Gets a list of all saved project metadata (ID and name).
        /// </summary>
        public async Task<List<ProjectMetadata>> GetProjectListAsync()
        {
            try
            {
                var json = await jsInterop.GetLocalStorageAsync(ProjectListKey);
                if (string.IsNullOrEmpty(json))
                {
                    return new List<ProjectMetadata>();
                }
                
                return JsonSerializer.Deserialize<List<ProjectMetadata>>(json) ?? new List<ProjectMetadata>();
            }
            catch
            {
                return new List<ProjectMetadata>();
            }
        }

        /// <summary>
        /// Saves a project to local storage.
        /// </summary>
        public async Task SaveProjectAsync(Project project)
        {
            var key = GetProjectKey(project.Guid);
            var serialized = SerializeProject(project);
            await jsInterop.SetLocalStorageAsync(key, serialized);
            
            // Update project list
            var projectList = await GetProjectListAsync();
            var existing = projectList.FirstOrDefault(p => p.Id == project.Guid);
            if (existing != null)
            {
                existing.Name = project.Name;
                existing.LastModified = DateTime.UtcNow;
            }
            else
            {
                projectList.Add(new ProjectMetadata
                {
                    Id = project.Guid,
                    Name = project.Name,
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                });
            }
            
            var listJson = JsonSerializer.Serialize(projectList);
            await jsInterop.SetLocalStorageAsync(ProjectListKey, listJson);
        }

        /// <summary>
        /// Loads a project from local storage.
        /// </summary>
        public async Task<Project?> LoadProjectAsync(Guid projectId)
        {
            try
            {
                var key = GetProjectKey(projectId);
                var json = await jsInterop.GetLocalStorageAsync(key);
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }
                
                return DeserializeProject(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes a project from local storage.
        /// </summary>
        public async Task DeleteProjectAsync(Guid projectId)
        {
            var key = GetProjectKey(projectId);
            await jsInterop.RemoveLocalStorageAsync(key);
            
            // Update project list
            var projectList = await GetProjectListAsync();
            projectList.RemoveAll(p => p.Id == projectId);
            
            var listJson = JsonSerializer.Serialize(projectList);
            await jsInterop.SetLocalStorageAsync(ProjectListKey, listJson);
        }

        /// <summary>
        /// Renames a project in the project list.
        /// </summary>
        public async Task RenameProjectAsync(Guid projectId, string newName)
        {
            var project = await LoadProjectAsync(projectId);
            if (project != null)
            {
                project.Name = newName;
                await SaveProjectAsync(project);
            }
        }

        private static string GetProjectKey(Guid projectId) => $"{StorageKeyPrefix}{projectId:N}";

        private static string SerializeProject(Project project)
        {
            var dto = new ProjectDto
            {
                Guid = project.Guid,
                Name = project.Name,
                Files = project.Files.Select(f => new FileDto
                {
                    Path = f.Path,
                    Content = f.Content
                }).ToList(),
                MainFilePath = project.MainFile?.Path ?? "",
                Includes = project.Includes.ToList()
            };
            
            return JsonSerializer.Serialize(dto);
        }

        private static Project? DeserializeProject(string json)
        {
            var dto = JsonSerializer.Deserialize<ProjectDto>(json);
            if (dto == null) return null;
            
            var project = new Project
            {
                Guid = dto.Guid,
                Name = dto.Name
            };
            
            foreach (var fileDto in dto.Files)
            {
                var file = project.AddFile(fileDto.Path);
                file.Content = fileDto.Content;
            }
            
            foreach (var include in dto.Includes)
            {
                project.Includes.Add(include);
            }
            
            // Set the main file if specified
            if (!string.IsNullOrEmpty(dto.MainFilePath))
            {
                var mainFile = project.Files.FirstOrDefault(f => f.Path == dto.MainFilePath);
                if (mainFile != null)
                {
                    project.MainFile = mainFile;
                }
            }
            
            return project;
        }

        private class ProjectDto
        {
            public Guid Guid { get; set; }
            public string Name { get; set; } = "";
            public List<FileDto> Files { get; set; } = new();
            public string MainFilePath { get; set; } = "";
            public List<string> Includes { get; set; } = new();
        }

        private class FileDto
        {
            public string Path { get; set; } = "";
            public string Content { get; set; } = "";
        }
    }

    /// <summary>
    /// Metadata about a saved project (for the project list).
    /// </summary>
    public class ProjectMetadata
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
    }
}
