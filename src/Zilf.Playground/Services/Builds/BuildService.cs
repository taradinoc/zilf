#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zapf;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Playground.Services.Workspaces;

namespace Zilf.Playground.Services.Builds
{
    enum BuildStatus
    {
        NotBuilt,
        Compiling,
        CompilerError,
        Assembling,
        AssemblerError,
        Built,
    }

    sealed class BuildService
    {
        private readonly WorkspaceService workspace;
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

        public BuildService(WorkspaceService workspace)
        {
            this.workspace = workspace;
        }

        private static string GetZapPath(string zilPath)
        {
            const string OutputPrefix = "_build/";
            var zilFileName = zilPath[(zilPath.LastIndexOf('/') + 1)..];
            return OutputPrefix + System.IO.Path.ChangeExtension(zilFileName, ".zap");
        }

        public void CompileWorkspace()
        {
            // make sure there's something to compile
            var project = workspace.Project;

            if (project.MainFile == null)
            {
                Status = BuildStatus.NotBuilt;
                return;
            }

            // invoke ZILF
            var fileSystem = project.CopyToFileSystem();
            var frontEnd = new FrontEnd { FileSystem = fileSystem, Logger = NullDiagnosticLogger.Instance };

            foreach (var path in project.GetIncludePaths())
                frontEnd.IncludePaths.Add(path);

            var zapPath = GetZapPath(project.MainFile.Path);

            Status = BuildStatus.Compiling;

            var compResult = frontEnd.Compile(project.MainFile.Path, zapPath, false);

            if (!compResult.Success)
            {
                Status = BuildStatus.CompilerError;
                return;
            }

            var ifs = (Common.InMemoryFileSystem)fileSystem;
            foreach (var p in ifs.Paths)
                Console.WriteLine(">>> " + p);

            // invoke ZAPF
            Status = BuildStatus.Assembling;

            var assembler = new Zapf.ZapfAssembler { FileSystem = fileSystem };
            var asmResult = assembler.Assemble(zapPath, System.IO.Path.ChangeExtension(zapPath, ".z#"));

            if (!asmResult.Success)
            {
                Status = BuildStatus.AssemblerError;
                return;
            }

            Status = BuildStatus.Built;
        }
    }
}
