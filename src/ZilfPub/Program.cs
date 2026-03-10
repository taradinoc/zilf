using System.CommandLine;

namespace ZilfPub;

internal static class Program
{
	private static readonly Lazy<WebsiteCommandSpec> CommandSpec = new(CreateCommandSpec);

	public static Task<int> Main(string[] args)
	{
		var spec = CommandSpec.Value;
		return spec.RootCommand.Parse(args).InvokeAsync();
	}

	private static WebsiteCommandSpec CreateCommandSpec()
	{
		var storyFileArgument = new Argument<string?>("story-file")
		{
			Description = "The story file to publish (.z#, .ulx, .zblorb, or .gblorb).",
			Arity = ArgumentArity.ZeroOrOne,
		};

		var outputDirOption = new Option<DirectoryInfo?>("--output-dir")
		{
			Description = "The directory to write the generated website into."
		};
		outputDirOption.Aliases.Add("-o");

		var projectNameOption = CreateOption<string?>("--project-name",
			"The project title shown on the generated pages. Defaults to story metadata or the story filename.");
		var authorNameOption = CreateOption<string?>("--author-name", "The author name shown on the generated pages.");
		var ifidOption = CreateOption<string?>("--ifid", "Override the IFID shown on the home page.");
		var releaseIdOption = CreateOption<int?>("--release-id", "Override the release number shown on the home page.");
		var coverArtOption = CreateOption<FileInfo?>("--cover-art", "An optional cover image file to copy into the published site.");
		var descriptionHtmlOption = CreateOption<string?>("--description", "Optional HTML fragment shown on the home page.");
		var descriptionFileOption = CreateOption<FileInfo?>("--description-file", "Read the optional description HTML fragment from a file.");
		var sourceFilenamesOption = CreateOption<string[]>("--source-filename", "Source files to publish as source text pages.");
		sourceFilenamesOption.AllowMultipleArgumentsPerToken = true;
		var sourceDirectoryOption = CreateOption<DirectoryInfo[]>("--source-dir",
			"A directory whose top-level .zil and .mud files should be published as source text pages.");
		sourceDirectoryOption.AllowMultipleArgumentsPerToken = true;
		var overwriteOption = CreateOption<bool>("--overwrite", "Allow overwriting files in an existing output directory.");
		var listThemesOption = CreateOption<bool>("--list-themes", "List available Bootswatch themes and exit.");
		var themeOption = CreateOption<string?>("--theme", "Use a Bootswatch theme by name (e.g., darkly, cosmo, united).");

		var rootCommand = new RootCommand("Generate a static website for a ZILF story file.")
		{
			TreatUnmatchedTokensAsErrors = true,
		};

		rootCommand.Arguments.Add(storyFileArgument);
		rootCommand.Options.Add(outputDirOption);
		rootCommand.Options.Add(projectNameOption);
		rootCommand.Options.Add(authorNameOption);
		rootCommand.Options.Add(ifidOption);
		rootCommand.Options.Add(releaseIdOption);
		rootCommand.Options.Add(coverArtOption);
		rootCommand.Options.Add(descriptionHtmlOption);
		rootCommand.Options.Add(descriptionFileOption);
		rootCommand.Options.Add(sourceFilenamesOption);
		rootCommand.Options.Add(sourceDirectoryOption);
		rootCommand.Options.Add(overwriteOption);
		rootCommand.Options.Add(listThemesOption);
		rootCommand.Options.Add(themeOption);

		var spec = new WebsiteCommandSpec(
			rootCommand,
			storyFileArgument,
			outputDirOption,
			projectNameOption,
			authorNameOption,
			ifidOption,
			releaseIdOption,
			coverArtOption,
			descriptionHtmlOption,
			descriptionFileOption,
			sourceFilenamesOption,
			sourceDirectoryOption,
			overwriteOption,
			listThemesOption,
			themeOption);

		rootCommand.SetAction(parseResult => Execute(parseResult, spec));
		return spec;
	}

	private static int Execute(ParseResult parseResult, WebsiteCommandSpec spec)
	{
		// Handle --list-themes first, as it doesn't require a story file
		var listThemes = parseResult.GetValue(spec.ListThemesOption);
		if (listThemes)
		{
			return ExecuteListThemes();
		}

		var storyFilePath = parseResult.GetValue(spec.StoryFileArgument);
		if (string.IsNullOrWhiteSpace(storyFilePath))
		{
			Console.Error.WriteLine("A story file path is required.");
			return 1;
		}

		var storyFile = new FileInfo(storyFilePath);
		var outputDir = parseResult.GetValue(spec.OutputDirOption);
		var projectName = parseResult.GetValue(spec.ProjectNameOption);
		var authorName = parseResult.GetValue(spec.AuthorNameOption) ?? "Anonymous";
		var ifid = parseResult.GetValue(spec.IfidOption);
		var releaseId = parseResult.GetValue(spec.ReleaseIdOption);
		var coverArt = parseResult.GetValue(spec.CoverArtOption);
		var descriptionHtml = parseResult.GetValue(spec.DescriptionHtmlOption);
		var descriptionFile = parseResult.GetValue(spec.DescriptionFileOption);
		var sourceFilenames = ExpandSourceFiles(
			parseResult.GetValue(spec.SourceFilenamesOption),
			parseResult.GetValue(spec.SourceDirectoryOption),
			out var sourceErrorMessage);
		var overwrite = parseResult.GetValue(spec.OverwriteOption);
		var themeName = parseResult.GetValue(spec.ThemeOption);

		if (!storyFile.Exists)
		{
			Console.Error.WriteLine($"Story file not found: {storyFile.FullName}");
			return 1;
		}

		if (coverArt is { Exists: false })
		{
			Console.Error.WriteLine($"Cover art file not found: {coverArt.FullName}");
			return 1;
		}

		if (descriptionHtml is not null && descriptionFile is not null)
		{
			Console.Error.WriteLine("Specify either --description or --description-file, not both.");
			return 1;
		}

		if (descriptionFile is { Exists: false })
		{
			Console.Error.WriteLine($"Description file not found: {descriptionFile.FullName}");
			return 1;
		}

		if (sourceErrorMessage is not null)
		{
			Console.Error.WriteLine(sourceErrorMessage);
			return 1;
		}

		// Resolve theme if specified
		string? themeCssContent = null;
		if (!string.IsNullOrWhiteSpace(themeName))
		{
			try
			{
				var theme = BootswatchThemeService.FetchThemeByNameAsync(themeName).GetAwaiter().GetResult();
				if (theme is null)
				{
					Console.Error.WriteLine($"Theme '{themeName}' not found. Use --list-themes to see available themes.");
					return 1;
				}
				Console.WriteLine($"Resolving theme '{theme.Name}'...");
				themeCssContent = BootswatchThemeService.DownloadThemeCssAsync(theme).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to fetch theme: {ex.Message}");
				return 1;
			}
		}

		try
		{
			var options = new WebsitePublishOptions(
				StoryFile: storyFile,
				OutputDirectory: outputDir,
				ProjectName: projectName,
				AuthorName: authorName,
				Ifid: ifid,
				ReleaseId: releaseId,
				CoverArtFile: coverArt,
				DescriptionHtml: descriptionHtml,
				DescriptionFile: descriptionFile,
				SourceFilenames: sourceFilenames,
				Overwrite: overwrite,
				ThemeCssContent: themeCssContent);

			var result = WebsitePublisher.Publish(options);
			Console.WriteLine($"Created website in {result.OutputDirectory}");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return 1;
		}
	}

	private static int ExecuteListThemes()
	{
		try
		{
			var apiResponse = BootswatchThemeService.FetchThemesAsync().GetAwaiter().GetResult();
			Console.WriteLine($"Available Bootswatch themes (Bootstrap {apiResponse.Version}):");
			Console.WriteLine();

			foreach (var theme in apiResponse.Themes)
			{
				Console.WriteLine($"  {theme.Name,-15} {theme.Description}");
			}

			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Failed to fetch themes: {ex.Message}");
			return 1;
		}
	}

	private static Option<T> CreateOption<T>(string name, string description)
	{
		return new(name)
		{
			Description = description
		};
	}

	private static string[]? ExpandSourceFiles(
		string[]? sourceFilenames,
		DirectoryInfo[]? sourceDirectories,
		out string? errorMessage)
	{
		errorMessage = null;
		var results = new List<string>();
		var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (sourceFilenames is not null)
		{
			foreach (var sourceFilename in sourceFilenames)
			{
				if (string.IsNullOrWhiteSpace(sourceFilename))
				{
					continue;
				}

				var file = new FileInfo(sourceFilename);
				if (!file.Exists)
				{
					errorMessage = $"Source file not found: {file.FullName}";
					return null;
				}

				AddSourcePath(results, seenPaths, file.FullName);
			}
		}

		if (sourceDirectories is not null)
		{
			foreach (var sourceDirectory in sourceDirectories)
			{
				if (!sourceDirectory.Exists)
				{
					errorMessage = $"Source directory not found: {sourceDirectory.FullName}";
					return null;
				}

				var directoryFiles = Directory
					.EnumerateFiles(sourceDirectory.FullName, "*", SearchOption.TopDirectoryOnly)
					.Where(IsSupportedSourceFile)
					.OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

				foreach (var filePath in directoryFiles)
				{
					AddSourcePath(results, seenPaths, Path.GetFullPath(filePath));
				}
			}
		}

		return results.Count > 0 ? results.ToArray() : null;
	}

	private static void AddSourcePath(List<string> results, HashSet<string> seenPaths, string filePath)
	{
		if (seenPaths.Add(filePath))
		{
			results.Add(filePath);
		}
	}

	private static bool IsSupportedSourceFile(string path)
	{
		var extension = Path.GetExtension(path);
		return extension.Equals(".zil", StringComparison.OrdinalIgnoreCase)
			|| extension.Equals(".mud", StringComparison.OrdinalIgnoreCase);
	}

	private sealed record WebsiteCommandSpec(
		RootCommand RootCommand,
		Argument<string?> StoryFileArgument,
		Option<DirectoryInfo?> OutputDirOption,
		Option<string?> ProjectNameOption,
		Option<string?> AuthorNameOption,
		Option<string?> IfidOption,
		Option<int?> ReleaseIdOption,
		Option<FileInfo?> CoverArtOption,
		Option<string?> DescriptionHtmlOption,
		Option<FileInfo?> DescriptionFileOption,
		Option<string[]> SourceFilenamesOption,
		Option<DirectoryInfo[]> SourceDirectoryOption,
		Option<bool> OverwriteOption,
		Option<bool> ListThemesOption,
		Option<string?> ThemeOption);
}

