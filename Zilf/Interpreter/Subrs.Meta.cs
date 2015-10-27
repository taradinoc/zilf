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
using System;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        public static ZilObject PerformLoadFile(Context ctx, string file, string name)
        {
            try
            {
                string oldFile = ctx.CurrentFile;
                string newFile = ctx.FindIncludeFile(file);
                var oldFlags = ctx.CurrentFileFlags;

                ctx.CurrentFile = newFile;
                ctx.CurrentFileFlags = FileFlags.None;
                try
                {
                    using (var stream = ctx.OpenFile(newFile, false))
                    {
                        Antlr.Runtime.ICharStream inputStream =
                            new Antlr.Runtime.ANTLRInputStream(stream);
                        Program.Evaluate(ctx, inputStream);
                    }
                    return new ZilString("DONE");
                }
                finally
                {
                    ctx.CurrentFile = oldFile;
                    ctx.CurrentFileFlags = oldFlags;
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                throw new InterpreterError(name + ": file not found: " + file, ex);
            }
            catch (System.IO.IOException ex)
            {
                throw new InterpreterError(name + ": error loading file: " +
                    ex.Message, ex);
            }
        }

        [Subr("INSERT-FILE")]
        [Subr("FLOAD")]
        [Subr("XFLOAD")]
        public static ZilObject INSERT_FILE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);
         
            // we ignore arguments after the first
            if (args.Length == 0)
                throw new InterpreterError("INSERT-FILE", 1, 0);

            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.STRING)
                throw new InterpreterError("INSERT-FILE: first arg must be a string");

            string file = args[0].ToStringContext(ctx, true);

            return PerformLoadFile(ctx, file, "INSERT-FILE");
        }

        [Subr("FILE-FLAGS")]
        public static ZilObject FILE_FLAGS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            var newFlags = FileFlags.None;

            foreach (var arg in args)
            {
                var atom = arg as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("FILE-FLAGS: all args must be atoms");

                switch (atom.StdAtom)
                {
                    case StdAtom.CLEAN_STACK_P:
                        newFlags |= FileFlags.CleanStack;
                        break;

                    case StdAtom.MDL_ZIL_P:
                        newFlags |= FileFlags.MdlZil;
                        break;

                    default:
                        throw new InterpreterError("FILE-FLAGS: unrecognized flag: " + atom);
                }
            }

            ctx.CurrentFileFlags = newFlags;
            return ctx.TRUE;
        }

        [Subr("DELAY-DEFINITION")]
        public static ZilObject DELAY_DEFINITION(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args); 
            
            if (args.Length != 1)
                throw new InterpreterError("DELAY-DEFINITION", 1, 1);

            var name = args[0] as ZilAtom;
            if (name == null)
                throw new InterpreterError("DELAY-DEFINITION: first arg must be an atom");

            if (ctx.GetProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION)) != null)
                throw new InterpreterError("DELAY-DEFINITION: section has already been referenced: " + name);

            ctx.PutProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION), ctx.GetStdAtom(StdAtom.DELAY_DEFINITION));
            return name;
        }

        [FSubr("REPLACE-DEFINITION")]
        public static ZilObject REPLACE_DEFINITION(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("REPLACE-DEFINITION", 2, 0);

            var name = args[0] as ZilAtom;
            if (name == null)
                throw new InterpreterError("REPLACE-DEFINITION: first arg must be an atom");

            var replaceAtom = ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION);
            var state = ctx.GetProp(name, replaceAtom);

            if (state == null)
            {
                // store the replacement now, insert it at the DEFAULT-DEFINITION
                ctx.PutProp(name, replaceAtom, new ZilVector(args.Skip(1).ToArray()));
                return name;
            }
            else if (state == ctx.GetStdAtom(StdAtom.DELAY_DEFINITION))
            {
                // insert the replacement now
                ctx.PutProp(name, replaceAtom, replaceAtom);
                return ZilObject.EvalProgram(ctx, args.Skip(1).ToArray());
            }
            else if (state == replaceAtom || state == ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION))
            {
                throw new InterpreterError("REPLACE-DEFINITION: section has already been inserted: " + name);
            }
            else if (state is ZilVector)
            {
                throw new InterpreterError("REPLACE-DEFINITION: duplicate replacement for section: " + name);
            }
            else
            {
                throw new InterpreterError("REPLACE-DEFINITION: bad state: " + state);
            }
        }

        [FSubr("DEFAULT-DEFINITION")]
        public static ZilObject DEFAULT_DEFINITION(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("DEFAULT-DEFINITION", 2, 0);

            var name = args[0] as ZilAtom;
            if (name == null)
                throw new InterpreterError("DEFAULT-DEFINITION: first arg must be an atom");

            var replaceAtom = ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION);
            var state = ctx.GetProp(name, replaceAtom);

            if (state == null)
            {
                // no replacement, insert the default now
                ctx.PutProp(name, replaceAtom, ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION));
                return ZilObject.EvalProgram(ctx, args.Skip(1).ToArray());
            }
            else if (state == replaceAtom || state == ctx.GetStdAtom(StdAtom.DELAY_DEFINITION))
            {
                // ignore the default
                return name;
            }
            else if (state is ZilVector)
            {
                // insert the replacement now
                ctx.PutProp(name, replaceAtom, replaceAtom);
                return ZilObject.EvalProgram(ctx, ((ZilVector)state).ToArray());
            }
            else if (state == ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION))
            {
                throw new InterpreterError("DEFAULT-DEFINITION: duplicate default for section: " + name);
            }
            else
            {
                throw new InterpreterError("DEFAULT-DEFINITION: bad state: " + state);
            }
        }

        [Subr("COMPILATION-FLAG")]
        public static ZilObject COMPILATION_FLAG(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("COMPILATION-FLAG", 1, 2);

            var atom = args[0] as ZilAtom;
            if (atom == null)
            {
                var str = args[0] as ZilString;
                if (str != null)
                {
                    atom = ZilAtom.Parse(str.Text, ctx);
                }
                else
                {
                    throw new InterpreterError("COMPILATION-FLAG: flag name must be an atom or string");
                }
            }

            var value = (args.Length >= 2) ? args[1] : ctx.TRUE;

            ctx.DefineCompilationFlag(atom, value, redefine: true);
            return atom;
        }

        [Subr("COMPILATION-FLAG-DEFAULT")]
        public static ZilObject COMPILATION_FLAG_DEFAULT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("COMPILATION-FLAG-DEFAULT", 2, 2);

            var atom = args[0] as ZilAtom;
            if (atom == null)
            {
                var str = args[0] as ZilString;
                if (str != null)
                {
                    atom = ZilAtom.Parse(str.Text, ctx);
                }
                else
                {
                    throw new InterpreterError("COMPILATION-FLAG: flag name must be an atom or string");
                }
            }

            ctx.DefineCompilationFlag(atom, args[1], redefine: false);
            return atom;
        }

        [Subr("COMPILATION-FLAG-VALUE")]
        public static ZilObject COMPILATION_FLAG_VALUE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("COMPILATION-FLAG-VALUE", 1, 1);

            var atom = args[0] as ZilAtom;
            if (atom == null)
            {
                var str = args[0] as ZilString;
                if (str != null)
                {
                    atom = ZilAtom.Parse(str.Text, ctx);
                }
                else
                {
                    throw new InterpreterError("COMPILATION-FLAG-VALUE: flag name must be an atom or string");
                }
            }

            var result = ctx.GetCompilationFlagValue(atom);

            if (result == null)
                throw new InterpreterError("COMPILATION-FLAG-VALUE: no such flag: " + atom);

            return result;
        }

        [FSubr("IFFLAG")]
        public static ZilObject IFFLAG(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1)
                throw new InterpreterError("IFFLAG", 1, 0);

            foreach (var clause in args)
            {
                var list = clause as ZilList;
                if (list == null || list.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("IFFLAG: args must be lists");

                if (list.IsEmpty)
                    throw new InterpreterError("IFFLAG: lists must be non-empty");

                bool match;

                ZilAtom atom;
                ZilForm form;
                ZilObject value;

                if ((atom = list.First as ZilAtom) != null &&
                    (value = ctx.GetCompilationFlagValue(atom)) != null)
                {
                    // name of a defined compilation flag
                    match = value.IsTrue;
                }
                else if ((form = list.First as ZilForm) != null)
                {
                    form = SubstituteIfflagForm(ctx, form);
                    match = form.Eval(ctx).IsTrue;
                }
                else
                {
                    match = true;
                }

                if (match)
                {
                    var result = list.First;
                    foreach (var expr in list.Rest)
                        result = expr.Eval(ctx);
                    return result;
                }
            }

            // no match
            return ctx.FALSE;
        }

        /// <summary>
        /// Copies a form, replacing any direct children that are atoms naming compilation
        /// flags with the corresponding values.
        /// </summary>
        /// <param name="ctx">The context containing the compilation flags.</param>
        /// <param name="form">The original form.</param>
        /// <returns>A new form, containing elements from the original and/or
        /// the values of compilation flags.</returns>
        internal static ZilForm SubstituteIfflagForm(Context ctx, ZilForm form)
        {
            var result = new ZilForm(
                form.Select(zo =>
                {
                    ZilAtom atom;
                    ZilObject value;
                    if ((atom = zo as ZilAtom) != null &&
                        (value = ctx.GetCompilationFlagValue(atom)) != null)
                    {
                        return value;
                    }
                    else
                    {
                        return zo;
                    }
                }));

            result.SourceLine = form.SourceLine;
            return result;
        }

        [Subr("TIME")]
        public static ZilObject TIME(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // TODO: measure actual CPU time
            return new ZilFix(1);
        }

        [Subr("QUIT")]
        public static ZilObject QUIT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                Environment.Exit(0);
            else if (args[0] is ZilFix)
                Environment.Exit(((ZilFix)args[0]).Value);
            else
                Environment.Exit(4);

            return ctx.TRUE;
        }

        [Subr]
        [Subr("STACK")]
        public static ZilObject ID(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("ID", 1, 1);

            return args[0];
        }

        [Subr]
        public static ZilObject SNAME(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("SNAME", 1, 1);

            return args[0];
        }

        [Subr("GC-MON")]
        [Subr("GC")]
        [Subr("BLOAT")]
        [Subr("ZSTR-ON")]
        [Subr("ZSTR-OFF")]
        [Subr("ENDLOAD")]
        [Subr("PUT-PURE-HERE")]
        [Subr("DEFAULTS-DEFINED")]
        [Subr("ZSECTION")]
        [Subr("ZZSECTION")]
        [Subr("ENDSECTION")]
        [Subr("INCLUDE")]
        [Subr("CHECKPOINT")]
        public static ZilObject SubrIgnored(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);
            
            // nada
            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject ERROR(Context ctx, ZilObject[] args)
        {
            throw new InterpreterError("ERROR: " + string.Join(" ", args.Select(a => a.ToStringContext(ctx, false))));
        }
    }
}
