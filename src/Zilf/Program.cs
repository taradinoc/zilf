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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Language.Parsing;

namespace Zilf
{
    static class Program
    {
        internal static string GetVersion() =>
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "<?.??>";

        internal static string GetBanner() => $"ZILF {GetVersion()}";

        internal static int Main(string[] args)
        {
            var ctx = BuildContext(args, out var inFile, out var outFile);

            if (ctx == null)
                return 1;       // BuildContext signaled an error

            if (!ctx.Quiet)
            {
                Console.Write(GetBanner());
                Console.Write(" built ");
                Console.WriteLine(RetrieveLinkerTimestamp());
            }

            switch (ctx.RunMode)
            {
                case RunMode.Interactive:
                    DoREPL(ctx);
                    return 0;

                case RunMode.Expression:
                    Debug.Assert(inFile != null);
                    using (ctx.PushFileContext("<cmdline>"))
                    {
                        Console.WriteLine(Evaluate(ctx, inFile));
                        if (ctx.ErrorCount > 0)
                            return 2;
                    }
                    return 0;

                case RunMode.Compiler:
                    Debug.Assert(inFile != null);
                    Debug.Assert(outFile != null);
                    return WrapInFrontEnd(frontEnd => frontEnd.Compile(ctx, inFile, outFile, ctx.WantDebugInfo));

                case RunMode.Interpreter:
                    Debug.Assert(inFile != null);
                    return WrapInFrontEnd(frontEnd => frontEnd.Interpret(ctx, inFile));

                default:
                    throw new UnreachableCodeException();
            }

            static int WrapInFrontEnd(Func<FrontEnd, FrontEndResult> func)
            {
                var frontEnd = new FrontEnd();
                try
                {
                    var result = func(frontEnd);

                    if (result.WarningCount > 0)
                    {
                        Console.Error.Write("{0} warning{1}",
                            result.WarningCount,
                            result.WarningCount == 1 ? "" : "s");

                        if (result.SuppressedWarningCount > 0)
                        {
                            Console.Error.Write(
                                " ({0} suppressed)",
                                result.SuppressedWarningCount);
                        }

                        Console.Error.WriteLine();
                    }

                    if (result.ErrorCount > 0)
                    {
                        Console.Error.WriteLine("{0} error{1}",
                            result.ErrorCount,
                            result.ErrorCount == 1 ? "" : "s");
                        return 2;
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Console.Error.WriteLine("file not found: " + ex.FileName);
                    return 1;
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine("I/O error: " + ex.Message);
                    return 1;
                }

                return 0;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This is a top-level loop that reports unhandled exceptions to the user.")]
        static void DoREPL(Context ctx)
        {
            using (ctx.PushFileContext("<stdin>"))
            using (new Completer(ctx).Attach())
            {
                ReadLine.HistoryEnabled = true;

                var sb = new StringBuilder();

                int angles = 0, rounds = 0, squares = 0, quotes = 0;

                while (true)
                {
                    try
                    {
                        var line = ReadLine.Read(sb.Length == 0 ? "> " : ">> ");

                        if (line == null)
                            break;

                        if (sb.Length > 0)
                            sb.AppendLine();
                        sb.Append(line);

                        int state = (quotes > 0) ? 1 : 0;
                        foreach (char c in line)
                        {
                            switch (state)
                            {
                                case 0:
                                    switch (c)
                                    {
                                        case '<': angles++; break;
                                        case '>': angles--; break;
                                        case '(': rounds++; break;
                                        case ')': rounds--; break;
                                        case '[': squares++; break;
                                        case ']': squares--; break;
                                        case '"': quotes++; state = 1; break;
                                    }
                                    break;

                                case 1:
                                    switch (c)
                                    {
                                        case '"': quotes--; state = 0; break;
                                        case '\\': state = 2; break;
                                    }
                                    break;

                                case 2:
                                    state = 1;
                                    break;
                            }
                        }

                        if (angles == 0 && rounds == 0 && squares == 0 && quotes == 0)
                        {
                            var result = Evaluate(ctx, sb.ToString());
                            if (result != null)
                            {
                                try
                                {
                                    Console.WriteLine(result.ToStringContext(ctx, false));
                                }
                                catch (InterpreterError ex)
                                {
                                    ctx.HandleError(ex);
                                }
                            }

                            sb.Length = 0;
                        }
                    }
                    // ReSharper disable once CatchAllClause
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        sb.Length = 0;
                    }
                }
            }
        }

        static DateTime RetrieveLinkerTimestamp()
        {
            // http://stackoverflow.com/questions/1600962/displaying-the-build-date
            string filePath = Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            Span<byte> b = stackalloc byte[2048];

            using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                s.Read(b);
            }

            var i = BitConverter.ToInt32(b.Slice(c_PeHeaderOffset, 4));
            var secondsSince1970 = BitConverter.ToInt32(b.Slice(i + c_LinkerTimestampOffset, 4));
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.ToLocalTime();
            return dt;
        }

        [return: NotNullIfNotNull("inFile")]
        static Context? BuildContext(string[] args, [NotNullIfNotNull("outFile")] out string? inFile, out string? outFile)
        {
            string? newInFile = inFile = null;
            string? newOutFile = outFile = null;

            bool traceRoutines = false, debugInfo = false, warningsAsErrors = false, suppressNoisyWarnings = true;
            bool? caseSensitive = null;
            RunMode? mode = null;
            bool? quiet = null;
            var includePaths = new List<string>();
            var suppressedDiagnosticCodes = new List<string>();

            if (!ParseArgs())
                return null;

            if (!SetDefaultsAndValidate())
                return null;

            Debug.Assert(caseSensitive != null);
            Debug.Assert(mode != null);
            Debug.Assert(quiet != null);

            // initialize and return Context
            var ctx = new Context(!caseSensitive.Value)
            {
                TraceRoutines = traceRoutines,
                WantDebugInfo = debugInfo,
                WarningsAsErrors = warningsAsErrors,
                SuppressNoisyWarnings = suppressNoisyWarnings,
                RunMode = mode.Value,
                Quiet = quiet.Value
            };

            ctx.IncludePaths.AddRange(includePaths);
            AddImplicitIncludePaths(ctx.IncludePaths, newInFile, mode.Value);

            foreach (var code in suppressedDiagnosticCodes)
                ctx.DiagnosticManager.Suppress(code);

            inFile = newInFile;
            outFile = newOutFile;
            return ctx;

            bool ParseArgs()
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToUpperInvariant())
                    {
                        case "-C":
                            mode = RunMode.Compiler;
                            break;

                        case "-E":
                            mode = RunMode.Expression;
                            break;

                        case "-I":
                            mode = RunMode.Interactive;
                            break;

                        case "-Q":
                            quiet = true;
                            break;

                        case "-CS":
                            caseSensitive = true;
                            break;

                        case "-CI":
                            caseSensitive = false;
                            break;

                        case "-TR":
                            traceRoutines = true;
                            break;

                        case "-D":
                            debugInfo = true;
                            break;

                        case "-X":
                            mode = RunMode.Interpreter;
                            break;

                        case "-IP":
                            i++;
                            if (i < args.Length)
                            {
                                includePaths.Add(args[i]);
                            }
                            else
                            {
                                Usage();
                                return false;
                            }

                            break;

                        case "-W":
                            suppressNoisyWarnings = false;
                            break;

                        case "-WE":
                            warningsAsErrors = true;
                            break;

                        case "-WS":
                            i++;
                            if (i < args.Length)
                            {
                                suppressedDiagnosticCodes.AddRange(args[i].Split(','));
                            }
                            else
                            {
                                Usage();
                                return false;
                            }

                            break;

                        case "-?":
                        case "--HELP":
                        case "/?":
                            Usage();
                            return false;

                        default:
                            if (newInFile == null)
                            {
                                newInFile = args[i];
                            }
                            else if (newOutFile == null)
                            {
                                newOutFile = args[i];
                            }
                            else
                            {
                                Usage();
                                return false;
                            }

                            break;
                    }
                }

                return true;
            }

            bool SetDefaultsAndValidate()
            {
                mode ??= (newInFile == null ? RunMode.Interactive : RunMode.Compiler);
                quiet ??= (mode is RunMode.Expression or RunMode.Interpreter);

                switch (mode.Value)
                {
                    case RunMode.Compiler:
                        if (newInFile == null)
                        {
                            Usage();
                            return false;
                        }

                        newOutFile ??= Path.ChangeExtension(newInFile, ".zap");
                        break;

                    case RunMode.Expression:
                    case RunMode.Interpreter:
                        if (newInFile == null || newOutFile != null)
                        {
                            Usage();
                            return false;
                        }

                        break;

                    case RunMode.Interactive:
                        if (newInFile != null)
                        {
                            Usage();
                            return false;
                        }

                        break;
                }

                caseSensitive ??= mode.Value switch
                {
                    RunMode.Expression or RunMode.Interactive => false,
                    _ => true,
                };

                return true;
            }
        }

        static void AddImplicitIncludePaths(List<string> includePaths, string? inFile, RunMode mode)
        {
            if (inFile != null && mode != RunMode.Expression && Path.GetDirectoryName(Path.GetFullPath(inFile)) is string dir)
            {
                includePaths.Insert(0, dir);
            }

            if (includePaths.Count == 0)
            {
                includePaths.Add(Environment.CurrentDirectory);
            }

            // look for a "library" directory somewhere near zilf.exe
            var strippables = new HashSet<string> { "bin", "debug", "release", "zilf", "src" };
            string[] libraryDirNames = { "Library", "library", "lib", "zillib" };

            var zilfDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(zilfDir != null);

            while (true)
            {
                bool found = false;

                foreach (var n in libraryDirNames)
                {
                    var candidate = Path.Combine(zilfDir, n);

                    if (Directory.Exists(candidate))
                    {
                        found = true;
                        foreach (var path in RecursiveLibraryIncludePaths(candidate))
                            includePaths.Add(path);
                        break;
                    }
                }

                if (!found)
                {
                    zilfDir += Path.DirectorySeparatorChar;
                    var pos = strippables.Max(strippable =>
                        zilfDir.LastIndexOf(Path.DirectorySeparatorChar + strippable + Path.DirectorySeparatorChar,
                            StringComparison.InvariantCultureIgnoreCase));

                    if (pos >= 0)
                    {
                        // remove part after split point and keep looking
                        zilfDir = zilfDir[..pos];

                        if (!string.IsNullOrEmpty(zilfDir))
                            continue;
                    }
                }

                break;
            }

            static IEnumerable<string> RecursiveLibraryIncludePaths(string parent)
            {
                var first = Enumerable.Repeat(parent, 1);

                static bool Excluded(string name) => name[0] is '.' or '_' || name.ToUpperInvariant() is "TEST" or "TESTS";

                var rest = from subdir in Directory.EnumerateDirectories(parent)
                           let name = Path.GetFileName(subdir)
                           where !Excluded(name)
                           from result in RecursiveLibraryIncludePaths(subdir)
                           select result;

                return first.Concat(rest);
            }
        }

        static void Usage()
        {
            Console.WriteLine(GetBanner());
            Console.WriteLine(
@"Interact: zilf [switches] [-i]
Evaluate: zilf [switches] -e ""<expression>""
 Execute: zilf [switches] -x <inFile.zil>
 Compile: zilf [switches] [-c] <inFile.zil> [<outFile>]

Modes:
  -c filename           execute code file and generate Z-code (default)
  -x filename           execute code file but produce no output
  -e ""expr""             evaluate expr from command line
  -i                    interactive mode (default if no filename given)
General switches:
  -q                    quiet: no banner or prompt
  -cs                   case sensitive (default for -c, -x)
  -ci                   case insensitive (default for -e, -i)
  -ip dir               add dir to include path (may be repeated)
Compiler switches:
  -tr                   trace routine calls at runtime
  -d                    include debug information
Warning message options:
  -w                    enable all warnings (even noisy ones)
  -we                   treat warnings as errors
  -ws code[,code...]    suppress specific warnings (may be repeated)");
        }

        // TODO: move Parse somewhere more sensible
        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse(Context ctx, IEnumerable<char> chars)
        {
            return Parse(ctx, null, chars, null);
        }

        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse(Context ctx, IEnumerable<char> chars, params ZilObject[] templateParams)
        {
            return Parse(ctx, null, chars, templateParams);
        }

        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse(Context ctx, ISourceLine? src, IEnumerable<char> chars, params ZilObject[]? templateParams)
        {
            var parser = new Parser(ctx, src, templateParams);

            foreach (var po in parser.Parse(chars))
            {
                if (po.IsIgnorable)
                    continue;

                switch (po.Type)
                {
                    case ParserOutputType.Object:
                        yield return po.Object;
                        break;

                    case ParserOutputType.EndOfInput:
                        yield break;

                    case ParserOutputType.SyntaxError:
                        throw new InterpreterError(
                            src ?? new FileSourceLine(ctx.CurrentFile.Path, parser.Line),
                            InterpreterMessages.Syntax_Error_0, po.Exception.Message);

                    case ParserOutputType.Terminator:
                        throw new InterpreterError(
                            src ?? new FileSourceLine(ctx.CurrentFile.Path, parser.Line),
                            InterpreterMessages.Syntax_Error_0, "misplaced terminator");

                    default:
                        throw new UnhandledCaseException("parser output type");
                }
            }
        }

        static IEnumerable<char> ReadAllChars(Stream stream)
        {
            using var rdr = new StreamReader(stream);

            int c;
            while ((c = rdr.Read()) >= 0)
            {
                yield return (char)c;
            }
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public static ZilObject? Evaluate(Context ctx, Stream stream, bool wantExceptions = false)
        {
            return Evaluate(ctx, ReadAllChars(stream), wantExceptions);
        }

        /// <summary>
        /// Evaluates some code in a <see cref="Context"/>.
        /// </summary>
        /// <param name="ctx">The context in which to evaluate.</param>
        /// <param name="chars">The code to evaluate.</param>
        /// <param name="wantExceptions"><see langword="true"/> if the method should be allowed to throw
        /// <see cref="InterpreterError"/>, or <see langword="false"/> to catch it.</param>
        /// <returns>The result of evaluating the last object in the code; or <see langword="null"/> if either the code contained
        /// no objects, or <paramref name="wantExceptions"/> was <see langword="false"/> and an <see cref="InterpreterError"/> was caught.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static ZilObject? Evaluate(Context ctx, IEnumerable<char> chars, bool wantExceptions = false)
        {
            try
            {
                var ztree = Parse(ctx, chars);

                ZilObject? result = null;
                bool first = true;
                foreach (var node in ztree)
                {
                    try
                    {
                        using (DiagnosticContext.Push(node.SourceLine))
                        {
                            if (first)
                            {
                                // V4 games can identify themselves this way instead of using <VERSION EZIP>
                                if (node is ZilString str &&
                                    str.Text.StartsWith("EXTENDED", StringComparison.Ordinal) &&
                                    ctx.ZEnvironment.ZVersion == 3)
                                {
                                    ctx.SetZVersion(4);
                                }

                                first = false;
                            }
                            result = (ZilObject)node.Eval(ctx);
                        }
                    }
                    catch (InterpreterError ex) when (wantExceptions == false)
                    {
                        ctx.HandleError(ex);
                    }
                }

                return result;
            }
            catch (InterpreterError ex) when (wantExceptions == false)
            {
                ctx.HandleError(ex);
                return null;
            }
        }
    }
}