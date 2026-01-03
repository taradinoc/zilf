namespace Zilf.Language
{
    sealed class FileSourceSpan : ISourceSpan
    {
        public FileSourceSpan(string fileName, int startLine, int startColumn, int endLine, int endColumn)
        {
            FileName = fileName;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public string SourceInfo => $"{FileName}:{StartLine}";

        public string FileName { get; }

        public int StartLine { get; }

        public int StartColumn { get; }

        public int EndLine { get; }

        public int EndColumn { get; }

        public override string ToString() => SourceInfo;
    }
}
