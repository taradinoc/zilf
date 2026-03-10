using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ZilfPub.Syntax;

namespace ZilfPub;

internal sealed record WebsitePublishOptions(
    FileInfo StoryFile,
    DirectoryInfo? OutputDirectory,
    string? ProjectName,
    string AuthorName,
    string? Ifid,
    int? ReleaseId,
    FileInfo? CoverArtFile,
    string? DescriptionHtml,
    FileInfo? DescriptionFile,
    string[]? SourceFilenames,
    bool Overwrite,
    string? ThemeCssContent);

internal sealed record WebsitePublishResult(string OutputDirectory, WebsiteModel Model);

internal static class WebsitePublisher
{
    private const string BundledAssetManifestPrefix = "Static/";
    private static readonly Lazy<IReadOnlyDictionary<string, string>> BundledAssetResourceNames =
        new(CreateBundledAssetResourceNames);

    public static WebsitePublishResult Publish(WebsitePublishOptions options)
    {
        var storyInfo = StoryFileInspector.Inspect(options.StoryFile.FullName);
        var outputDirectory = ResolveOutputDirectory(options);
        var sourceFiles = ResolveSourceFiles(options.SourceFilenames);

        PrepareOutputDirectory(outputDirectory, options.Overwrite);
        CopyBundledAssets(outputDirectory, options.Overwrite);

        // If theme CSS content is provided, overwrite the default Bootstrap CSS
        if (options.ThemeCssContent is not null)
        {
            var themeCssPath = Path.Combine(outputDirectory, "vendor", "bootstrap", "bootstrap.min.css");
            WriteTextFile(themeCssPath, options.ThemeCssContent);
        }

        var storyOutputPath = CopyStoryFile(options.StoryFile.FullName, outputDirectory, options.Overwrite);
        var sourceArchiveUri = WriteSourceArchive(
            outputDirectory,
            options.StoryFile.Name,
            options.SourceFilenames);
        var coverArtUri = ResolveCoverArt(options, outputDirectory);
        var descriptionHtml = ResolveDescriptionHtml(options);
        var parchmentEmbeddedAssets = LoadParchmentEmbeddedAssets(storyInfo.ParchmentFormat);

        var model = new WebsiteModel
        {
            AuthorName = options.AuthorName,
            ProjectName = options.ProjectName ?? storyInfo.ProjectName ?? Path.GetFileNameWithoutExtension(options.StoryFile.Name),
            IFID = options.Ifid ?? storyInfo.Ifid,
            ReleaseId = options.ReleaseId ?? storyInfo.ReleaseId,
            SerialNumber = storyInfo.SerialNumber,
            CoverArtUri = coverArtUri,
            DescriptionHtml = descriptionHtml,
            StoryFileUri = MakeSiteRelativePath(outputDirectory, storyOutputPath),
            StoryFileFormat = storyInfo.DisplayFormat,
            StoryFileHasZilfVersion = storyInfo.HasZilfVersion,
            StoryFileParchmentFormat = storyInfo.ParchmentFormat,
            StoryFileSize = FormatFileSize(options.StoryFile.Length),
            SourceArchiveUri = sourceArchiveUri,
            StoryFileJsUri = $"embedded:{storyInfo.EmbeddedId}",
            StoryFileEmbeddedId = storyInfo.EmbeddedId,
            StoryFileEmbeddedData = storyInfo.EmbeddedData,
            ParchmentBootstrapModuleSource = LoadBundledTextAsset(Path.Combine("vendor", "parchment", "web.js")),
            ParchmentStylesheetSource = LoadAndRebaseParchmentStylesheet(),
            ParchmentJQuerySource = LoadBundledTextAsset(Path.Combine("vendor", "parchment", "jquery.min.js")),
            ParchmentLegacySupportScriptSource = LoadBundledTextAsset(Path.Combine("vendor", "parchment", "ie.js")),
            ParchmentWaitingImageDataUri =
                $"data:image/gif;base64,{Convert.ToBase64String(LoadBundledBinaryAsset(Path.Combine("vendor", "parchment", "waiting.gif")))}",
            ParchmentEmbeddedAssets = parchmentEmbeddedAssets,
            SourceFiles = sourceFiles,
        };

        WriteTextFile(Path.Combine(outputDirectory, "index.html"), new IndexPage(model).TransformText());
        WriteTextFile(Path.Combine(outputDirectory, "play.html"), new PlayPage(model).TransformText());

        foreach (var sourceFile in sourceFiles)
        {
            WriteTextFile(
                Path.Combine(outputDirectory, sourceFile.PageUri),
                new SourcePage(model, sourceFile).TransformText());
        }

        return new(outputDirectory, model);
    }

    private static string ResolveOutputDirectory(WebsitePublishOptions options)
    {
        if (options.OutputDirectory is not null)
        {
            return options.OutputDirectory.FullName;
        }

        var baseDirectory = options.StoryFile.DirectoryName ?? Environment.CurrentDirectory;
        var siteName = Path.GetFileNameWithoutExtension(options.StoryFile.Name) + "-site";
        return Path.Combine(baseDirectory, siteName);
    }

    private static void PrepareOutputDirectory(string outputDirectory, bool overwrite)
    {
        if (Directory.Exists(outputDirectory) && overwrite)
        {
            DeleteManagedOutput(outputDirectory, "index.html");
            DeleteManagedOutput(outputDirectory, "play.html");
            DeleteManagedOutput(outputDirectory, "style.css");
            DeleteManagedOutput(outputDirectory, "assets");
            DeleteManagedOutput(outputDirectory, "css");
            DeleteManagedOutput(outputDirectory, "vendor");
            DeleteManagedSourcePages(outputDirectory);
        }

        if (Directory.Exists(outputDirectory) && !overwrite)
        {
            var existingEntries = Directory.EnumerateFileSystemEntries(outputDirectory).Take(1).Any();
            if (existingEntries)
            {
                throw new InvalidOperationException(
                    $"Output directory '{outputDirectory}' already exists and is not empty. Use --overwrite to reuse it.");
            }
        }

        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(outputDirectory, "assets"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "assets", "story"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "assets", "images"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "assets", "source"));
    }

    private static void DeleteManagedOutput(string outputDirectory, string relativePath)
    {
        var targetPath = Path.Combine(outputDirectory, relativePath);

        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, recursive: true);
            return;
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
    }

    private static void DeleteManagedSourcePages(string outputDirectory)
    {
        foreach (var filePath in Directory.EnumerateFiles(outputDirectory, "source*.html", SearchOption.TopDirectoryOnly))
        {
            File.Delete(filePath);
        }
    }

    private static string CopyStoryFile(string storyFilePath, string outputDirectory, bool overwrite)
    {
        var targetPath = Path.Combine(outputDirectory, "assets", "story", Path.GetFileName(storyFilePath));
        File.Copy(storyFilePath, targetPath, overwrite: overwrite);
        return targetPath;
    }

    private static string? ResolveCoverArt(WebsitePublishOptions options, string outputDirectory)
    {
        if (options.CoverArtFile is null)
        {
            return null;
        }

        var targetPath = Path.Combine(outputDirectory, "assets", "images", Path.GetFileName(options.CoverArtFile.Name));
        File.Copy(options.CoverArtFile.FullName, targetPath, overwrite: options.Overwrite);
        return MakeSiteRelativePath(outputDirectory, targetPath);
    }

    private static string? ResolveDescriptionHtml(WebsitePublishOptions options)
    {
        if (options.DescriptionHtml is not null)
        {
            return options.DescriptionHtml;
        }

        if (options.DescriptionFile is null)
        {
            return null;
        }

        return File.ReadAllText(options.DescriptionFile.FullName);
    }

    private static string? WriteSourceArchive(
        string outputDirectory,
        string storyFilename,
        string[]? sourceFilenames)
    {
        if (sourceFilenames is not { Length: > 0 })
        {
            return null;
        }

        var projectName = Path.GetFileNameWithoutExtension(storyFilename);
        var archiveFileName = $"{projectName}-source.zip";
        var archivePath = Path.Combine(outputDirectory, "assets", "source", archiveFileName);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

        var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, index) in sourceFilenames.Select((path, index) => (path, index)))
        {
            var fileName = Path.GetFileName(path);
            var fallbackName = $"source-{index + 1}.txt";
            var archiveName = EnsureUniqueArchiveEntryName(SanitizeArchiveEntryName(fileName, fallbackName), usedEntryNames);

            var entry = archive.CreateEntry(archiveName, CompressionLevel.Optimal);
            using var input = File.OpenRead(path);
            using var output = entry.Open();
            input.CopyTo(output);
        }

        return MakeSiteRelativePath(outputDirectory, archivePath);
    }

    private static string SanitizeArchiveEntryName(string? candidate, string fallbackName)
    {
        var name = string.IsNullOrWhiteSpace(candidate) ? fallbackName : candidate;

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c =>
                c == '/' || c == '\\' || invalidChars.Contains(c)
                    ? '_'
                    : c)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? fallbackName : sanitized;
    }

    private static string EnsureUniqueArchiveEntryName(string fileName, ISet<string> usedNames)
    {
        if (usedNames.Add(fileName))
        {
            return fileName;
        }

        var extension = Path.GetExtension(fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);

        for (var duplicateIndex = 2; ; duplicateIndex++)
        {
            var candidate = $"{stem}-{duplicateIndex}{extension}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static string MakeSiteRelativePath(string rootDirectory, string filePath)
    {
        var relativePath = Path.GetRelativePath(rootDirectory, filePath);
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static void CopyBundledAssets(string outputDirectory, bool overwrite)
    {
        foreach (var relativePath in EnumerateBundledAssetPaths())
        {
            if (IsExcludedBundledAssetPath(relativePath))
            {
                continue;
            }

            var targetPath = Path.Combine(outputDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            WriteBinaryFile(targetPath, LoadBundledBinaryAsset(relativePath), overwrite);
        }
    }

    private static bool IsExcludedBundledAssetPath(string relativePath)
    {
        var normalizedPath = NormalizeAssetPath(relativePath);

        return normalizedPath.Equals("vendor/parchment", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith("vendor/parchment/", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateBundledAssetPaths()
    {
        return BundledAssetResourceNames.Value.Keys.Order(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> CreateBundledAssetResourceNames()
    {
        var assembly = typeof(WebsitePublisher).Assembly;
        var resources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var normalizedResourceName = NormalizeAssetPath(resourceName);
            if (!normalizedResourceName.StartsWith(BundledAssetManifestPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = normalizedResourceName[BundledAssetManifestPrefix.Length..];
            resources.Add(relativePath, resourceName);
        }

        if (resources.Count == 0)
        {
            throw new InvalidOperationException("Unable to locate the embedded website assets.");
        }

        return resources;
    }

    private static ParchmentEmbeddedAsset[] LoadParchmentEmbeddedAssets(string parchmentFormat)
    {
        string[] assetPaths = parchmentFormat switch
        {
            "zcode" =>
            [
                Path.Combine("vendor", "parchment", "web.js"),
                Path.Combine("vendor", "parchment", "bocfel.js"),
                Path.Combine("vendor", "parchment", "bocfel.wasm"),
                Path.Combine("vendor", "parchment", "glkaudio_bg.wasm"),
            ],
            "glulx" =>
            [
                Path.Combine("vendor", "parchment", "web.js"),
                Path.Combine("vendor", "parchment", "glulxe.js"),
                Path.Combine("vendor", "parchment", "glulxe.wasm"),
                Path.Combine("vendor", "parchment", "glkaudio_bg.wasm"),
            ],
            _ => throw new InvalidOperationException($"Unsupported Parchment format '{parchmentFormat}'."),
        };

        return assetPaths
            .Select(path => path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                ? new ParchmentEmbeddedAsset(Path.GetFileName(path), LoadBundledTextAsset(path), "text/plain")
                : new ParchmentEmbeddedAsset(
                    Path.GetFileName(path),
                    Convert.ToBase64String(LoadBundledBinaryAsset(path)),
                    "text/plain"))
            .ToArray();
    }

    private static string LoadBundledTextAsset(string relativePath)
    {
        using var stream = OpenBundledAssetStream(relativePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string LoadAndRebaseParchmentStylesheet()
    {
        var stylesheet = LoadBundledTextAsset(Path.Combine("vendor", "parchment", "web.css"));
        return stylesheet.Replace("../fonts/", "vendor/fonts/", StringComparison.Ordinal);
    }

    private static byte[] LoadBundledBinaryAsset(string relativePath)
    {
        using var stream = OpenBundledAssetStream(relativePath);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static Stream OpenBundledAssetStream(string relativePath)
    {
        var normalizedPath = NormalizeAssetPath(relativePath);

        if (!BundledAssetResourceNames.Value.TryGetValue(normalizedPath, out var resourceName))
        {
            throw new InvalidOperationException($"Unable to locate the embedded website asset '{normalizedPath}'.");
        }

        return typeof(WebsitePublisher).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Unable to open the embedded website asset '{normalizedPath}'.");
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static void WriteBinaryFile(string path, byte[] content, bool overwrite)
    {
        if (!overwrite && File.Exists(path))
        {
            throw new IOException($"The file '{path}' already exists.");
        }

        File.WriteAllBytes(path, content);
    }

    private static void WriteTextFile(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static WebsiteSourceFile[] ResolveSourceFiles(string[]? sourceFilenames)
    {
        if (sourceFilenames is not { Length: > 0 })
        {
            return [];
        }

        var displayNames = sourceFilenames.Select(path => Path.GetFileName(path) ?? path).ToArray();
        var duplicateDisplayNames = displayNames
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return sourceFilenames
            .Select((path, index) =>
            {
                var sourceText = File.ReadAllText(path);
                var highlightedHtml = TryHighlight(sourceText);

                return new WebsiteSourceFile(
                    DisplayName: duplicateDisplayNames.Contains(displayNames[index]) ? path : displayNames[index],
                    PageUri: index == 0 ? "source.html" : $"source-{index + 1}.html",
                    SourceText: sourceText,
                    HighlightedHtml: highlightedHtml);
            })
            .ToArray();
    }

    private static string TryHighlight(string sourceText)
    {
        try
        {
            return ZilSyntaxHighlighter.HighlightHtml(sourceText);
        }
        catch
        {
            return System.Net.WebUtility.HtmlEncode(sourceText);
        }
    }

    private static string FormatFileSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = size;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? FormattableString.Invariant($"{value:0} {units[unitIndex]}")
            : FormattableString.Invariant($"{value:0.0} {units[unitIndex]}");
    }
}

internal sealed record StoryFileInspection(
    string EmbeddedData,
    string EmbeddedId,
    string DisplayFormat,
    string Ifid,
    string? ProjectName,
    int ReleaseId,
    string SerialNumber,
    bool HasZilfVersion,
    string ParchmentFormat);

internal static partial class StoryFileInspector
{
    public static StoryFileInspection Inspect(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var filename = Path.GetFileName(path);

        if (IsBlorb(bytes))
        {
            return InspectBlorb(filename, bytes);
        }

        if (IsGlulx(bytes))
        {
            var zilfMetadata = TryReadGlulxZilfMetadata(bytes);
            var embeddedIfid = FindEmbeddedIfid(bytes);
            return new(
                EmbeddedData: Convert.ToBase64String(bytes),
                EmbeddedId: filename,
                DisplayFormat: "Glulx",
                Ifid: embeddedIfid ?? ComputeFallbackIfid("GLULX", bytes),
                ProjectName: Path.GetFileNameWithoutExtension(filename),
                ReleaseId: zilfMetadata.ReleaseId,
                SerialNumber: zilfMetadata.SerialNumber,
                HasZilfVersion: zilfMetadata.HasZilfVersion,
                ParchmentFormat: "glulx");
        }

        if (IsZCode(bytes))
        {
            var header = ReadZCodeHeader(bytes);
            var hasZilfVersion = HasZCodeZilfVersion(bytes);
            var embeddedIfid = FindEmbeddedIfid(bytes);
            return new(
                EmbeddedData: Convert.ToBase64String(bytes),
                EmbeddedId: filename,
                DisplayFormat: $"Z-code v{header.Version}",
                Ifid: embeddedIfid ?? ComputeFallbackIfid("ZCODE", bytes),
                ProjectName: Path.GetFileNameWithoutExtension(filename),
                ReleaseId: header.ReleaseId,
                SerialNumber: header.SerialNumber,
                HasZilfVersion: hasZilfVersion,
                ParchmentFormat: "zcode");
        }

        throw new InvalidOperationException(
            $"Unsupported story format for '{path}'. Expected a Z-code file, a Glulx file, or a Blorb container.");
    }

    private static StoryFileInspection InspectBlorb(string filename, byte[] bytes)
    {
        byte[]? executable = null;
        string? executableChunkId = null;
        BlorbMetadata? metadata = null;

        var offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            var chunkId = ReadFourCc(bytes, offset);
            var chunkLength = ReadInt32BigEndian(bytes, offset + 4);
            var chunkDataOffset = offset + 8;

            if (chunkLength < 0 || chunkDataOffset + chunkLength > bytes.Length)
            {
                break;
            }

            var chunkData = bytes.AsSpan(chunkDataOffset, chunkLength).ToArray();

            if ((chunkId == "ZCOD" || chunkId == "GLUL") && executable is null)
            {
                executable = chunkData;
                executableChunkId = chunkId;
            }
            else if (chunkId == "IFmd")
            {
                metadata = ParseBlorbMetadata(chunkData);
            }

            offset = chunkDataOffset + chunkLength;
            if ((chunkLength & 1) != 0)
            {
                offset++;
            }
        }

        var projectName = metadata?.Title ?? Path.GetFileNameWithoutExtension(filename);
        var ifid = metadata?.Ifid;
        var releaseId = DefaultReleaseId();
        var serialNumber = DefaultSerialNumber();
        var hasZilfVersion = false;
        var displayFormat = "Blorb";
        var parchmentFormat = "blorb";

        if (executableChunkId == "ZCOD" && executable is not null)
        {
            var header = ReadZCodeHeader(executable);
            hasZilfVersion = HasZCodeZilfVersion(executable);
            ifid ??= FindEmbeddedIfid(executable)
                ?? FindEmbeddedIfid(bytes)
                ?? BuildZCodeIfid(header.ReleaseId, header.SerialNumber);
            releaseId = header.ReleaseId;
            serialNumber = header.SerialNumber;

            displayFormat = "Z-code Blorb";
            parchmentFormat = "zcode";
        }
        else if (executableChunkId == "GLUL")
        {
            var zilfMetadata = TryReadGlulxZilfMetadata(executable ?? bytes);
            if (!zilfMetadata.HasZilfVersion && executable is not null)
            {
                zilfMetadata = TryReadGlulxZilfMetadata(bytes);
            }

            ifid ??= FindEmbeddedIfid(executable ?? bytes) ?? FindEmbeddedIfid(bytes) ?? ComputeFallbackIfid("GLULX", executable ?? bytes);
            releaseId = zilfMetadata.ReleaseId;
            serialNumber = zilfMetadata.SerialNumber;
            hasZilfVersion = zilfMetadata.HasZilfVersion;
            displayFormat = "Glulx Blorb";
            parchmentFormat = "glulx";
        }
        else
        {
            ifid ??= FindEmbeddedIfid(bytes) ?? ComputeFallbackIfid("BLORB", bytes);
        }

        return new(
            EmbeddedData: Convert.ToBase64String(bytes),
            EmbeddedId: filename,
            DisplayFormat: displayFormat,
            Ifid: ifid,
            ProjectName: projectName,
            ReleaseId: releaseId,
            SerialNumber: serialNumber,
            HasZilfVersion: hasZilfVersion,
            ParchmentFormat: parchmentFormat);
    }

    private static BlorbMetadata? ParseBlorbMetadata(byte[] bytes)
    {
        try
        {
            var document = XDocument.Parse(Encoding.UTF8.GetString(bytes));
            return new(
                Title: FindFirstValue(document, "title"),
                Ifid: FindFirstValue(document, "ifid"),
                Author: FindFirstValue(document, "author"));
        }
        catch
        {
            return null;
        }
    }

    private static string? FindFirstValue(XDocument document, string localName)
    {
        return document
            .Descendants()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
            ?.Value
            .Trim();
    }

    private static bool IsBlorb(byte[] bytes)
    {
        return bytes.Length >= 12
            && ReadFourCc(bytes, 0) == "FORM"
            && ReadFourCc(bytes, 8) == "IFRS";
    }

    private static bool IsGlulx(byte[] bytes)
    {
        return bytes.Length >= 4 && ReadFourCc(bytes, 0) == "Glul";
    }

    private static bool IsZCode(byte[] bytes)
    {
        return bytes.Length >= 64 && bytes[0] is >= 1 and <= 8;
    }

    private static ZCodeHeader ReadZCodeHeader(byte[] bytes)
    {
        if (bytes.Length < 30)
        {
            throw new InvalidOperationException("The Z-code file is too short to contain a valid header.");
        }

        var serialBytes = bytes.AsSpan(18, 6).ToArray();
        var serial = Encoding.ASCII.GetString(serialBytes).TrimEnd('\0', ' ');
        if (string.IsNullOrWhiteSpace(serial))
        {
            serial = DefaultSerialNumber();
        }

        return new(
            Version: bytes[0],
            ReleaseId: ReadUInt16BigEndian(bytes, 2),
            SerialNumber: serial.ToUpperInvariant());
    }

    private static bool HasZCodeZilfVersion(byte[] bytes)
    {
        return TryReadZilfVersionAtOffset(bytes, 0x38, out _);
    }

    private static StoryMetadata TryReadGlulxZilfMetadata(byte[] bytes)
    {
        if (!TryFindZilfVersionOffset(bytes, out var offset))
        {
            return new(DefaultReleaseId(), DefaultSerialNumber(), HasZilfVersion: false);
        }

        var releaseOffset = offset + 8;
        if (releaseOffset + 8 > bytes.Length)
        {
            return new(DefaultReleaseId(), DefaultSerialNumber(), HasZilfVersion: true);
        }

        var releaseId = ReadUInt16BigEndian(bytes, releaseOffset);
        var serialBytes = bytes.AsSpan(releaseOffset + 2, 6).ToArray();
        var serialNumber = Encoding.ASCII.GetString(serialBytes).TrimEnd('\0', ' ').ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            serialNumber = DefaultSerialNumber();
        }

        return new(releaseId, serialNumber, HasZilfVersion: true);
    }

    private static bool TryFindZilfVersionOffset(byte[] bytes, out int offset)
    {
        for (var i = 0; i <= bytes.Length - 8; i++)
        {
            if (TryReadZilfVersionAtOffset(bytes, i, out _))
            {
                offset = i;
                return true;
            }
        }

        offset = -1;
        return false;
    }

    private static bool TryReadZilfVersionAtOffset(byte[] bytes, int offset, out string version)
    {
        if (offset < 0 || offset + 8 > bytes.Length)
        {
            version = string.Empty;
            return false;
        }

        if (bytes[offset] != (byte)'Z'
            || bytes[offset + 1] != (byte)'I'
            || bytes[offset + 2] != (byte)'L'
            || bytes[offset + 3] != (byte)'F')
        {
            version = string.Empty;
            return false;
        }

        for (var i = 4; i <= 6; i++)
        {
            if (!char.IsAsciiLetterOrDigit((char)bytes[offset + i]))
            {
                version = string.Empty;
                return false;
            }
        }

        var suffix = (char)bytes[offset + 7];
        if (suffix is not ('a' or 'b' or 'c' or '~'))
        {
            version = string.Empty;
            return false;
        }

        version = Encoding.ASCII.GetString(bytes, offset, 8);
        return true;
    }

    private static ushort ReadUInt16BigEndian(byte[] bytes, int offset)
    {
        return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
    }

    private static int ReadInt32BigEndian(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24)
            | (bytes[offset + 1] << 16)
            | (bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    private static string ReadFourCc(byte[] bytes, int offset)
    {
        return Encoding.ASCII.GetString(bytes, offset, 4);
    }

    private static string? FindEmbeddedIfid(byte[] bytes)
    {
        var text = Encoding.ASCII.GetString(bytes);
        var match = GetUuidRegex().Match(text);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    [GeneratedRegex(@"UUID://([A-Za-z0-9\-]+)//", RegexOptions.CultureInvariant)]
    private static partial Regex GetUuidRegex();

    private static string BuildZCodeIfid(int releaseId, string serialNumber)
    {
        return $"ZCODE-{releaseId}-{serialNumber}";
    }

    private static string ComputeFallbackIfid(string prefix, byte[] bytes)
    {
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        return $"{prefix}-{hash[..12]}";
    }

    private static string DefaultSerialNumber()
    {
        return DateTime.UtcNow.ToString("yyMMdd", CultureInfo.InvariantCulture);
    }

    private static int DefaultReleaseId()
    {
        return 1;
    }

    private sealed record BlorbMetadata(string? Title, string? Ifid, string? Author);

    private sealed record ZCodeHeader(byte Version, int ReleaseId, string SerialNumber);

    private sealed record StoryMetadata(int ReleaseId, string SerialNumber, bool HasZilfVersion);
}