/* Copyright 2010-2026 Tara McGrew
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Zilf.Compiler;

namespace Zilf.Cli
{
    internal sealed record class ZilfCliServices(
        IHostFileSystem HostFileSystem,
        IFrontEndFactory FrontEndFactory,
        IExternalToolService ExternalToolService,
        ContextFactory ContextFactory,
        ProjectTemplateService ProjectTemplateService,
        BuildCommandHandler BuildCommandHandler,
        ReplCommandHandler ReplCommandHandler,
        ExecCommandHandler ExecCommandHandler,
        NewProjectCommandHandler NewProjectCommandHandler,
        ZilfCommandSpecFactory CommandSpecFactory);

    internal interface IHostFileSystem
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        string GetCurrentDirectory();

        IEnumerable<string> EnumerateDirectories(string path);

        void CreateDirectory(string path);

        string ReadAllText(string path);

        void WriteAllText(string path, string content, Encoding encoding);

        byte[] ReadAllBytes(string path);

        Stream OpenWrite(string path);
    }

    internal sealed class HostFileSystem : IHostFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

        public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string content, Encoding encoding) => File.WriteAllText(path, content, encoding);

        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public Stream OpenWrite(string path) => new FileStream(path, FileMode.Create, FileAccess.Write);
    }

    internal interface IFrontEndFactory
    {
        FrontEnd Create();
    }

    internal sealed class FrontEndFactory : IFrontEndFactory
    {
        public FrontEnd Create() => new();
    }

    internal interface IExternalToolService
    {
        string? FindZapfExecutable();

        string? FindZilfPubExecutable();

        string? FindGlazerExecutable();

        int RunZapfProcess(string zapfPath, string zapInputPath, IReadOnlyList<string> extraArgs, string? finalOutput = null);

        int RunGlazerProcess(string glazerPath, string asmInputPath, IReadOnlyList<string> extraArgs, string? finalOutput = null);

        int RunZilfPubProcess(string zilfPubPath, IReadOnlyList<string> arguments);
    }

    internal sealed class ExternalToolService : IExternalToolService
    {
        public string? FindZapfExecutable() => FindExecutable(["zapf", "Zapf", "zapf.exe", "Zapf.exe"]);

        public string? FindZilfPubExecutable() => FindExecutable(["zilfpub", "ZilfPub", "zilfpub.exe", "ZilfPub.exe"]);

        public string? FindGlazerExecutable() => FindExecutable(["glazer", "Glazer", "glazer.exe", "Glazer.exe"]);

        public int RunZapfProcess(string zapfPath, string zapInputPath, IReadOnlyList<string> extraArgs, string? finalOutput = null)
        {
            var arguments = new List<string>(extraArgs)
            {
                zapInputPath
            };

            if (!string.IsNullOrEmpty(finalOutput))
            {
                arguments.Add(finalOutput);
            }

            return RunProcess(zapfPath, arguments, "ZAPF");
        }

        public int RunGlazerProcess(string glazerPath, string asmInputPath, IReadOnlyList<string> extraArgs, string? finalOutput = null)
        {
            var arguments = new List<string>(extraArgs)
            {
                asmInputPath
            };

            if (!string.IsNullOrEmpty(finalOutput))
            {
                arguments.Add("-o");
                arguments.Add(finalOutput);
            }

            return RunProcess(glazerPath, arguments, "Glazer");
        }

        public int RunZilfPubProcess(string zilfPubPath, IReadOnlyList<string> arguments)
        {
            return RunProcess(zilfPubPath, arguments, "ZilfPub", includeStartFailureMessage: true);
        }

        private static string? FindExecutable(IEnumerable<string> candidates)
        {
            var baseDir = AppContext.BaseDirectory;
            foreach (var name in candidates)
            {
                var full = Path.Combine(baseDir, name);
                if (File.Exists(full))
                    return full;
            }

            return null;
        }

        private static int RunProcess(
            string executablePath,
            IReadOnlyList<string> arguments,
            string displayName,
            bool includeStartFailureMessage = false)
        {
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
            };

            foreach (var argument in arguments)
            {
                proc.StartInfo.ArgumentList.Add(argument);
            }

            try
            {
                if (!proc.Start())
                {
                    if (includeStartFailureMessage)
                        Console.Error.WriteLine($"Failed to start {displayName} process.");
                    return 1;
                }

                proc.WaitForExit();
                return proc.ExitCode;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.Error.WriteLine($"Failed to launch {displayName}: {ex.Message}");
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"{displayName} not found: {ex.FileName}");
                return 1;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"Failed to run {displayName}: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error invoking {displayName}: {ex.Message}");
                return 1;
            }
        }
    }
}