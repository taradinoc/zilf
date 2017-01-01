/* Copyright 2010-2016 Jesse McGrew
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
using Antlr.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Language;
using Zilf.Language.Lexing;

namespace Zilf.Interpreter.Values
{
    [ContractClass(typeof(ZilObjectContracts))]
    abstract class ZilObject : IProvideSourceLine
    {
        /// <summary>
        /// Gets or sets a value indicating the object's source code location.
        /// </summary>
        public virtual ISourceLine SourceLine { get; set; }

        /// <summary>
        /// Translates a syntax tree (from the Antlr parser) to ZIL objects.
        /// </summary>
        /// <remarks>
        /// This approximates the behavior of MDL's READ routine.
        /// </remarks>
        /// <param name="tree">The root of a syntax tree, or null.</param>
        /// <param name="ctx">The current context.</param>
        /// <param name="templateParams">An array of objects to replace
        /// template placeholders with, or <b>null</b>.</param>
        /// <param name="src">The source line to use for translated structures, or <b>null</b> to
        /// generate it from <see cref="Context.CurrentFile"/> and <see cref="ITree.Line"/>.</param>
        /// <returns>A sequence of zero or more ZIL objects, depending
        /// on whether <paramref name="tree"/> was null, a parsed expression,
        /// or the phantom root of a tree containing multiple
        /// expressions.</returns>
        public static IEnumerable<ZilObject> ReadAllFromAST(ITree tree, Context ctx, ZilObject[] templateParams, ISourceLine src)
        {
            Contract.Requires(tree != null);
            Contract.Requires(ctx != null);
            //Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);
            //Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<ZilObject>>(), r => r != null));

            if (tree == null)
                return EmptyArray;

            /* obj might be an expression if the source file only contained
             * one expression. otherwise it's a fake node created by antlr to
             * contain multiple expressions. */
            if (tree.Type == 0)
            {
                // multiple expressions
                return Enumerable.Range(0, tree.ChildCount)
                    .SelectMany(i => ReadOneFromAST(tree.GetChild(i), ctx, templateParams, src));
            }

            // just one
            return ReadOneFromAST(tree, ctx, templateParams, src);
        }

        /// <summary>
        /// Translates the children of a syntax tree node into ZIL objects.
        /// </summary>
        /// <param name="tree">The tree node.</param>
        /// <param name="ctx">The current context.</param>
        /// <param name="templateParams">An array of objects to replace
        /// template placeholders with, or <b>null</b>.</param>
        /// <param name="src">The source line to use for translated structures, or <b>null</b> to
        /// generate it from <see cref="Context.CurrentFile"/> and <see cref="ITree.Line"/>.</param>
        /// <returns>The array of translated child objects.</returns>
        static ZilObject[] ReadChildrenFromAST(ITree tree, Context ctx, ZilObject[] templateParams, ISourceLine src)
        {
            Contract.Requires(tree != null);
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject[]>() != null);
            Contract.Ensures(Contract.ForAll(Contract.Result<ZilObject[]>(), r => r != null));

            if (tree.ChildCount == 0)
                return new ZilObject[0];

            var result = new List<ZilObject>(tree.ChildCount);
            for (int i = 0; i < tree.ChildCount; i++)
            {
                foreach (var obj in ReadOneFromAST(tree.GetChild(i), ctx, templateParams, src))
                {
                    var expandAfter = obj as IMayExpandAfterEvaluation;

                    if (expandAfter?.ShouldExpandAfterEvaluation == true)
                    {
                        result.AddRange(expandAfter.ExpandAfterEvaluation(ctx, ctx.LocalEnvironment));
                    }
                    else
                    {
                        if (obj != null)
                            result.Add(obj);
                    }
                }
            }
            return result.ToArray();
        }

        static readonly ZilObject[] EmptyArray = new ZilObject[0];

        /// <summary>
        /// Translates a single syntax tree node into some number of ZIL objects.
        /// </summary>
        /// <param name="tree">The tree node.</param>
        /// <param name="ctx">The current context.</param>
        /// <param name="templateParams">An array of objects to replace
        /// template placeholders with, or <b>null</b>.</param>
        /// <param name="src">The source line to use for translated structures, or <b>null</b> to
        /// generate it from <see cref="Context.CurrentFile"/> and <see cref="ITree.Line"/>.</param>
        /// <returns>The sequence of translated objects. This may be empty if the node
        /// could not be translated, or it may contain more than one object if the node
        /// caused a splice.</returns>
        static IEnumerable<ZilObject> ReadOneFromAST(ITree tree, Context ctx, ZilObject[] templateParams, ISourceLine src)
        {
            Contract.Requires(tree != null);
            Contract.Requires(ctx != null);

            var curSourceLine = src ?? new FileSourceLine(ctx.CurrentFile.Path, tree.Line);

            ZilObject[] children;

            using (DiagnosticContext.Push(curSourceLine))
            {
                switch (tree.Type)
                {
                    case ZilLexer.ATOM:
                        var atom = ZilAtom.Parse(tree.Text, ctx);
                        if (atom is ZilLink)
                            return new[] { ctx.GetGlobalVal(atom) };

                        return new[] { atom };
                    case ZilLexer.CHAR:
                        Contract.Assume(tree.Text.Length >= 3);
                        return new[] { new ZilChar(tree.Text[2]) };
                    case ZilLexer.COMMENT:
                        // ignore comments
                        return EmptyArray;
                    case ZilLexer.FORM:
                        children = ReadChildrenFromAST(tree, ctx, templateParams, src);
                        if (children.Length == 0)
                            return new[] { ctx.FALSE };

                        return new[] { new ZilForm(children) { SourceLine = curSourceLine } };
                    case ZilLexer.HASH:
                        return new[] { ZilHash.Parse(ctx, ReadChildrenFromAST(tree, ctx, templateParams, src)) };
                    case ZilLexer.LIST:
                        return new[] { new ZilList(ReadChildrenFromAST(tree, ctx, templateParams, src)) { SourceLine = curSourceLine } };
                    case ZilLexer.VECTOR:
                    case ZilLexer.UVECTOR:  // TODO: a real UVECTOR type?
                        return new[] { new ZilVector(ReadChildrenFromAST(tree, ctx, templateParams, src)) { SourceLine = curSourceLine } };
                    case ZilLexer.ADECL:
                        children = ReadChildrenFromAST(tree, ctx, templateParams, src);
                        Contract.Assume(children.Length == 2);
                        return new[] { new ZilAdecl(children[0], children[1]) };
                    case ZilLexer.MACRO:
                    case ZilLexer.VMACRO:
                        // expand macros
                        var inner = ReadOneFromAST(tree.GetChild(0), ctx, templateParams, src).SingleOrDefault();
                        if (inner != null)
                        {
                            using (DiagnosticContext.Push(inner.SourceLine))
                            {
                                try
                                {
                                    var result = inner.Eval(ctx);

                                    if (tree.Type == ZilLexer.MACRO)
                                        return result as ZilSplice ?? (IEnumerable<ZilObject>)new[] { result };
                                }
                                catch (ControlException ex)
                                {
                                    throw new InterpreterError(inner.SourceLine, InterpreterMessages.Misplaced_0, ex.Message);
                                }
                            }
                        }
                        return EmptyArray;
                    case ZilLexer.NUM:
                        return new[] { new ZilFix(ParseNumber(tree.Text)) };
                    case ZilLexer.SEGMENT:
                        return new[] { new ZilSegment(ReadOneFromAST(tree.GetChild(0), ctx, templateParams, src).Single()) };
                    case ZilLexer.STRING:
                        Contract.Assume(tree.Text.Length >= 2);
                        return new[] { ZilString.Parse(tree.Text) };
                    case ZilLexer.TEMPLATE:
                        var selector = ReadOneFromAST(tree.GetChild(0), ctx, templateParams, src).Single();
                        return ExpandTemplateToken(selector, templateParams);
                    default:
                        throw new ArgumentException("Unexpected tree type: " + tree.Type.ToString(), nameof(tree));
                }
            }
        }

        static IEnumerable<ZilObject> ExpandTemplateToken(ZilObject selector, ZilObject[] templateParams)
        {
            if (templateParams == null)
                throw new InterpreterError(InterpreterMessages.Templates_Cannot_Be_Used_Here);

            if (selector is ZilFix)
            {
                var idx = ((ZilFix)selector).Value;
                if (idx >= 0 && idx < templateParams.Length)
                    return new[] { templateParams[idx] };
            }
            else if (selector is ZilAdecl)
            {
                var adecl = (ZilAdecl)selector;
                var idx = (adecl.First as ZilFix)?.Value;
                var atom = adecl.Second as ZilAtom;

                if (idx >= 0 && idx < templateParams.Length && atom?.StdAtom == StdAtom.SPLICE)
                {
                    var result = templateParams[(int)idx] as IEnumerable<ZilObject>;
                    if (result != null)
                        return result;
                }
            }

            throw new InterpreterError(InterpreterMessages.Unrecognized_0_1, "template reference", selector);
        }

        static int ParseNumber(string text)
        {
            Contract.Requires(!string.IsNullOrEmpty(text));

            // decimal: -?[0-9]+
            // octal: \*[0-7]+\*
            // binary: #2\s+[01]+

            if (text[0] == '*')
                return Convert.ToInt32(text.Substring(1, text.Length - 2), 8);

            if (text[0] == '#')
            {
                for (int i = 2; i < text.Length; i++)
                {
                    if (!char.IsWhiteSpace(text[i]))
                        return Convert.ToInt32(text.Substring(i), 2);
                }
            }

            return Convert.ToInt32(text);
        }

        /// <summary>
        /// Converts the ZIL object to a string (in reparsable format, if possible).
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        /// <remarks>This method is unaffected by PRINTTYPE, since it has no context.</remarks>
        public abstract override string ToString();

        /// <summary>
        /// Converts the ZIL object to a string, given a context, and optionally
        /// in "friendly" (PRINC) format rather than reparsable format.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="friendly">true if the string should be returned without
        /// quotes, escapes, or qualifiers.</param>
        /// <param name="ignorePrintType">true to ignore any PRINTTYPE set for this type
        /// and use the built-in formatting.</param>
        /// <returns>A string representation of the object.</returns>
        /// <remarks>If a PRINTTYPE is used, <paramref name="friendly"/> has no effect.</remarks>
        public string ToStringContext(Context ctx, bool friendly, bool ignorePrintType = false)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if (!ignorePrintType)
            {
                var del = ctx.GetPrintTypeDelegate(GetTypeAtom(ctx));
                if (del != null)
                    return del(this);
            }

            return ToStringContextImpl(ctx, friendly);
        }

        /// <summary>
        /// Converts the ZIL object to a string, given a context, and optionally
        /// in "friendly" (PRINC) format rather than reparsable format.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="friendly">true if the string should be returned without
        /// quotes, escapes, or qualifiers.</param>
        /// <returns>A string representation of the object.</returns>
        /// <remarks>This method is not affected by PRINTTYPE, which is handled
        /// by <see cref="ToStringContext(Context, bool, bool)"/>.</remarks>
        protected virtual string ToStringContextImpl(Context ctx, bool friendly)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return ToString();
        }

        /// <summary>
        /// Gets an atom representing this object's type.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>The type atom.</returns>
        [Pure]
        public virtual ZilAtom GetTypeAtom(Context ctx)
        {
            var stdAtom = StdTypeAtom;
            Contract.Assert(stdAtom != StdAtom.None);
            return ctx.GetStdAtom(stdAtom);
        }

        /// <summary>
        /// Gets a <see cref="StdAtom"/> representing the object's type, or
        /// <see cref="StdAtom.None"/> if the object belongs to a user-defined type.
        /// </summary>
        [Pure]
        public abstract StdAtom StdTypeAtom { get; }

        /// <summary>
        /// Gets a value indicating the type of this object's primitive form.
        /// </summary>
        [Pure]
        public abstract PrimType PrimType { get; }

        /// <summary>
        /// Gets the primitive form of this object.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>An object of the type indicated by <see cref="PrimType"/>.</returns>
        public abstract ZilObject GetPrimitive(Context ctx);

        /// <summary>
        /// Evaluates an object: performs function calls, duplicates lists, etc.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="environment">The environment in which to evaluate the object,
        /// or <b>null</b> to use the current environment.</param>
        /// <returns>The result of evaluating this object, which may be the same object.</returns>
        public ZilObject Eval(Context ctx, LocalEnvironment environment = null)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            var del = ctx.GetEvalTypeDelegate(GetTypeAtom(ctx));

            if (del != null)
            {
                if (environment != null)
                    return ctx.ExecuteInEnvironment(environment, () => del(this));

                return del(this);
            }

            return EvalImpl(ctx, environment, null);
        }

        /// <summary>
        /// Evaluates an object on behalf of another type.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="originalType">The type of the original object, which must have the
        /// same primtype.</param>
        /// <returns>The result of evaluating the object.</returns>
        /// <remarks>This is invoked when the original type has an EVALTYPE pointing to this
        /// type. The object being evaluated is temporarily CHTYPEd to this type in order to
        /// call this method. <see cref="EvalImpl(Context, LocalEnvironment, ZilAtom)"/> may
        /// use the knowledge of the original type to return a different result; for example,
        /// <see cref="ZilList.EvalImpl(Context, LocalEnvironment, ZilAtom)"/> returns a list
        /// CHTYPEd to the original type.</remarks>
        internal ZilObject EvalAsOtherType(Context ctx, ZilAtom originalType)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(originalType != null);
            Contract.Requires(ctx.IsRegisteredType(originalType));
            Contract.Requires(ctx.GetTypePrim(originalType) == this.PrimType);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            return EvalImpl(ctx, null, originalType);
        }

        /// <summary>
        /// Evaluates an object: performs function calls, duplicates lists, etc.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="environment">The environment in which to evaluate the object,
        /// or <b>null</b> to use the current environment.</param>
        /// <param name="originalType">The type atom of the original object being evaluated, if
        /// EVALTYPE processing has caused it to be changed to the current type, or <b>null</b> in
        /// the usual case.</param>
        /// <returns>The result of evaluating this object, which may be the same object.</returns>
        /// <remarks>
        /// <para>EVALTYPE is handled by <see cref="Eval(Context, LocalEnvironment)"/>.</para>
        /// <para><paramref name="originalType"/> is set to a type atom in cases where one type has
        /// its EVALTYPE set to the name of another type. When evaluating an object of the first type,
        /// it is CHTYPEd to the second type, and the second type's EvalImpl is called with the first
        /// type as a parameter. EvalImpl may use this to produce an object of the appropriate type;
        /// for example, see <see cref="ZilList.EvalImpl"/>.</para>
        /// </remarks>
        protected virtual ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            return this;
        }

        /// <summary>
        /// Expands a macro invocation.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>The result of expanding this object, or the same object if this is
        /// not a macro invocation.</returns>
        public virtual ZilObject Expand(Context ctx)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            return this;
        }

        /// <summary>
        /// Gets a value indicating whether this object is "true", i.e. non-FALSE.
        /// </summary>
        public virtual bool IsTrue
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this object is a local variable reference (.FOO).
        /// </summary>
        /// <returns>True if the object is an LVAL.</returns>
        public virtual bool IsLVAL()
        {
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether this object is a global variable reference (,FOO).
        /// </summary>
        /// <returns>True if the object is a GVAL.</returns>
        public virtual bool IsGVAL()
        {
            return false;
        }

        /// <summary>
        /// Evaluates a series of expressions, returning the value of the last expression.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="prog">The expressions to evaluate.</param>
        /// <returns>The value of the last expression evaluated.</returns>
        public static ZilObject EvalProgram(Context ctx, ZilObject[] prog)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(prog != null);
            Contract.Requires(prog.Length > 0);
            Contract.Requires(Contract.ForAll(prog, p => p != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            ZilObject result = null;

            for (int i = 0; i < prog.Length; i++)
                result = prog[i].Eval(ctx);

            return result;
        }

        public static IEnumerable<ZilObject> ExpandOrEvalWithSplice(Context ctx, ZilObject obj,
            LocalEnvironment environment)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(obj != null);
            Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);

            var expandBefore = obj as IMayExpandBeforeEvaluation;

            if (expandBefore != null && expandBefore.ShouldExpandBeforeEvaluation)
                return expandBefore.ExpandBeforeEvaluation(ctx, environment);

            var result = obj.Eval(ctx, environment);

            var expandAfter = result as IMayExpandAfterEvaluation;

            if (expandAfter != null && expandAfter.ShouldExpandAfterEvaluation)
                return expandAfter.ExpandAfterEvaluation(ctx, environment);

            return Enumerable.Repeat(result, 1);
        }

        /// <summary>
        /// Evaluates a sequence of expressions, expanding segment references (!.X) when encountered.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="sequence">The sequence to evaluate.</param>
        /// <param name="environment">The environment in which to evaluate the expressions,
        /// or <b>null</b> to use the current environment.</param>
        /// <returns>A sequence of evaluation results.</returns>
        /// <remarks>The values obtained by expanding segment references are not evaluated in turn.</remarks>
        public static IEnumerable<ZilObject> EvalSequence(Context ctx, IEnumerable<ZilObject> sequence,
            LocalEnvironment environment = null)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(sequence != null);
            Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);

            return sequence.SelectMany(zo => ExpandOrEvalWithSplice(ctx, zo, environment));
        }

        public static IEnumerable<ZilObject> EvalWithSplice(Context ctx, ZilObject obj)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(obj != null);
            Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);

            if (obj is IMayExpandBeforeEvaluation)
                return Enumerable.Repeat(obj, 1);

            var result = obj.Eval(ctx);

            var expandAfter = result as IMayExpandAfterEvaluation;

            if (expandAfter?.ShouldExpandAfterEvaluation == true)
                return expandAfter.ExpandAfterEvaluation(ctx, ctx.LocalEnvironment);

            return Enumerable.Repeat(result, 1);
        }

        /// <summary>
        /// Evaluates a sequence of expressions without expanding segment references.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="sequence">The sequence to evaluate.</param>
        /// <returns>A sequence of evaluation results.</returns>
        public static IEnumerable<ZilObject> EvalSequenceLeavingSegments(Context ctx, IEnumerable<ZilObject> sequence)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(sequence != null);
            Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);

            return sequence.SelectMany(zo => EvalWithSplice(ctx, zo));
        }

        /// <summary>
        /// Expands segment references (!.X) in a sequence of expressions, leaving the rest unchanged.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="sequence">The sequence of expressions.</param>
        /// <returns>A sequence of resulting expressions.</returns>
        public static IEnumerable<ZilObject> ExpandSegments(Context ctx, IEnumerable<ZilObject> sequence)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(sequence != null);
            Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);

            return sequence.SelectMany(zo =>
            {
                var expandBefore = zo as IMayExpandBeforeEvaluation;

                if (expandBefore?.ShouldExpandBeforeEvaluation == true)
                    return expandBefore.ExpandBeforeEvaluation(ctx, ctx.LocalEnvironment);

                return Enumerable.Repeat(zo, 1);
            });
        }
    }
}