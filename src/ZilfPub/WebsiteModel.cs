namespace ZilfPub;

public sealed record ParchmentEmbeddedAsset(string Id, string Content, string Type);

public sealed record WebsiteSourceFile(string DisplayName, string PageUri, string SourceText, string HighlightedHtml);

public class WebsiteModel
{
    public required string AuthorName { get; init; }
    public required string ProjectName { get; init; }
    public required string IFID { get; init; }
    public required int ReleaseId { get; init; }
    public required string SerialNumber { get; init; }
    public string? CoverArtUri { get; init; }
    public string? DescriptionHtml { get; init; }

    public required string StoryFileUri { get; init; }
    public required string StoryFileFormat { get; init; }
    public required bool StoryFileHasZilfVersion { get; init; }
    public required string StoryFileParchmentFormat { get; init; }
    public required string StoryFileSize { get; init; }
    public string? SourceArchiveUri { get; init; }

    public required string StoryFileJsUri { get; init; }
    public required string StoryFileEmbeddedId { get; init; }
    public required string StoryFileEmbeddedData { get; init; }
    public required string ParchmentBootstrapModuleSource { get; init; }
    public required string ParchmentStylesheetSource { get; init; }
    public required string ParchmentJQuerySource { get; init; }
    public required string ParchmentLegacySupportScriptSource { get; init; }
    public required string ParchmentWaitingImageDataUri { get; init; }
    public required ParchmentEmbeddedAsset[] ParchmentEmbeddedAssets { get; init; }

    public WebsiteSourceFile[] SourceFiles { get; init; } = [];

    public bool HasSourceFiles => SourceFiles.Length > 0;
}
