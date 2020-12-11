#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zilf.Playground.Services.Templates
{
    public class ProjectTemplates
    {
        [JsonPropertyName("showcase")]
        public string[] Showcase { get; set; } = default!;

        [JsonPropertyName("templates")]
        public Dictionary<string, Template> Templates { get; set; } = default!;

        [JsonPropertyName("libraries")]
        public Dictionary<string, Library>? Libraries { get; set; }
    }

    public class Template
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("description")]
        public string Description { get; set; } = default!;

        [JsonPropertyName("features")]
        public string[]? Features { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("files")]
        public string[] Files { get; set; } = default!;

        [JsonPropertyName("include")]
        public string[]? Include { get; set; }
    }

    public class Library
    {
        [JsonPropertyName("files")]
        public string[] Files { get; set; } = default!;
    }
}
