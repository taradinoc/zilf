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
        Compiling,
        CompilerError,
        Assembling,
        AssemblerError,
        Built,
    }

    public class BackgroundBuildWorker
    {
        public event EventHandler<int>? StatusUpdate;

        private static string GetZapPath(string zilPath)
        {
            const string OutputPrefix = "_build/";
            var zilFileName = zilPath[(zilPath.LastIndexOf('/') + 1)..];
            return OutputPrefix + System.IO.Path.ChangeExtension(zilFileName, ".zap");
        }

        public (string[] newPaths, string[] newContentsBase64)? Build(string[] filePaths, string[] fileContents, string[] includePaths, string mainFilePath)
        {
            // set up filesystem
            var fileSystem = new InMemoryFileSystem();

            for (int i = 0; i < filePaths.Length; i++)
                fileSystem.SetText(filePaths[i], fileContents[i]);

            var zapPath = GetZapPath(mainFilePath);

            // invoke ZILF
            var frontEnd = new FrontEnd { FileSystem = fileSystem, Logger = NullDiagnosticLogger.Instance };

            foreach (var path in includePaths)
                frontEnd.IncludePaths.Add(path);

            StatusUpdate?.Invoke(this, (int)BuildStatus.Compiling);

            var compResult = frontEnd.Compile(mainFilePath, zapPath, false);

            if (!compResult.Success)
            {
                StatusUpdate?.Invoke(this, (int)BuildStatus.CompilerError);

                var logger = new DefaultDiagnosticLogger();
                foreach (var d in compResult.Diagnostics)
                    logger.Log(d);

                return null;
            }

            //foreach (var p in fileSystem.Paths)
            //    Console.WriteLine(">>> " + p);

            // invoke ZAPF
            StatusUpdate?.Invoke(this, (int)BuildStatus.Assembling);

            var assembler = new Zapf.ZapfAssembler { FileSystem = fileSystem };
            var asmResult = assembler.Assemble(zapPath, System.IO.Path.ChangeExtension(zapPath, ".z#"));

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

            return (newPaths: newFilePaths.ToArray(), newContentsBase64: newFileContents.ToArray());
        }
    }

    sealed class BuildService
    {
        private readonly WorkspaceService workspace;
        private readonly IWorkerFactory workerFactory;
        private readonly JSInterop jsInterop;

        private BuildStatus _status = BuildStatus.NotBuilt;

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

        public event Action? StatusChanged;

        public FrontEndResult? CompilerResult { get; private set; }
        public AssemblyResult? AssemblerResult { get; private set; }

        public BuildService(WorkspaceService workspace, IWorkerFactory workerFactory, JSInterop jsInterop)
        {
            this.workspace = workspace;
            this.workerFactory = workerFactory;
            this.jsInterop = jsInterop;
        }

        private static readonly Regex StoryFileRegExp = new(@"\.z\d$", RegexOptions.IgnoreCase);

        public async Task CompileWorkspaceAsync()
        {
            // make sure there's something to compile
            var project = workspace.Project;

            if (project.MainFile == null)
            {
                Status = BuildStatus.NotBuilt;
                return;
            }

            // hand off to background build worker
            var worker = await workerFactory.CreateAsync();
            var service = await worker.CreateBackgroundServiceAsync<BackgroundBuildWorker>(
                options => options
                    .AddAssemblies(
                        "ReadLine.dll",
                        "Zapf.dll",
                        "Zapf.Parsing.dll",
                        "Zilf.dll",
                        "Zilf.Common.dll",
                        "Zilf.Emit.dll",
                        "Zilf.Playground.dll"
                        ));

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
            var result = await service.RunAsync(w => w.Build(paths.ToArray(), contents.ToArray(), includePaths, mainFilePath));

            if (result != null)
            {
                var (newPaths, newContentsBase64) = result.Value;

                for (int i = 0; i < newPaths.Length; i++)
                {
                    var p = newPaths[i];

                    if (StoryFileRegExp.IsMatch(p))
                    {
                        var filename = p[(p.LastIndexOf('/') + 1)..];
                        const string contentType = "application/x-zmachine";

                        jsInterop.DownloadBytesAsFile(Convert.FromBase64String(newContentsBase64[i]), filename, contentType);
                        break;
                    }
                }
            }
        }
    }
}
