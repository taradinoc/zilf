using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Antlr.Runtime.Tree;
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
        /// <returns>A sequence of zero or more ZIL objects, depending
        /// on whether <paramref name="obj"/> was null, a parsed expression,
        /// or the phantom root of a tree containing multiple
        /// expressions.</returns>
        public static IEnumerable<ZilObject> ReadFromAST(ITree tree, Context ctx)
        {
            Contract.Requires(tree != null);
            Contract.Requires(ctx != null);
            //Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);
            //Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<ZilObject>>(), r => r != null));

            if (tree == null)
                yield break;

            /* obj might be an expression if the source file only contained
             * one expression. otherwise it's a fake node created by antlr to
             * contain multiple expressions. */
            List<ZilObject> result;
            int i;
            if (tree.Type == 0)
            {
                // multiple expressions
                result = new List<ZilObject>(tree.ChildCount);
                for (i = 0; i < tree.ChildCount; i++)
                {
                    ZilObject obj = ReadOneFromAST(tree.GetChild(i), ctx);
                    if (obj != null)
                        yield return obj;
                }
            }
            else
            {
                // just one
                result = new List<ZilObject>(1);
                ZilObject obj = ReadOneFromAST(tree, ctx);
                if (obj != null)
                    yield return obj;
            }
        }

        /// <summary>
        /// Translates the children of a syntax tree node into ZIL objects.
        /// </summary>
        /// <param name="tree">The tree node.</param>
        /// <param name="ctx">The current context.</param>
        /// <returns>The array of translated child objects.</returns>
        private static ZilObject[] ReadChildrenFromAST(ITree tree, Context ctx)
        {
            Contract.Requires(tree != null);
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject[]>() != null);
            Contract.Ensures(Contract.ForAll(Contract.Result<ZilObject[]>(), r => r != null));

            if (tree.ChildCount == 0)
                return new ZilObject[0];

            List<ZilObject> result = new List<ZilObject>(tree.ChildCount);
            for (int i = 0; i < tree.ChildCount; i++)
            {
                ZilObject obj = ReadOneFromAST(tree.GetChild(i), ctx);
                if (obj != null)
                    result.Add(obj);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Translates a single syntax tree node into ZIL objects.
        /// </summary>
        /// <param name="tree">The tree node.</param>
        /// <param name="ctx">The current context.</param>
        /// <returns>The translated object, or null if it was untranslatable.</returns>
        private static ZilObject ReadOneFromAST(ITree tree, Context ctx)
        {
            Contract.Requires(tree != null);
            Contract.Requires(ctx != null);

            ZilObject[] children;

            try
            {
                switch (tree.Type)
                {
                    case ZilLexer.ATOM:
                        return ZilAtom.Parse(tree.Text, ctx);
                    case ZilLexer.CHAR:
                        Contract.Assume(tree.Text.Length >= 3);
                        return new ZilChar(tree.Text[2]);
                    case ZilLexer.COMMENT:
                        // ignore comments
                        return null;
                    case ZilLexer.FORM:
                        children = ReadChildrenFromAST(tree, ctx);
                        if (children.Length == 0)
                            return ctx.FALSE;
                        else
                            return new ZilForm(children) { SourceLine = new FileSourceLine(ctx.CurrentFile, tree.Line) };
                    case ZilLexer.HASH:
                        return ZilHash.Parse(ctx, ReadChildrenFromAST(tree, ctx));
                    case ZilLexer.LIST:
                        return new ZilList(ReadChildrenFromAST(tree, ctx)) { SourceLine = new FileSourceLine(ctx.CurrentFile, tree.Line) };
                    case ZilLexer.VECTOR:
                    case ZilLexer.UVECTOR:  // TODO: a real UVECTOR type?
                        return new ZilVector(ReadChildrenFromAST(tree, ctx)) { SourceLine = new FileSourceLine(ctx.CurrentFile, tree.Line) };
                    case ZilLexer.ADECL:
                        children = ReadChildrenFromAST(tree, ctx);
                        Contract.Assume(children.Length == 2);
                        return new ZilAdecl(children[0], children[1]);
                    case ZilLexer.MACRO:
                    case ZilLexer.VMACRO:
                        // expand macros
                        ZilObject inner = ReadOneFromAST(tree.GetChild(0), ctx);
                        if (inner == null)
                            return null;
                        try
                        {
                            ZilObject result = inner.Eval(ctx);
                            if (tree.Type == ZilLexer.MACRO)
                                return result;
                            else
                                return null;
                        }
                        catch (ZilError ex)
                        {
                            if (ex.SourceLine == null)
                                ex.SourceLine = inner.SourceLine;
                            throw;
                        }
                        catch (ControlException ex)
                        {
                            throw new InterpreterError(inner.SourceLine, "misplaced " + ex.Message);
                        }
                    case ZilLexer.NUM:
                        return new ZilFix(ParseNumber(tree.Text));
                    case ZilLexer.SEGMENT:
                        return new ZilSegment(ReadOneFromAST(tree.GetChild(0), ctx));
                    case ZilLexer.STRING:
                        Contract.Assume(tree.Text.Length >= 2);
                        return ZilString.Parse(tree.Text);
                    default:
                        throw new ArgumentException("Unexpected tree type: " + tree.Type.ToString(), "tree");
                }
            }
            catch (InterpreterError ex)
            {
                if (ex.SourceLine == null)
                    ex.SourceLine = new FileSourceLine(ctx.CurrentFile, tree.Line);

                throw;
            }
        }

        private static int ParseNumber(string text)
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
        public abstract override string ToString();

        /// <summary>
        /// Converts the ZIL object to a string, given a context, and optionally
        /// in "friendly" (PRINC) format rather than reparsable format.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="friendly">true if the string should be returned without
        /// quotes, escapes, or qualifiers.</param>
        /// <returns>A string representation of the object.</returns>
        public virtual string ToStringContext(Context ctx, bool friendly)
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
        public abstract ZilAtom GetTypeAtom(Context ctx);

        /// <summary>
        /// Gets a value indicating the type of this object's primitive form.
        /// </summary>
        public abstract PrimType PrimType { get; }

        /// <summary>
        /// Gets the primitive form of this object.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>An object of the type indicated by <see cref="PrimType."/>.</returns>
        public abstract ZilObject GetPrimitive(Context ctx);

        /// <summary>
        /// Evaluates an object: performs function calls, duplicates lists, etc.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>The result of evaluating this object, which may be the same object.</returns>
        public virtual ZilObject Eval(Context ctx)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

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
            Contract.Requires(Contract.ForAll(prog, p => p != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            try
            {
                for (int i = 0; i < prog.Length; i++)
                    if (i == prog.Length - 1)
                        return prog[i].Eval(ctx);
                    else
                        prog[i].Eval(ctx);
            }
            catch (ReturnException ex)
            {
                return ex.Value;
            }

            // shouldn't get here
            throw new ArgumentException("Missing program", "body");
        }

        /// <summary>
        /// Evaluates a sequence of expressions, expanding segment references (!.X) when encountered.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="sequence">The sequence to evaluate.</param>
        /// <returns>A sequence of evaluation results.</returns>
        public static IEnumerable<ZilObject> EvalSequence(Context ctx, IEnumerable<ZilObject> sequence)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(sequence != null);
            //Contract.Ensures(Contract.Result<IEnumerable<ZilObject>>() != null);

            foreach (ZilObject obj in sequence)
            {
                if (obj is ZilSegment)
                {
                    ZilObject result = ((ZilSegment)obj).Form.Eval(ctx);
                    if (result is IEnumerable<ZilObject>)
                    {
                        foreach (ZilObject inner in (IEnumerable<ZilObject>)result)
                            yield return inner;
                    }
                    else
                        throw new InterpreterError("segment evaluation must return a structure");
                }
                else
                {
                    var result = obj.Eval(ctx);
                    if (result.GetTypeAtom(ctx).StdAtom == StdAtom.SPLICE)
                    {
                        foreach (ZilObject inner in (IEnumerable<ZilObject>)result.GetPrimitive(ctx))
                            yield return inner;
                    }
                    else
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}