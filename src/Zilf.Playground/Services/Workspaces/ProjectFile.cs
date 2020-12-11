namespace Zilf.Playground.Services.Workspaces
{
    public sealed class ProjectFile
    {
        public string Path { get; }
        public string Content { get; set; }

        public ProjectFile(string path) : this(path, "") { }

        public ProjectFile(string path, string content)
        {
            this.Path = path;
            this.Content = content;
        }
    }
}
