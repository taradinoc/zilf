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
using System.Text.Json.Nodes;
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
        static ZilString PerformLoadFile(Context ctx, string file, string name)
        {
            try
            {
                var newFile = ctx.FindIncludeFile(file);

                using (ctx.PushFileContext(Path.GetFullPath(newFile)))
                {
                    using var stream = ctx.FileSystem.OpenForReading(newFile);
                    Program.Evaluate(ctx, stream);
                    return ZilString.FromString("DONE");
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_File_Not_Found_1, name, file, ex);
            }
            catch (IOException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_Error_Loading_File_1, name, ex.Message, ex);
            }
        }

        /// <summary>
        /// Loads and evaluates ZIL code from a file.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="file">The name of the file to load.</param>
        /// <param name="args">Ignored.</param>
        /// <returns>The string "DONE".</returns>
        /// <exception cref="InterpreterError">The file was not found or could not be loaded.</exception>
        [Subr("INSERT-FILE")]
        [Subr("FLOAD")]
        [Subr("XFLOAD")]
        public static ZilObject INSERT_FILE(Context ctx, string file, ZilObject[] args)
        {
            // we ignore arguments after the first

            return PerformLoadFile(ctx, file, "INSERT-FILE");
        }

        /// <summary>
        /// Sets compiler options for the current file.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="flags">A set of flags to apply to definitions in the rest of the current file.</param>
        /// <returns></returns>
        /// <exception cref="InterpreterError">Unrecognized flag.</exception>
        [Subr("FILE-FLAGS")]
        public static ZilObject FILE_FLAGS(Context ctx, ZilAtom[] flags)
        {
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

                    case StdAtom.KEEP_ROUTINES_P:
                        newFlags |= FileFlags.KeepRoutines;
                        break;

                    case StdAtom.UNUSED_ROUTINES_P:
                        newFlags |= FileFlags.SuppressUnusedRoutineWarnings;
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_File_Flag_1, "FILE-FLAGS", atom);
                }
            }

            ctx.CurrentFile.Flags = newFlags;
            return ctx.TRUE;
        }

        /// <summary>
        /// Marks an upcoming definition section as having a delayed replacement,
        /// so that it won't be inserted when the definition is encountered.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InterpreterError">The section has already been referenced.</exception>
        [Subr("DELAY-DEFINITION")]
        public static ZilObject DELAY_DEFINITION(Context ctx, ZilAtom name)
        {
            name = ctx.ZEnvironment.InternGlobalName(name);

            if (ctx.GetProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION)) != null)
                throw new InterpreterError(InterpreterMessages._0_Section_Has_Already_Been_Referenced_1, "DELAY-DEFINITION", name);

            ctx.PutProp(name, ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION), ctx.GetStdAtom(StdAtom.DELAY_DEFINITION));
            return name;
        }

        /// <summary>
        /// Defines a replacement section to be inserted in place of an upcoming definition section when it's encountered,
        /// or to be inserted immediately if the definition has been delayed.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the definition section to replace.</param>
        /// <param name="body">The content with which to replace the section.</param>
        /// <returns></returns>
        /// <exception cref="InterpreterError">The section has already been inserted,
        /// or a replacement has already been defined, or it is in a bad state.</exception>
        [FSubr("REPLACE-DEFINITION")]
        public static ZilResult REPLACE_DEFINITION(Context ctx, ZilAtom name, [Required] ZilObject[] body)
        {
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

        /// <summary>
        /// Defines a named definition section, which is inserted immediately unless a replacement or delay has been specified.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the definition section.</param>
        /// <param name="body">The content of the definition section.</param>
        /// <returns>The result of evaluating the last expression in the body if it is inserted immediately; otherwise, the name of the section.</returns>
        /// <exception cref="InterpreterError">The section is in a bad state.</exception>
        [FSubr("DEFAULT-DEFINITION")]
        public static ZilResult DEFAULT_DEFINITION(Context ctx, ZilAtom name, [Required] ZilObject[] body)
        {
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

        /// <summary>
        /// Defines a compilation flag with a specified value, as well as macros for checking its value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the compilation flag.</param>
        /// <param name="value">The value of the compilation flag. If omitted, defaults to T.</param>
        /// <returns>The name of the flag.</returns>
        [Subr("COMPILATION-FLAG")]
        public static ZilObject COMPILATION_FLAG(Context ctx,
            AtomParams.StringOrAtom name, ZilObject? value = null)
        {
            var atom = name.GetAtom(ctx);
            ctx.DefineCompilationFlag(atom, value ?? ctx.TRUE, true);
            return atom;
        }

        /// <summary>
        /// Defines a compilation flag with a specified default value,
        /// as well as macros for checking its value, if it is not already defined.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the compilation flag.</param>
        /// <param name="value">The value of the compilation flag.</param>
        /// <returns>The name of the flag.</returns>
        [Subr("COMPILATION-FLAG-DEFAULT")]
        public static ZilObject COMPILATION_FLAG_DEFAULT(Context ctx,
            AtomParams.StringOrAtom name, ZilObject value)
        {
            var atom = name.GetAtom(ctx);
            ctx.DefineCompilationFlag(atom, value);
            return atom;
        }

        /// <summary>
        /// Returns the value of a compilation flag.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the compilation flag.</param>
        /// <returns>The value of the compilation flag, or false if not defined.</returns>
        [Subr("COMPILATION-FLAG-VALUE")]
        public static ZilObject COMPILATION_FLAG_VALUE(Context ctx,
            AtomParams.StringOrAtom name)
        {
            var atom = name.GetAtom(ctx);
            return ctx.GetCompilationFlagValue(atom) ?? ctx.FALSE;
        }

        /// <summary>
        /// Evaluates conditional clauses based on compilation flags.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">A sequence of conditional clauses, where the condition of each one is
        /// evaluated after substituting the names of compilation flags with their values.</param>
        /// <returns>The result of the first matching clause, or false if no clauses match.</returns>
        [FSubr("IFFLAG")]
        public static ZilResult IFFLAG(Context ctx, [Required] CondClause[] args)
        {
            foreach (var clause in args)
            {
                bool match;
                ZilObject? value;

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
        internal static ZilForm SubstituteIfflagForm(Context ctx, ZilForm form)
        {
            var body = form.Select(zo =>
            {
                if (zo is ZilAtom atom && ctx.GetCompilationFlagValue(atom) is { } value)
                {
                    return value;
                }

                return zo;
            });

            return new ZilForm(body) { SourceLine = form.SourceLine };
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>The integer 1.</returns>
        [Subr("TIME")]
        public static ZilObject TIME(Context ctx)
        {
            // TODO: measure actual CPU time
            return new ZilFix(1);
        }

        /// <summary>
        /// Quits the interpreter with an optional exit code.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="exitCode">The exit code. If omitted, defaults to zero.</param>
        /// <returns>False if quitting is disabled; otherwise, does not return.</returns>
        [Subr("QUIT")]
        public static ZilObject QUIT(Context ctx, ZilObject? exitCode = null)
        {
            // quitting the playground REPL breaks the site, so QUIT can be disabled
            if (!ctx.Quittable)
            {
                return ctx.FALSE;
            }

            var code = exitCode switch
            {
                null => 0,
                ZilFix fix => fix.Value,
                _ => 4
            };

            Environment.Exit(code);

            // shouldn't get here
            return ctx.TRUE;
        }

        /// <summary>
        /// Returns its argument.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">A value.</param>
        /// <returns>The value.</returns>
        [Subr]
        [Subr("STACK")]
        [Subr("SNAME")]
        public static ZilObject ID(Context ctx, ZilObject arg)
        {
            return arg;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">Ignored.</param>
        /// <returns>False.</returns>
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
            // nada
            return ctx.FALSE;
        }

        /// <summary>
        /// Performs garbage collection immediately.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">Ignored.</param>
        /// <returns>True.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.GC.Collect")]
        [Subr]
        public static ZilObject GC(Context ctx, ZilObject[] args)
        {
            System.GC.Collect();
            return ctx.TRUE;
        }

        /// <summary>
        /// Throws an interpreter error with a user-specified message.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">Any values. They will be converted to strings and joined with spaces to form the error message.</param>
        /// <returns></returns>
        /// <exception cref="InterpreterError">Always thrown.</exception>
        [Subr]
        public static ZilObject ERROR(Context ctx, ZilObject[] args)
        {
            throw new InterpreterError(
                InterpreterMessages.UserSpecifiedError_0_1,
                "ERROR",
                string.Join(" ", args.Select(a => a.ToStringContext(ctx, false))));
        }

        /// <summary>
        /// Causes warnings to be treated as errors.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="enabled">True to make warnings be treated as errors, false otherwise.</param>
        /// <returns>True.</returns>
        [Subr("WARN-AS-ERROR?")]
        public static ZilObject WARN_AS_ERROR_P(Context ctx, bool enabled = true)
        {
            ctx.WarningsAsErrors = enabled;
            return ctx.TRUE;
        }

        public static class WarningParams
        {
            [ZilSequenceParam]
            [ParamDesc("all-none-or-codes")]
            public struct CodesOrWildcard
            {
#pragma warning disable CS0649
                [Either(typeof(Wildcard), typeof(AtomParams.StringOrAtom[]))]
                public object Content;
#pragma warning restore CS0649

                public StdAtom? GetWildcard()
                {
                    return Content is Wildcard w ? w.Atom.StdAtom : (StdAtom?)null;
                }

                public string[]? GetCodes()
                {
                    return Content is AtomParams.StringOrAtom[] sas
                        ? sas.Select(sa => sa.ToString()).ToArray()
                        : null;
                }
            }

            [ZilSequenceParam]
            public struct Wildcard
            {
#pragma warning disable CS0649
                [Decl("<OR 'ALL 'NONE>")]
                public ZilAtom Atom;
#pragma warning restore CS0649

                public StdAtom StdAtom => Atom.StdAtom;
            }
        }

        /// <summary>
        /// Controls suppression of specified warnings, or all warnings.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="codesOrWildcard">A set of warning codes (as strings or atoms), or the atoms ALL or NONE.</param>
        /// <returns></returns>
        [Subr("SUPPRESS-WARNINGS?")]
        public static ZilObject SUPPRESS_WARNINGS_P(Context ctx,
            WarningParams.CodesOrWildcard codesOrWildcard)
        {
            switch (codesOrWildcard.GetWildcard())
            {
                case StdAtom.ALL:
                    ctx.DiagnosticManager.SuppressAll();
                    break;

                case StdAtom.NONE:
                    ctx.DiagnosticManager.SuppressNone();
                    break;

                default:
                    var codes = codesOrWildcard.GetCodes();
                    Debug.Assert(codes != null);

                    foreach (var code in codes)
                        ctx.DiagnosticManager.Suppress(code);
                    break;
            }

            return ctx.TRUE;
        }

        #region IDE Help

        /// <summary>
        /// Returns a detailed JSON description of all built-in functions.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>A JSON string describing all built-in functions.</returns>
        [Subr("DESC-BUILTINS", ObList = "YOMIN")]
        public static ZilObject DESCRIBE_BUILTINS(Context ctx)
        {
            var result = new JsonObject();

            foreach (var (name, signature) in GetBuiltinSignatures(ctx))
            {
                var desc = JsonDescriber.Describe(signature);

                if (result[name] is not JsonArray array)
                {
                    result[name] = new JsonArray(desc);
                }
                else
                {
                    array.Add((JsonNode?)desc);
                }
            }

            return ZilString.FromString(result.ToJsonString());
        }

        /// <summary>
        /// Returns a JSON summary of all built-in functions.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>A JSON string summarizing all built-in functions.</returns>
        [Subr("SUMMARIZE-BUILTINS", ObList = "YOMIN")]
        public static ZilObject SUMMARIZE_BUILTINS(Context ctx)
        {
            var result = new JsonObject();

            var sigs = from pair in GetBuiltinSignatures(ctx)
                       group pair.signature by pair.name;

            foreach (var g in sigs.OrderBy(g => g.Key))
            {
                var groupItem = new JsonArray();

                foreach (var signature in g)
                {
                    var sigItem = new JsonObject()
                    {
                        ["params"] = PlainDescriber.Describe(signature)
                    };

                    switch (signature)
                    {
                        case SubrSignature _:
                            sigItem["context"] = "mdl";
                            break;

                        case ZBuiltinSignature bs:
                            sigItem["context"] = "zcode";
                            sigItem["minVersion"] = bs.MinVersion;
                            sigItem["maxVersion"] = bs.MaxVersion;
                            break;
                    }

                    if (signature.Summary != null)
                    {
                        sigItem["summary"] = signature.Summary;
                    }

                    groupItem.Add((JsonNode?)sigItem);
                }

                result.Add(g.Key, groupItem);
            }

            return ZilString.FromString(result.ToJsonString());
        }

        private static IEnumerable<(string name, ISignature signature)> GetBuiltinSignatures(Context ctx)
        {
            var zcodeSignatures =
                from name in ZBuiltins.GetBuiltinNames()
                from signature in ZBuiltins.GetBuiltinSignatures(name)
                select (name, signature);

            var subrSignatures =
                from pair in GeneratedSubrSignatureMetadata.SubrSignatures
                let name = pair.Key
                from signature in pair.Value
                select (name, signature);

            return zcodeSignatures.Concat(subrSignatures);
        }

        #endregion

        #region Blorb

        [Subr("BLORB-PICTURE")]
        public static ZilObject BLORB_PICTURE(Context ctx, string path)
        {
            byte[] data;
            try
            {
                string newPath = ctx.FindExactIncludeFile(path);
                data = File.ReadAllBytes(newPath);
            }
            catch (FileNotFoundException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_File_Not_Found_1, "BLORB-PICTURE", path, ex);
            }
            catch (IOException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_Error_Loading_File_1, "BLORB-PICTURE", ex.Message, ex);
            }

            int id = ctx.Blorb.AddPicture(data);
            return new ZilFix(id);
        }

        #endregion
    }
}
