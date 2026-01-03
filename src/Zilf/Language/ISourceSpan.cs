namespace Zilf.Language
{
    /// <summary>
    /// Provides a file-backed source span (start/end line/column) for IDE features.
    /// </summary>
    public interface ISourceSpan : ISourceLine
    {
        string FileName { get; }
        int StartLine { get; }
        int StartColumn { get; }
        int EndLine { get; }
        int EndColumn { get; }
    }
}
