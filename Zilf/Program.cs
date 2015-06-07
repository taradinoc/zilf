/* Copyright 2010, 2015 Jesse McGrew
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

using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using Zilf.Compiler;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Language.Lexing;
using Zilf.Language.Parsing;

namespace Zilf
{
    class Program
    {
        public const string VERSION = "ZILF 0.5";

        internal static int Main(string[] args)
        {
            string inFile, outFile;
            Context ctx = ParseArgs(args, out inFile, out outFile);

            if (ctx == null)
                return 1;       // ParseArgs signaled an error

            if (!ctx.Quiet)
                Console.WriteLine(VERSION);

            if (ctx.RunMode == RunMode.Interactive)
            {
                ctx.CurrentFile = "<stdin>";
                StringBuilder sb = new StringBuilder();

                int angles = 0, rounds = 0, squares = 0, quotes = 0;

                while (true)
                {
                    if (!ctx.Quiet)
                        Console.Write(sb.Length == 0 ? "> " : ">> ");

                    try
                    {
                        string line = Console.ReadLine();
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
                            Console.WriteLine(Evaluate(ctx, sb.ToString()));
                            sb.Length = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        sb.Length = 0;
                    }
                }
            }
            else if (ctx.RunMode == RunMode.Expression)
            {
                ctx.CurrentFile = "<cmdline>";
                Console.WriteLine(Evaluate(ctx, inFile));
                if (ctx.ErrorCount > 0)
                    return 2;
            }
            else
            {
                // interpreter or compiler
                ICharStream charStream;
                try
                {
                    charStream = new ANTLRFileStream(inFile);
                }
                catch (FileNotFoundException)
                {
                    Console.Error.WriteLine("file not found: " + inFile);
                    return 1;
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine("error loading file: " + ex.Message);
                    return 1;
                }

                ctx.CurrentFile = inFile;
                Evaluate(ctx, charStream);

                if (ctx.ErrorCount > 0)
                {
                    Console.Error.WriteLine("{0} error{1}",
                        ctx.ErrorCount,
                        ctx.ErrorCount == 1 ? "" : "s");
                    return 2;
                }

                if (ctx.RunMode == RunMode.Compiler)
                {
                    // TODO: Rely on ZilfCompiler for all this logic.
                    ctx.SetDefaultConstants();

                    try
                    {
                        var gameBuilder = new Emit.Zap.GameBuilder(ctx.ZEnvironment.ZVersion, outFile, ctx.WantDebugInfo,
                            ZilfCompiler.MakeGameOptions(ctx));
                        Zilf.Compiler.Compiler.Compile(ctx, gameBuilder);
                    }
                    catch (ZilError ex)
                    {
                        ctx.HandleError(ex);
                    }
                    if (ctx.ErrorCount > 0)
                    {
                        Console.Error.WriteLine("{0} error{1}",
                            ctx.ErrorCount,
                            ctx.ErrorCount == 1 ? "" : "s");
                        return 3;
                    }
                }
            }

            return 0;
        }

        private static Context ParseArgs(string[] args, out string inFile, out string outFile)
        {
            Contract.Requires(args != null);

            inFile = null;
            outFile = null;

            bool traceRoutines = false, debugInfo = false;
            bool? caseSensitive = null;
            RunMode? mode = null;
            bool? quiet = null;
            List<string> includePaths = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
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
                            return null;
                        }
                        break;

                    case "-?":
                    case "--help":
                    case "/?":
                        Usage();
                        return null;

                    default:
                        if (inFile == null)
                        {
                            inFile = args[i];
                        }
                        else if (outFile == null)
                        {
                            outFile = args[i];
                        }
                        else
                        {
                            Usage();
                            return null;
                        }
                        break;
                }
            }

            // defaults
            if (mode == null)
                mode = (inFile == null ? RunMode.Interactive : RunMode.Compiler);
            if (quiet == null)
                quiet = (mode == RunMode.Expression || mode == RunMode.Interpreter);

            // validate
            switch (mode.Value)
            {
                case RunMode.Compiler:
                    if (inFile == null) { Usage(); return null; }
                    if (outFile == null)
                        outFile = Path.ChangeExtension(inFile, ".zap");
                    break;

                case RunMode.Expression:
                case RunMode.Interpreter:
                    if (inFile == null || outFile != null) { Usage(); return null; }
                    break;

                case RunMode.Interactive:
                    if (inFile != null) { Usage(); return null; }
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

            Context ctx = new Context(!caseSensitive.Value);

            if (inFile != null && mode.Value != RunMode.Expression)
            {
                ctx.IncludePaths.Add(Path.GetDirectoryName(Path.GetFullPath(inFile)));
            }
            ctx.IncludePaths.AddRange(includePaths);

            ctx.TraceRoutines = traceRoutines;
            ctx.WantDebugInfo = debugInfo;
            ctx.RunMode = mode.Value;
            ctx.Quiet = quiet.Value;

            return ctx;
        }

        private static void Usage()
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

        public static ZilObject Evaluate(Context ctx, string str, bool wantExceptions = false)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(str != null);

            ICharStream charStream = new ANTLRStringStream(str);
            return Evaluate(ctx, charStream, wantExceptions);
        }

        public static ZilObject Evaluate(Context ctx, ICharStream charStream, bool wantExceptions = false)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(charStream != null);

            ZilLexer lexer = new ZilLexer(charStream);

            ITokenStream tokenStream = new CommonTokenStream(lexer);
            ZilParser parser = new ZilParser(tokenStream);

            var fret = parser.file(ctx.CurrentFile);
            if (parser.NumberOfSyntaxErrors > 0)
            {
                foreach (var error in parser.SyntaxErrors)
                {
                    ctx.HandleError(error);
                }
                return null;
            }

            try
            {
                IEnumerable<ZilObject> ztree;
                ztree = ZilObject.ReadFromAST((ITree)fret.Tree, ctx);

                ZilObject result = null;
                bool first = true;
                foreach (ZilObject node in ztree)
                {
                    try
                    {
                        if (first)
                        {
                            // V4 games can identify themselves this way instead of using <VERSION EZIP>
                            ZilString str = node as ZilString;
                            if (str != null && str.Text.StartsWith("EXTENDED") && ctx.ZEnvironment.ZVersion == 3)
                            {
                                ctx.ZEnvironment.ZVersion = 4;
                                ctx.InitPropDefs();
                            }

                            first = false;
                        }
                        result = node.Eval(ctx);
                    }
                    catch (InterpreterError ex)
                    {
                        if (ex.SourceLine == null)
                            ex.SourceLine = node.SourceLine;

                        if (wantExceptions)
                            throw;

                        ctx.HandleError(ex);
                    }
                    catch (ControlException ex)
                    {
                        var newEx = new InterpreterError(node.SourceLine, "misplaced " + ex.Message);

                        if (wantExceptions)
                            throw newEx;
                        else
                            ctx.HandleError(newEx);
                    }
                }

                return result;
            }
            catch (InterpreterError ex)
            {
                if (wantExceptions)
                    throw;

                ctx.HandleError(ex);
                return null;
            }
            catch (ControlException ex)
            {
                if (wantExceptions)
                    throw;

                ctx.HandleError(new InterpreterError("misplaced " + ex.Message));
                return null;
            }
        }
    }
}