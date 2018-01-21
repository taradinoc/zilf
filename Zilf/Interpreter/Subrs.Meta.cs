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

using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Compiler.Builtins;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Language.Signatures;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        /// <exception cref="InterpreterError">The file was not found or could not be loaded.</exception>
        [NotNull]
        static ZilObject PerformLoadFile([NotNull] Context ctx, [NotNull] string file, [NotNull] string name)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(file != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            try
            {
                var newFile = ctx.FindIncludeFile(file);

                using (ctx.PushFileContext(newFile))
                {
                    using (var stream = ctx.OpenFile(newFile, false))
                    {
                        Program.Evaluate(ctx, stream);
                    }
                    return ZilString.FromString("DONE");
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

        /// <exception cref="InterpreterError">The file was not found or could not be loaded.</exception>
        [NotNull]
        [Subr("INSERT-FILE")]
        [Subr("FLOAD")]
        [Subr("XFLOAD")]
        public static ZilObject INSERT_FILE([NotNull] Context ctx, [NotNull] string file, [ItemNotNull] [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(file != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx, args);
         
            // we ignore arguments after the first

            return PerformLoadFile(ctx, file, "INSERT-FILE");
        }

        /// <exception cref="InterpreterError">Unrecognized flag.</exception>
        [NotNull]
        [Subr("FILE-FLAGS")]
        public static ZilObject FILE_FLAGS([NotNull] Context ctx, [NotNull] ZilAtom[] flags)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(flags != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

                    case StdAtom.SENTENCE_ENDS_P:
                        newFlags |= FileFlags.SentenceEnds;
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "FILE-FLAGS", "flag", atom);
                }
            }

            ctx.CurrentFile.Flags = newFlags;
            return ctx.TRUE;
        }

        /// <exception cref="InterpreterError">The section has already been referenced.</exception>
        [NotNull]
        [Subr("DELAY-DEFINITION")]
        public static ZilObject DELAY_DEFINITION([NotNull] Context ctx, [NotNull] ZilAtom name)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx); 
            
            name = ctx.ZEnvironment.InternGlobalName(name);

            if (ctx.GetProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION)) != null)
                throw new InterpreterError(InterpreterMessages._0_Section_Has_Already_Been_Referenced_1, "DELAY-DEFINITION", name);

            ctx.PutProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION), ctx.GetStdAtom(StdAtom.DELAY_DEFINITION));
            return name;
        }

        /// <exception cref="InterpreterError">The section has already been inserted, or a replacement has already been defined, or it is in a bad state.</exception>
        [FSubr("REPLACE-DEFINITION")]
        public static ZilResult REPLACE_DEFINITION([NotNull] Context ctx, [NotNull] ZilAtom name, [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(body != null);
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

            if (state == ctx.GetStdAtom(StdAtom.DELAY_DEFINITION))
            {
                // insert the replacement now
                ctx.PutProp(name, replaceAtom, replaceAtom);
                return ZilObject.EvalProgram(ctx, body);
            }

            if (state == replaceAtom || state == ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION))
                throw new InterpreterError(InterpreterMessages._0_Section_Has_Already_Been_Inserted_1, "REPLACE-DEFINITION", name);

            if (state is ZilVector)
                throw new InterpreterError(InterpreterMessages._0_Duplicate_Replacement_For_Section_1, "REPLACE-DEFINITION", name);

            throw new InterpreterError(InterpreterMessages._0_Bad_State_1, "REPLACE-DEFINITION", state);
        }

        /// <exception cref="InterpreterError">The section is in a bad state.</exception>
        [FSubr("DEFAULT-DEFINITION")]
        public static ZilResult DEFAULT_DEFINITION([NotNull] Context ctx, ZilAtom name, [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
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

            if (state == replaceAtom || state == ctx.GetStdAtom(StdAtom.DELAY_DEFINITION))
            {
                // ignore the default
                return name;
            }

            if (state is ZilVector vec)
            {
                // insert the replacement now
                ctx.PutProp(name, replaceAtom, replaceAtom);
                return ZilObject.EvalProgram(ctx, vec.ToArray());
            }

            if (state == ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION))
            {
                ctx.HandleError(new InterpreterError(InterpreterMessages._0_Duplicate_Default_For_Section_1,
                    "DEFAULT-DEFINITION", name));
                return ctx.FALSE;
            }

            throw new InterpreterError(InterpreterMessages._0_Bad_State_1, "DEFAULT-DEFINITION", state);
        }

        [NotNull]
        [Subr("COMPILATION-FLAG")]
        public static ZilObject COMPILATION_FLAG([NotNull] Context ctx,
            AtomParams.StringOrAtom name, [CanBeNull] ZilObject value = null)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var atom = name.GetAtom(ctx);
            ctx.DefineCompilationFlag(atom, value ?? ctx.TRUE, true);
            return atom;
        }

        [NotNull]
        [Subr("COMPILATION-FLAG-DEFAULT")]
        public static ZilObject COMPILATION_FLAG_DEFAULT([NotNull] Context ctx,
            AtomParams.StringOrAtom name, [NotNull] ZilObject value)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var atom = name.GetAtom(ctx);
            ctx.DefineCompilationFlag(atom, value);
            return atom;
        }

        [NotNull]
        [Subr("COMPILATION-FLAG-VALUE")]
        public static ZilObject COMPILATION_FLAG_VALUE([NotNull] Context ctx,
            AtomParams.StringOrAtom name)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var atom = name.GetAtom(ctx);
            return ctx.GetCompilationFlagValue(atom) ?? ctx.FALSE;
        }

        [FSubr("IFFLAG")]
        public static ZilResult IFFLAG([NotNull] Context ctx, [NotNull] [Required] CondClause[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            SubrContracts(ctx);

            foreach (var clause in args)
            {
                bool match;
                ZilObject value;

                switch (clause.Condition)
                {
                    case ZilAtom atom when (value = ctx.GetCompilationFlagValue(atom)) != null:
                    case ZilString str when (value = ctx.GetCompilationFlagValue(str.Text)) != null:
                        // name of a defined compilation flag
                        match = value.IsTrue;
                        break;
                    case ZilForm form:
                        form = SubstituteIfflagForm(ctx, form);
                        var zr = form.Eval(ctx);
                        if (zr.ShouldPass())
                            return zr;
                        match = ((ZilObject)zr).IsTrue;
                        break;
                    default:
                        match = true;
                        break;
                }

                if (!match)
                    continue;

                ZilResult result = clause.Condition;

                foreach (var expr in clause.Body)
                {
                    result = expr.Eval(ctx);
                    if (result.ShouldPass())
                        break;
                }

                return result;
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
        [NotNull]
        internal static ZilForm SubstituteIfflagForm([NotNull] Context ctx, [NotNull] ZilForm form)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(form != null);
            Contract.Ensures(Contract.Result<ZilForm>() != null);
            var body = form.Select(zo =>
            {
                ZilObject value;

                switch (zo)
                {
                    case ZilAtom atom when ((value = ctx.GetCompilationFlagValue(atom)) != null):
                        return value;

                    default:
                        return zo;
                }
            });

            return new ZilForm(body) { SourceLine = form.SourceLine };
        }

        [NotNull]
        [Subr("TIME")]
        public static ZilObject TIME(Context ctx)
        {
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            // TODO: measure actual CPU time
            return new ZilFix(1);
        }

        [Subr("QUIT")]
        public static ZilObject QUIT([NotNull] Context ctx, ZilObject exitCode = null)
        {
            Contract.Requires(ctx != null);
            SubrContracts(ctx);

            int code;
            switch (exitCode)
            {
                case null:
                    code = 0;
                    break;

                case ZilFix fix:
                    code = fix.Value;
                    break;

                default:
                    code = 4;
                    break;
            }

            Environment.Exit(code);

            // shouldn't get here
            return ctx.TRUE;
        }

        [NotNull]
        [Subr]
        [Subr("STACK")]
        [Subr("SNAME")]
        public static ZilObject ID([NotNull] Context ctx, ZilObject arg)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return arg;
        }

        [NotNull]
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
        public static ZilObject SubrIgnored([NotNull] Context ctx, [ItemNotNull] [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx, args);
            
            // nada
            return ctx.FALSE;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.GC.Collect")]
        [NotNull]
        [Subr]
        public static ZilObject GC([NotNull] Context ctx, [ItemNotNull] [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx, args);

            System.GC.Collect();
            return ctx.TRUE;
        }

        /// <exception cref="InterpreterError">Always thrown.</exception>
        [Subr]
        public static ZilObject ERROR([NotNull] Context ctx, [ItemNotNull] [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            SubrContracts(ctx, args);

            throw new InterpreterError(
                InterpreterMessages.UserSpecifiedError_0_1,
                "ERROR",
                string.Join(" ", args.Select(a => a.ToStringContext(ctx, false))));
        }

        #region IDE Help

        [NotNull]
        [Subr("DESC-BUILTINS", ObList = "YOMIN")]
        public static ZilObject DESCRIBE_BUILTINS([NotNull] Context ctx)
        {
            SubrContracts(ctx);

            var result = new JObject();

            // Z-code builtins
            foreach (var name in ZBuiltins.GetBuiltinNames())
            {
                var jsigs = ZBuiltins.GetBuiltinSignatures(name).Select(JsonDescriber.Describe);
                result[name] = new JArray(jsigs);
            }

            // add SUBRs
            foreach (var (name, mi, isFSubr) in ctx.GetSubrDefinitions())
            {
                var sig = SubrSignature.FromMethodInfo(mi, isFSubr);
                var desc = JsonDescriber.Describe(sig);

                var array = (JArray)result[name];
                if (array == null)
                {
                    result[name] = new JArray(desc);
                }
                else
                {
                    array.Add(desc);
                }
            }

            return ZilString.FromString(result.ToString());
        }

        #endregion
    }
}
