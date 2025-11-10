using Microsoft.AspNetCore.Mvc;
using Zilf.Compiler;
using Zilf.Common;
using System.Text;

namespace Zilf.CompilerService;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();

        // no Swagger by default to keep dependencies minimal

        // Set up work directories early for monitoring endpoint
        var workRoot = Environment.GetEnvironmentVariable("ZILF_WORK_ROOT") ?? "/work"; // configurable via env
        var stashRoot = Path.Combine(workRoot, "Stash");
        var projectsRoot = Path.Combine(workRoot, "Projects");
        Directory.CreateDirectory(stashRoot);
        Directory.CreateDirectory(projectsRoot);

        // Monitoring endpoints
        app.MapGet("/monitoring/ping", () => Results.Ok("pong"));

        app.MapGet("/monitoring/storage", () =>
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(workRoot) ?? "/");
            var usedPercent = (1.0 - (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize) * 100;
            var threshold = double.TryParse(Environment.GetEnvironmentVariable("DISKSPACE_ALERT_THRESHOLD"), out var t) ? t : 99.0;

            if (usedPercent >= threshold)
                return Results.StatusCode(500);

            return Results.Ok(new { status = "ok", usedPercent = Math.Round(usedPercent, 2), threshold });
        });

        app.MapGet("/monitoring/version", () => Results.Ok(new
        {
            version = GetZilfVersion(),
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription
        }));

        static bool IsSafeId(string id) => id.Length > 0 && id.Length <= 100 && id.All(c => char.IsLetterOrDigit(c) || c == '-');

        static bool TryGetSafeFilePath(string baseDir, string relativePath, out string? safePath)
        {
            safePath = null;
            
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            // Normalize path separators
            var normalized = relativePath.Trim().Replace('\\', '/');
            
            // Reject traversal attempts and rooted paths
            if (normalized.StartsWith("../") || normalized.Contains("/../") || Path.IsPathRooted(normalized))
                return false;
            
            // Strip leading slash if present
            if (normalized.StartsWith('/'))
                normalized = normalized[1..];
            
            // Convert to platform-specific separators and build full path
            var platformPath = normalized.Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(baseDir, platformPath));
            var baseDirFull = Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar;
            
            // Ensure the resolved path is within the base directory
            if (!candidate.StartsWith(baseDirFull, StringComparison.OrdinalIgnoreCase))
                return false;
            
            safePath = candidate;
            return true;
        }

        app.MapPost("/prepare", (PrepareRequest req) =>
        {
            var jobId = (req.data.sessionId ?? string.Empty).Trim();
            if (!IsSafeId(jobId)) return Results.BadRequest(new { error = "Invalid session id" });

            var projectDir = Path.Combine(projectsRoot, jobId);
            Directory.CreateDirectory(projectDir);

            foreach (var f in req.included)
            {
                if (f.type != "file" || f.attributes is null)
                    return Results.BadRequest(new { error = "Invalid file entry" });

                var targetDir = Path.Combine(projectDir, f.attributes.directory ?? string.Empty);
                Directory.CreateDirectory(targetDir);
                var filePath = Path.Combine(targetDir, f.attributes.name);
                File.WriteAllText(filePath, f.attributes.contents ?? string.Empty, new UTF8Encoding(false));
            }

            return Results.Ok(new PrepareResponse(jobId));
        });

        app.MapGet("/compile/{jobId}", async Task<IResult> (string jobId, [FromQuery] string? mainFile, [FromQuery] bool? debug) =>
        {
            if (string.IsNullOrWhiteSpace(jobId) || !IsSafeId(jobId))
                return Results.BadRequest(new { error = "Invalid job id" });

            var projectDir = Path.Combine(projectsRoot, jobId);
            if (!Directory.Exists(projectDir)) return Results.BadRequest(new { error = "Unknown job id" });

            // Determine main file: if caller provided one, use it; else prefer story.zil; else first *.zil
            string? selectedMainFile;
            if (!string.IsNullOrWhiteSpace(mainFile))
            {
                if (!TryGetSafeFilePath(projectDir, mainFile, out var safePath))
                    return Results.BadRequest(new { error = "Invalid mainFile path" });
                if (!File.Exists(safePath))
                    return Results.BadRequest(new { error = "Specified mainFile not found" });
                selectedMainFile = safePath;
            }
            else
            {
                selectedMainFile = Directory.EnumerateFiles(projectDir, "story.zil", SearchOption.AllDirectories).FirstOrDefault()
                    ?? Directory.EnumerateFiles(projectDir, "*.zil", SearchOption.AllDirectories).FirstOrDefault();
            }

            if (selectedMainFile is null) return Results.BadRequest(new { error = "No ZIL source found" });

            // Determine output zap path; keep relative subdirectory structure by placing next to the main file
            var zapPath = Path.ChangeExtension(selectedMainFile, ".zap");

            // Set up FrontEnd
            var logSb = new StringBuilder();
            var fe = new FrontEnd
            {
                FileSystem = PhysicalFileSystem.Instance,
                Logger = new Zilf.Diagnostics.DefaultDiagnosticLogger { Writer = new StringWriter(logSb) }
            };
            fe.IncludePaths.Add(Path.GetDirectoryName(selectedMainFile)!);
            fe.IncludePaths.Add(Path.GetFullPath("zillib")); // library path if mounted/copied

            var result = fe.Compile(selectedMainFile, zapPath, wantDebugInfo: debug ?? false);

            var messages = result.Diagnostics.Select(d => new Message(
                d.Severity.ToString().ToLowerInvariant(), d.Code, d.ToString())).ToArray();

            string? storyPath = null;
            if (result.Success)
            {
                try
                {
                    // Assemble to temporary .z# and then detect produced story file(s)
                    var assembler = new Zapf.ZapfAssembler { FileSystem = PhysicalFileSystem.Instance };
                    var tempOut = Path.ChangeExtension(zapPath, ".z#");
                    var asmResult = assembler.Assemble(zapPath, tempOut);
                    if (asmResult.Success)
                    {
                        // Look for any .z3/.z5/.z8 produced alongside zap
                        var produced = Directory.EnumerateFiles(projectDir, "*.z?", SearchOption.AllDirectories)
                            .Where(p => p.EndsWith(".z3", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".z5", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".z8", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(p => p) // deterministic
                            .ToList();
                        if (produced.Count == 1)
                        {
                            storyPath = produced[0];
                        }
                        else if (produced.Count > 1)
                        {
                            // If multiple, choose the one whose base matches main file name, else first
                            var baseName = Path.GetFileNameWithoutExtension(selectedMainFile);
                            storyPath = produced.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == baseName) ?? produced.First();
                        }
                        else
                        {
                            // Fallback: if tempOut got renamed or exists, try it
                            if (File.Exists(tempOut)) storyPath = tempOut;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logSb.AppendLine(ex.ToString());
                }
            }

            var stashDir = Path.Combine(stashRoot, jobId);
            Directory.CreateDirectory(stashDir);
            var logText = logSb.ToString();
            await File.WriteAllTextAsync(Path.Combine(stashDir, "compile.log"), logText, new UTF8Encoding(false));
            string? storyRel = null;
            if (storyPath != null && File.Exists(storyPath))
            {
                var dest = Path.Combine(stashDir, Path.GetFileName(storyPath));
                File.Copy(storyPath, dest, overwrite: true);
                storyRel = $"/results/{jobId}/storyfile/{Path.GetFileName(dest)}";
            }

            return Results.Ok(new CompileResponse(result.Success, jobId, storyRel, logText, messages));
        });

        app.MapGet("/results/{jobId}/storyfile/{file}", (string jobId, string file) =>
        {
            if (!IsSafeId(jobId)) return Results.BadRequest("Invalid job id");
            
            var stashDir = Path.Combine(stashRoot, jobId);
            if (!TryGetSafeFilePath(stashDir, file, out var safePath))
                return Results.BadRequest("Invalid file path");
            
            if (!File.Exists(safePath)) return Results.NotFound();
            return Results.File(safePath, "application/octet-stream", file);
        });

        app.MapGet("/results/{jobId}/log", (string jobId) =>
        {
            if (!IsSafeId(jobId)) return Results.BadRequest("Invalid job id");
            var path = Path.Combine(stashRoot, jobId, "compile.log");
            if (!File.Exists(path)) return Results.NotFound();
            return Results.File(path, "text/plain", enableRangeProcessing: false);
        });

        app.Run();
    }

    private static string GetZilfVersion()
    {
        var asm = typeof(FrontEnd).Assembly;
        var info = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(asm);
        return info?.InformationalVersion ?? asm.GetName().Version?.ToString() ?? "unknown";
    }

    // Data contracts
    public record PrepareRequest(PrepareData data, ProjectFile[] included);
    public record PrepareData(string sessionId, string? language, bool? debug, string? uuid);
    public record ProjectFile(string type, ProjectFileAttributes attributes);
    public record ProjectFileAttributes(string name, string directory, string contents);
    public record PrepareResponse(string jobId);
    public record CompileRequest(string jobId, string? mainFile, bool? debug);
    public record Message(string severity, string code, string text);
    public record CompileResponse(bool success, string jobId, string? storyfileUrl, string log, Message[] messages);
}
