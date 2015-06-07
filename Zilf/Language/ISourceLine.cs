namespace Zilf.Language
{
    /// <summary>
    /// Provides properties for describing the source of an error.
    /// </summary>
    interface ISourceLine
    {
        string SourceInfo { get; }
    }
}