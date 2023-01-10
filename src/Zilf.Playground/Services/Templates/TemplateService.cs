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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Zilf.Playground.Services.Workspaces;

namespace Zilf.Playground.Services.Templates
{
    public sealed class TemplateService : IDisposable
    {
        private bool disposedValue;
        private readonly HttpClient http;
        private readonly Uri iconBaseUri;

        private readonly Task<ProjectTemplates> metadata;
        private readonly Dictionary<string, Task<string>> content = new();

        private const string PathPrefix = "/project_templates/";
        private const string CatalogPath = PathPrefix + "project_templates.json";

        public Uri? GetIconUri(Template template)
        {
            return template.Icon == null ? null : new Uri(iconBaseUri, template.Icon);
        }

        public TemplateService(HttpClient http)
        {
            this.http = http;
            iconBaseUri = new Uri(http.BaseAddress ?? throw new ArgumentException("Missing base address"), PathPrefix);
            metadata = LoadTemplatesAsync();
        }

        private async Task<ProjectTemplates> LoadTemplatesAsync()
        {
            return await http.GetFromJsonAsync<ProjectTemplates>(CatalogPath).ConfigureAwait(false)
                ?? throw new Exception("Unable to load template metadata");
        }

        public async Task<KeyValuePair<string, Template>[]> GetShowcaseAsync()
        {
            var m = await metadata.ConfigureAwait(false);
            return Array.ConvertAll(m.Showcase, name => new KeyValuePair<string, Template>(name, m.Templates[name]));
        }

        public async Task<IReadOnlyCollection<string>> GetTemplateNamesAsync()
        {
            return (await metadata.ConfigureAwait(false)).Templates.Keys;
        }

        public async Task<Project> CopyToNewProjectAsync(string templateName)
        {
            var m = await metadata.ConfigureAwait(false);
            var template = m.Templates[templateName];

            var files = new List<string>();
            files.AddRange(template.Files);

            if (template.Include != null)
            {
                files.AddRange(template.Include.SelectMany(
                    libraryName =>
                    {
                        if (m.Libraries == null || !m.Libraries.TryGetValue(libraryName, out var library))
                            throw new InvalidOperationException("Invalid library reference");

                        return library.Files;
                    }));
            }

            await Task.WhenAll(files.Select(path => CacheFileAsync(path))).ConfigureAwait(false);

            var project = new Project();

            foreach (var path in files)
                project.AddFile(path).Content = await content[path].ConfigureAwait(false);

            if (template.Include != null)
            {
                foreach (var lib in template.Include)
                    project.Includes.Add(lib);
            }

            return project;
        }

        private Task CacheFileAsync(string path)
        {
            if (!content.TryGetValue(path, out var httpTask))
            {
                httpTask = http.GetStringAsync(PathPrefix + path);
                content.Add(path, httpTask);
            }

            return httpTask;
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                http.Dispose();
                disposedValue = true;
            }
        }
    }
}
