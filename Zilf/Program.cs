/* Copyright 2010-2017 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf
{
    static class Program
    {
        public const string VERSION = "ZILF 0.8";

        internal static int Main([ItemNotNull] [NotNull] string[] args)
        {
            Contract.Requires(args != null);
            var ctx = ParseArgs(args, out var inFile, out var outFile);

            if (ctx == null)
                return 1;       // ParseArgs signaled an error

            if (!ctx.Quiet)
            {
                Console.Write(VERSION);
                Console.Write(" built ");
                Console.WriteLine(RetrieveLinkerTimestamp());
            }

            if (ctx.RunMode == RunMode.Interactive)
            {
                DoREPL(ctx);
            }
            else if (ctx.RunMode == RunMode.Expression)
            {
                using (ctx.PushFileContext("<cmdline>"))
                {
                    Console.WriteLine(Evaluate(ctx, inFile));
                    if (ctx.ErrorCount > 0)
                        return 2;
                }
            }
            else
            {
                // interpreter or compiler

                var frontEnd = new FrontEnd();
                try
                {
                    FrontEndResult result;

                    if (ctx.RunMode == RunMode.Compiler)
                    {
                        Debug.Assert(outFile != null);
                        result = frontEnd.Compile(ctx, inFile, outFile, ctx.WantDebugInfo);
                    }
                    else
                    {
                        result = frontEnd.Interpret(ctx, inFile);
                    }

                    if (result.WarningCount > 0)
                    {
                        Console.Error.WriteLine("{0} warning{1}",
                            ctx.WarningCount,
                            ctx.WarningCount == 1 ? "" : "s");
                    }

                    if (result.ErrorCount > 0)
                    {
                        Console.Error.WriteLine("{0} error{1}",
                            ctx.ErrorCount,
                            ctx.ErrorCount == 1 ? "" : "s");
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
            }

            return 0;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This is a top-level loop that reports unhandled exceptions to the user.")]
        static void DoREPL([NotNull] Context ctx)
        {
            Contract.Requires(ctx != null);
            using (ctx.PushFileContext("<stdin>"))
            {
                var sb = new StringBuilder();

                int angles = 0, rounds = 0, squares = 0, quotes = 0;

                while (true)
                {
                    if (!ctx.Quiet)
                        Console.Write(sb.Length == 0 ? "> " : ">> ");

                    try
                    {
                        var line = Console.ReadLine();

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
            byte[] b = new byte[2048];

            using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                s.Read(b, 0, 2048);
            }

            var i = BitConverter.ToInt32(b, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.ToLocalTime();
            return dt;
        }

        [CanBeNull]
        [ContractAnnotation("=> null, inFile: null, outFile: null; => notnull, inFile: notnull, outFile: canbenull")]
        static Context ParseArgs([NotNull] string[] args, [CanBeNull] out string inFile, [CanBeNull] out string outFile)
        {
            Contract.Requires(args != null);

            string newInFile = inFile = null;
            string newOutFile = outFile = null;

            bool traceRoutines = false, debugInfo = false;
            bool? caseSensitive = null;
            RunMode? mode = null;
            bool? quiet = null;
            var includePaths = new List<string>();

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
                RunMode = mode.Value,
                Quiet = quiet.Value
            };

            ctx.IncludePaths.AddRange(includePaths);
            AddImplicitIncludePaths(ctx.IncludePaths, newInFile, mode.Value);

            inFile = newInFile;
            outFile = newOutFile;
            return ctx;

            bool ParseArgs()
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLowerInvariant())
                    {
                        case "-c":
                            mode = RunMode.Compiler;
                            break;

                        case "-e":
                            mode = RunMode.Expression;
                            break;

                        case "-i":
                            mode = RunMode.Interactive;
                            break;

                        case "-q":
                            quiet = true;
                            break;

                        case "-cs":
                            caseSensitive = true;
                            break;

                        case "-ci":
                            caseSensitive = false;
                            break;

                        case "-tr":
                            traceRoutines = true;
                            break;

                        case "-d":
                            debugInfo = true;
                            break;

                        case "-x":
                            mode = RunMode.Interpreter;
                            break;

                        case "-ip":
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

                        case "-?":
                        case "--help":
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
                if (mode == null)
                    mode = (newInFile == null ? RunMode.Interactive : RunMode.Compiler);
                if (quiet == null)
                    quiet = (mode == RunMode.Expression || mode == RunMode.Interpreter);

                switch (mode.Value)
                {
                    case RunMode.Compiler:
                        if (newInFile == null)
                        {
                            Usage();
                            return false;
                        }

                        if (newOutFile == null)
                            newOutFile = Path.ChangeExtension(newInFile, ".zap");
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

                if (caseSensitive == null)
                {
                    switch (mode.Value)
                    {
                        case RunMode.Expression:
                        case RunMode.Interactive:
                            caseSensitive = false;
                            break;

                        default:
                            caseSensitive = true;
                            break;
                    }
                }

                return true;
            }
        }

        static void AddImplicitIncludePaths([ItemNotNull] [NotNull] IList<string> includePaths, [CanBeNull] string inFile, RunMode mode)
        {
            Contract.Requires(includePaths != null);

            if (inFile != null && mode != RunMode.Expression)
            {
                includePaths.Insert(0, Path.GetDirectoryName(Path.GetFullPath(inFile)));
            }

            if (includePaths.Count == 0)
            {
                includePaths.Add(Environment.CurrentDirectory);
            }

            // look for a "library" directory somewhere near zilf.exe
            var strippables = new HashSet<string> { "bin", "debug", "release", "zilf" };
            string[] libraryDirNames = { "Library", "library", "lib" };

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
                        includePaths.Add(candidate);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var segment = Path.GetFileName(zilfDir);
                    Debug.Assert(segment != null);

                    if (strippables.Contains(segment.ToLowerInvariant()))
                    {
                        // strip last segment and keep looking
                        zilfDir = Path.GetDirectoryName(zilfDir);
                        if (zilfDir != null)
                        {
                            continue;
                        }
                    }
                }

                break;
            }
        }

        static void Usage()
        {
            Console.WriteLine(VERSION);
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
  -d                    include debug information");
        }

        // TODO: move Parse somewhere more sensible
        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse([NotNull] Context ctx, [NotNull] IEnumerable<char> chars)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(chars != null);

            return Parse(ctx, null, chars, null);
        }

        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse([NotNull] Context ctx, [NotNull] IEnumerable<char> chars, params ZilObject[] templateParams)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(chars != null);

            return Parse(ctx, null, chars, templateParams);
        }

        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse([NotNull] Context ctx, ISourceLine src, [NotNull] IEnumerable<char> chars, params ZilObject[] templateParams)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(chars != null);

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

        static IEnumerable<char> ReadAllChars([NotNull] Stream stream)
        {
            Contract.Requires(stream != null);
            using (var rdr = new StreamReader(stream))
            {
                int c;
                while ((c = rdr.Read()) >= 0)
                {
                    yield return (char)c;
                }
            }
        }

        [CanBeNull]
        [ContractAnnotation("wantExceptions: true => notnull")]
        // ReSharper disable once UnusedMethodReturnValue.Global
        public static ZilObject Evaluate([NotNull] Context ctx, [NotNull] Stream stream, bool wantExceptions = false)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(stream != null);

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
        [CanBeNull]
        public static ZilObject Evaluate([NotNull] Context ctx, [NotNull] IEnumerable<char> chars, bool wantExceptions = false)
        {
            Contract.Requires(ctx != null);
            try
            {
                var ztree = Parse(ctx, chars);

                ZilObject result = null;
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