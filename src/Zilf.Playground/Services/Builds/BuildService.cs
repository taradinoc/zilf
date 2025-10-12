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

using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zapf;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Playground.Services.Workspaces;

namespace Zilf.Playground.Services.Builds
{
    public enum BuildStatus
    {
        NotBuilt,
        CompilePending, // New: set as soon as build is requested
        Compiling,
        CompilerError,
        Assembling,
        AssemblerError,
        Built,
    }

    public class BackgroundBuildWorker
    {
        public event EventHandler<int>? StatusUpdate;
        public event EventHandler<string>? OutputMessage;

        private static string GetZapPath(string zilPath)
        {
            const string OutputPrefix = "_build/";
            var zilFileName = zilPath[(zilPath.LastIndexOf('/') + 1)..];
            return OutputPrefix + System.IO.Path.ChangeExtension(zilFileName, ".zap");
        }

        private void LogMessage(string message)
        {
            OutputMessage?.Invoke(this, message);
        }

        public (string[] newPaths, string[] newContentsBase64)? Build(string[] filePaths, string[] fileContents, string[] includePaths, string mainFilePath)
        {
            // set up filesystem
            var fileSystem = new InMemoryFileSystem();

            for (int i = 0; i < filePaths.Length; i++)
                fileSystem.SetText(filePaths[i], fileContents[i]);

            var zapPath = GetZapPath(mainFilePath);

            // Create custom logger that fires events instead of writing to Console
            var logger = new EventDiagnosticLogger();
            logger.DiagnosticLogged += (_, msg) => LogMessage(msg);

            // Create custom TextWriter to capture Zapf console output (which doesn't use logger)
            using var outputWriter = new System.IO.StringWriter();
            var originalError = Console.Error;
            
            try
            {
                // Redirect console error to capture Zapf output
                Console.SetError(outputWriter);

                // invoke ZILF
                var frontEnd = new FrontEnd 
                { 
                    FileSystem = fileSystem,
                    Logger = logger
                };

                foreach (var path in includePaths)
                    frontEnd.IncludePaths.Add(path);

                StatusUpdate?.Invoke(this, (int)BuildStatus.Compiling);

                var compResult = frontEnd.Compile(mainFilePath, zapPath, false);

                if (!compResult.Success)
                {
                    StatusUpdate?.Invoke(this, (int)BuildStatus.CompilerError);
                    return null;
                }

                // invoke ZAPF
                StatusUpdate?.Invoke(this, (int)BuildStatus.Assembling);

                var assembler = new Zapf.ZapfAssembler { FileSystem = fileSystem };
                var asmResult = assembler.Assemble(zapPath, System.IO.Path.ChangeExtension(zapPath, ".z#"));

                // Get captured output from assembly
                var assemblyOutput = outputWriter.ToString();
                if (!string.IsNullOrEmpty(assemblyOutput))
                {
                    foreach (var line in assemblyOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        LogMessage(line.TrimEnd('\r'));
                }

                if (!asmResult.Success)
                {
                    StatusUpdate?.Invoke(this, (int)BuildStatus.AssemblerError);
                    return null;
                }

                StatusUpdate?.Invoke(this, (int)BuildStatus.Built);

                // return changed files
                var newFilePaths = new List<string>();
                var newFileContents = new List<string>();

                foreach (var p in fileSystem.Paths)
                {
                    if (!filePaths.Contains(p))
                    {
                        newFilePaths.Add(p);
                        newFileContents.Add(Convert.ToBase64String(fileSystem.GetBytes(p)));
                    }
                }

                LogMessage($"Build complete. Generated {newFilePaths.Count} output file(s)");

                return (newPaths: newFilePaths.ToArray(), newContentsBase64: newFileContents.ToArray());
            }
            finally
            {
                // Restore console output
                Console.SetError(originalError);
            }
        }
    }

    sealed partial class BuildService
    {
        private readonly WorkspaceService workspace;
        private readonly IWorkerFactory workerFactory;
        private readonly JSInterop jsInterop;

        private BuildStatus _status = BuildStatus.NotBuilt;
        private readonly List<string> _buildOutput = new();
        private byte[]? _lastCompiledGame;

        public BuildStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    StatusChanged?.Invoke();
                }
            }
        }

        public void ResetStatus()
        {
            Status = BuildStatus.NotBuilt;
        }

        public IReadOnlyList<string> BuildOutput => _buildOutput;
        public byte[]? LastCompiledGame => _lastCompiledGame;

        public event Action? StatusChanged;
        public event Action? BuildOutputChanged;

        public FrontEndResult? CompilerResult { get; private set; }
        public AssemblyResult? AssemblerResult { get; private set; }

        public BuildService(WorkspaceService workspace, IWorkerFactory workerFactory, JSInterop jsInterop)
        {
            this.workspace = workspace;
            this.workerFactory = workerFactory;
            this.jsInterop = jsInterop;
        }

        [GeneratedRegex("\\.z\\d$", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex GetStoryFileRegex();

        private void AddBuildOutput(string message)
        {
            _buildOutput.Add(message);
            BuildOutputChanged?.Invoke();
        }

        public void ClearBuildOutput()
        {
            _buildOutput.Clear();
            BuildOutputChanged?.Invoke();
        }

        public async Task CompileWorkspaceAsync()
        {
            // Clear previous build output
            ClearBuildOutput();
            _lastCompiledGame = null;

            // make sure there's something to compile
            var project = workspace.Project;

            if (project.Files.Count == 0)
            {
                Status = BuildStatus.NotBuilt;
                AddBuildOutput("No files to compile");
                return;
            }

            // Set status to CompilePending immediately for instant UI feedback
            Status = BuildStatus.CompilePending;
            AddBuildOutput($"Starting build of {project.MainFile.Path}...");

            // hand off to background build worker
            var worker = await workerFactory.CreateAsync();
            var service = await worker.CreateBackgroundServiceAsync<BackgroundBuildWorker>();

            var paths = new List<string>();
            var contents = new List<string>();

            foreach (var f in project.Files)
            {
                paths.Add(f.Path);
                contents.Add(f.Content);
            }

            var includePaths = project.GetIncludePaths().ToArray();
            var mainFilePath = project.MainFile.Path;

            await service.RegisterEventListenerAsync(nameof(BackgroundBuildWorker.StatusUpdate), (object? _, int s) => Status = (BuildStatus)s);
            await service.RegisterEventListenerAsync(nameof(BackgroundBuildWorker.OutputMessage), (object? _, string msg) => AddBuildOutput(msg));

            var result = await service.RunAsync(w => w.Build(paths.ToArray(), contents.ToArray(), includePaths, mainFilePath));

            if (result != null)
            {
                var (newPaths, newContentsBase64) = result.Value;

                for (int i = 0; i < newPaths.Length; i++)
                {
                    var p = newPaths[i];

                    if (GetStoryFileRegex().IsMatch(p))
                    {
                        var filename = p[(p.LastIndexOf('/') + 1)..];
                        var gameData = Convert.FromBase64String(newContentsBase64[i]);

                        _lastCompiledGame = gameData;
                        AddBuildOutput($"Generated story file: {filename} ({gameData.Length} bytes)");

                        // Don't auto-download - let user choose when to download
                        break;
                    }
                }
            }
            else
            {
                AddBuildOutput("Build failed");
            }
        }

        public async Task DownloadStoryFileAsync()
        {
            if (_lastCompiledGame == null)
            {
                AddBuildOutput("No compiled game available. Please build first.");
                return;
            }

            // Generate filename based on main file
            var mainFilePath = workspace.Project.MainFile.Path;
            var baseFileName = System.IO.Path.GetFileNameWithoutExtension(mainFilePath);
            var storyFileName = $"{baseFileName}.z5";
            const string contentType = "application/x-zmachine";

            await jsInterop.DownloadBytesAsFileAsync(_lastCompiledGame, storyFileName, contentType);
        }

        public async Task PlayLastCompiledGameAsync()
        {
            if (_lastCompiledGame == null)
            {
                AddBuildOutput("No compiled game available. Please build first.");
                return;
            }

            AddBuildOutput("Loading game in Parchment...");
            await jsInterop.LoadGameInParchmentAsync(_lastCompiledGame);
        }
    }
}
