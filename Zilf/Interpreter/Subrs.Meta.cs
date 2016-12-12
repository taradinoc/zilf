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
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        public static ZilObject PerformLoadFile(Context ctx, string file, string name)
        {
            try
            {
                string oldFile = ctx.CurrentFile;
                var newFile = ctx.FindIncludeFile(file);
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
                    return ZilString.FromString("DONE");
                }
                finally
                {
                    ctx.CurrentFile = oldFile;
                    ctx.CurrentFileFlags = oldFlags;
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_File_Not_Found_1, name, file, ex);
            }
            catch (System.IO.IOException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_Error_Loading_File_1, name, ex.Message, ex);
            }
        }

        [Subr("INSERT-FILE")]
        [Subr("FLOAD")]
        [Subr("XFLOAD")]
        public static ZilObject INSERT_FILE(Context ctx, string file, ZilObject[] args)
        {
            SubrContracts(ctx);
         
            // we ignore arguments after the first

            return PerformLoadFile(ctx, file, "INSERT-FILE");
        }

        [Subr("FILE-FLAGS")]
        public static ZilObject FILE_FLAGS(Context ctx, ZilAtom[] flags)
        {
            SubrContracts(ctx);

            var newFlags = FileFlags.None;

            foreach (var atom in flags)
            {
                switch (atom.StdAtom)
                {
                    case StdAtom.CLEAN_STACK_P:
                        newFlags |= FileFlags.CleanStack;
                        break;

                    case StdAtom.MDL_ZIL_P:
                        newFlags |= FileFlags.MdlZil;
                        break;

                    case StdAtom.ZAP_TO_SOURCE_DIRECTORY_P:
                        // ignore
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_Flag_1, "FILE-FLAGS", atom);
                }
            }

            ctx.CurrentFileFlags = newFlags;
            return ctx.TRUE;
        }

        [Subr("DELAY-DEFINITION")]
        public static ZilObject DELAY_DEFINITION(Context ctx, ZilAtom name)
        {
            SubrContracts(ctx); 
            
            name = ctx.ZEnvironment.InternGlobalName(name);

            if (ctx.GetProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION)) != null)
                throw new InterpreterError(InterpreterMessages._0_Section_Has_Already_Been_Referenced_1, "DELAY-DEFINITION", name);

            ctx.PutProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION), ctx.GetStdAtom(StdAtom.DELAY_DEFINITION));
            return name;
        }

        [FSubr("REPLACE-DEFINITION")]
        public static ZilObject REPLACE_DEFINITION(Context ctx, ZilAtom name, [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            name = ctx.ZEnvironment.InternGlobalName(name);

            var replaceAtom = ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION);
            var state = ctx.GetProp(name, replaceAtom);

            if (state == null)
            {
                // store the replacement now, insert it at the DEFAULT-DEFINITION
                ctx.PutProp(name, replaceAtom, new ZilVector(body));
                return name;
            }
            else if (state == ctx.GetStdAtom(StdAtom.DELAY_DEFINITION))
            {
                // insert the replacement now
                ctx.PutProp(name, replaceAtom, replaceAtom);
                return ZilObject.EvalProgram(ctx, body);
            }
            else if (state == replaceAtom || state == ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION))
            {
                throw new InterpreterError(InterpreterMessages._0_Section_Has_Already_Been_Inserted_1, "REPLACE-DEFINITION", name);
            }
            else if (state is ZilVector)
            {
                throw new InterpreterError(InterpreterMessages._0_Duplicate_Replacement_For_Section_1, "REPLACE-DEFINITION", name);
            }
            else
            {
                throw new InterpreterError(InterpreterMessages._0_Bad_State_1, "REPLACE-DEFINITION", state);
            }
        }

        [FSubr("DEFAULT-DEFINITION")]
        public static ZilObject DEFAULT_DEFINITION(Context ctx, ZilAtom name, [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            name = ctx.ZEnvironment.InternGlobalName(name);

            var replaceAtom = ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION);
            var state = ctx.GetProp(name, replaceAtom);

            if (state == null)
            {
                // no replacement, insert the default now
                ctx.PutProp(name, replaceAtom, ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION));
                return ZilObject.EvalProgram(ctx, body);
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
                throw new InterpreterError(InterpreterMessages._0_Duplicate_Default_For_Section_1, "DEFAULT-DEFINITION", name);
            }
            else
            {
                throw new InterpreterError(InterpreterMessages._0_Bad_State_1, "DEFAULT-DEFINITION", state);
            }
        }

        [Subr("COMPILATION-FLAG")]
        public static ZilObject COMPILATION_FLAG(Context ctx,
            AtomParams.StringOrAtom name, ZilObject value = null)
        {
            SubrContracts(ctx);

            var atom = name.GetAtom(ctx);
            ctx.DefineCompilationFlag(atom, value ?? ctx.TRUE, redefine: true);
            return atom;
        }

        [Subr("COMPILATION-FLAG-DEFAULT")]
        public static ZilObject COMPILATION_FLAG_DEFAULT(Context ctx,
            AtomParams.StringOrAtom name, ZilObject value)
        {
            SubrContracts(ctx);

            var atom = name.GetAtom(ctx);
            ctx.DefineCompilationFlag(atom, value, redefine: false);
            return atom;
        }

        [Subr("COMPILATION-FLAG-VALUE")]
        public static ZilObject COMPILATION_FLAG_VALUE(Context ctx,
            AtomParams.StringOrAtom name)
        {
            SubrContracts(ctx);

            var atom = name.GetAtom(ctx);
            return ctx.GetCompilationFlagValue(atom) ?? ctx.FALSE;
        }

        [FSubr("IFFLAG")]
        public static ZilObject IFFLAG(Context ctx, [Required] CondClause[] args)
        {
            SubrContracts(ctx);

            foreach (var clause in args)
            {
                bool match;

                ZilAtom atom;
                ZilString str;
                ZilForm form;
                ZilObject value;

                if (((atom = clause.Condition as ZilAtom) != null &&
                     (value = ctx.GetCompilationFlagValue(atom)) != null) ||
                    ((str = clause.Condition as ZilString) != null &&
                    (value = ctx.GetCompilationFlagValue(str.Text)) != null))
                {
                    // name of a defined compilation flag
                    match = value.IsTrue;
                }
                else if ((form = clause.Condition as ZilForm) != null)
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
                    var result = clause.Condition;
                    foreach (var expr in clause.Body)
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
        public static ZilObject TIME(Context ctx)
        {
            SubrContracts(ctx);

            // TODO: measure actual CPU time
            return new ZilFix(1);
        }

        [Subr("QUIT")]
        public static ZilObject QUIT(Context ctx, ZilObject exitCode = null)
        {
            SubrContracts(ctx);

            int code;
            if (exitCode == null)
            {
                code = 0;
            }
            else if (exitCode is ZilFix)
            {
                code = ((ZilFix)exitCode).Value;
            }
            else
            {
                code = 4;
            }

            Environment.Exit(code);

            // shouldn't get here
            return ctx.TRUE;
        }

        [Subr]
        [Subr("STACK")]
        [Subr("SNAME")]
        public static ZilObject ID(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            return arg;
        }

        [Subr("GC-MON")]
        [Subr("BLOAT")]
        [Subr("ZSTR-ON")]
        [Subr("ZSTR-OFF")]
        [Subr("ENDLOAD")]
        [Subr("PUT-PURE-HERE")]
        [Subr("DEFAULTS-DEFINED")]
        [Subr("CHECKPOINT")]
        [Subr("BEGIN-SEGMENT")]
        [Subr("END-SEGMENT")]
        [Subr("DEFINE-SEGMENT")]
        [Subr("FREQUENT-WORDS?")]
        [Subr("NEVER-ZAP-TO-SOURCE-DIRECTORY?")]
        [Subr("ASK-FOR-PICTURE-FILE?")]
        [Subr("PICFILE")]
        public static ZilObject SubrIgnored(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);
            
            // nada
            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject GC(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx);

            System.GC.Collect();
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject ERROR(Context ctx, ZilObject[] args)
        {
            throw new InterpreterError(InterpreterMessages.UserSpecifiedError_0_1, "ERROR", string.Join(" ", args.Select(a => a.ToStringContext(ctx, false))));
        }
    }
}
