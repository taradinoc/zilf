/* Copyright 2010, 2012 Jesse McGrew
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
using System.Linq;
using System.Text;

using Antlr.Runtime.Tree;
using Zilf.Lexing;
using System.Reflection;
using System.IO;
using System.Diagnostics.Contracts;

namespace Zilf
{
    /// <summary>
    /// Indicates the primitive type of a ZilObject.
    /// </summary>
    enum PrimType
    {
        /// <summary>
        /// The primitive type is <see cref="ZilAtom"/>.
        /// </summary>
        ATOM,
        /// <summary>
        /// The primitive type is <see cref="ZilFix"/>.
        /// </summary>
        FIX,
        /// <summary>
        /// The primitive type is <see cref="ZilString"/>.
        /// </summary>
        STRING,
        /// <summary>
        /// The primitive type is <see cref="ZilList"/>.
        /// </summary>
        LIST,
        /// <summary>
        /// The primitive type is <see cref="ZilTable"/>.
        /// </summary>
        TABLE,
        /// <summary>
        /// The primitive type is <see cref="ZilVector"/>.
        /// </summary>
        VECTOR,
    }

    /// <summary>
    /// Specifies that a class implements a ZILF builtin type.
    /// </summary>
    /// <seealso cref="ChtypeMethodAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    class BuiltinTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new BuiltinTypeAttribute with the specified name and primitive type.
        /// </summary>
        /// <param name="name">The <see cref="StdAtom"/> representing the type name.</param>
        /// <param name="primType">The primitive type on which the type is based.</param>
        /// <remarks>A constructor or static method must be marked with
        /// <see cref="ChtypeMethodAttribute"/>.</remarks>
        public BuiltinTypeAttribute(StdAtom name, PrimType primType)
        {
            this.Name = name;
            this.PrimType = primType;
        }

        public StdAtom Name { get; private set; }
        public PrimType PrimType { get; private set; }
    }

    /// <summary>
    /// Specifies that a constructor or static method implements CHTYPE for a builtin type.
    /// </summary>
    /// <remarks>
    /// <para>If applied to a constructor, it must take a single value of the primitive type.</para>
    /// <para>If applied to a static method, it must take two parameters, <see cref="Context"/>
    /// and the primitive type, and return a type assignable to <see cref="ZilObject"/>.</para>
    /// </remarks>
    /// <seealso cref="BuiltinTypeAttribute"/>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    class ChtypeMethodAttribute : Attribute
    {
        public ChtypeMethodAttribute()
        {
        }
    }

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

    [ContractClassFor(typeof(ZilObject))]
    abstract class ZilObjectContracts : ZilObject
    {
        public override ZilAtom GetTypeAtom(Context ctx)
        {
            Contract.Ensures(Contract.Result<ZilAtom>() != null);
            return default(ZilAtom);
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            return default(ZilObject);
        }
    }

    #region Special Types

    class ZilHash : ZilObject
    {
        protected readonly ZilAtom type;
        protected readonly PrimType primtype;
        protected readonly ZilObject primvalue;

        internal ZilHash(ZilAtom type, PrimType primtype, ZilObject primvalue)
        {
            this.type = type;
            this.primtype = primtype;
            this.primvalue = primvalue;
        }

        public override bool Equals(object obj)
        {
            return (obj is ZilHash && ((ZilHash)obj).type == this.type &&
                    ((ZilHash)obj).primvalue.Equals(this.primvalue));
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^ primvalue.GetHashCode();
        }

        public ZilAtom Type
        {
            get { return type; }
        }

        public static ZilObject Parse(Context ctx, ZilObject[] initializer)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(initializer != null);

            if (initializer.Length != 2 || !(initializer[0] is ZilAtom) || initializer[1] == null)
                throw new ArgumentException("Expected 2 objects, the first a ZilAtom");

            ZilAtom type = (ZilAtom)initializer[0];
            ZilObject value = initializer[1];

            return ctx.ChangeType(value, type);
        }

        public override string ToString()
        {
            return "#" + type.ToString() + " " + primvalue.ToString();
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return "#" + type.ToStringContext(ctx, friendly) + " " + primvalue.ToStringContext(ctx, friendly);
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return type;
        }

        public override PrimType PrimType
        {
            get { return primtype; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return primvalue;
        }

        public override ZilObject Eval(Context ctx)
        {
            return this;
        }
    }

    class ZilStructuredHash : ZilHash, IStructure
    {
        public ZilStructuredHash(ZilAtom type, PrimType primtype, ZilObject primvalue)
            : base(type, primtype, primvalue)
        {
        }

        public override bool Equals(object obj)
        {
            return (obj is ZilStructuredHash && ((ZilStructuredHash)obj).type == this.type &&
                    ((ZilStructuredHash)obj).primvalue.Equals(this.primvalue));
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^ primvalue.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return ((IStructure)primvalue).GetFirst();
        }

        public IStructure GetRest(int skip)
        {
            return ((IStructure)primvalue).GetRest(skip);
        }

        public bool IsEmpty()
        {
            return ((IStructure)primvalue).IsEmpty();
        }

        public ZilObject this[int index]
        {
            get
            {
                return ((IStructure)primvalue)[index];
            }
            set
            {
                ((IStructure)primvalue)[index] = value;
            }
        }

        public int GetLength()
        {
            return ((IStructure)primvalue).GetLength();
        }

        public int? GetLength(int limit)
        {
            return ((IStructure)primvalue).GetLength(limit);
        }

        #endregion
    }

    [BuiltinType(StdAtom.SEGMENT, PrimType.LIST)]
    class ZilSegment : ZilObject, IStructure
    {
        private readonly ZilForm form;

        public ZilSegment(ZilObject obj)
        {
            Contract.Requires(obj != null);

            if (obj is ZilForm)
                this.form = (ZilForm)obj;
            else
                throw new ArgumentException("Segment must be based on a FORM");
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(form != null);
        }

        [ChtypeMethod]
        public static ZilSegment FromList(Context ctx, ZilList list)
        {
            ZilForm form = list as ZilForm;
            if (form == null)
            {
                form = new ZilForm(list);
                form.SourceLine = SourceLines.Chtyped;
            }

            return new ZilSegment(form);
        }

        public ZilForm Form
        {
            get { return form; }
        }

        public override string ToString()
        {
            return "!" + form.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.SEGMENT);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(form);
        }

        public override ZilObject Eval(Context ctx)
        {
            throw new InterpreterError("a SEGMENT can only be evaluated inside a structure");
        }

        public override bool Equals(object obj)
        {
            ZilSegment other = obj as ZilSegment;
            return other != null && other.form.Equals(this.form);
        }

        public override int GetHashCode()
        {
            return form.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return ((IStructure)form).GetFirst();
        }

        public IStructure GetRest(int skip)
        {
            return ((IStructure)form).GetRest(skip);
        }

        public bool IsEmpty()
        {
            return ((IStructure)form).IsEmpty();
        }

        public ZilObject this[int index]
        {
            get
            {
                return ((IStructure)form)[index];
            }
            set
            {
                ((IStructure)form)[index] = value;
            }
        }

        public int GetLength()
        {
            return ((IStructure)form).GetLength();
        }

        public int? GetLength(int limit)
        {
            return ((IStructure)form).GetLength(limit);
        }

        #endregion
    }

    [BuiltinType(StdAtom.CHANNEL, Zilf.PrimType.VECTOR)]
    class ZilChannel : ZilObject
    {
        private readonly FileAccess fileAccess;
        private readonly string path;
        private Stream stream;

        public ZilChannel(string path, FileAccess fileAccess)
        {
            this.path = path;
            this.fileAccess = fileAccess;
        }

        [ChtypeMethod]
        public static ZilChannel FromVector(Context ctx, ZilVector vector)
        {
            throw new InterpreterError("CHTYPE to CHANNEL not supported");
        }

        public override string ToString()
        {
            return string.Format(
                "#CHANNEL [{0} {1}]",
                fileAccess == FileAccess.Read ? "READ" : "NONE",
                ZilString.Quote(path));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.CHANNEL);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.VECTOR; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilVector(new ZilObject[] {
                ctx.GetStdAtom(fileAccess == FileAccess.Read ? StdAtom.READ : StdAtom.NONE),
                new ZilString(path)
            });
        }

        public void Open(Context ctx)
        {
            if (stream == null)
                stream = ctx.OpenChannelStream(path, fileAccess);
        }

        public void Close(Context ctx)
        {
            if (stream != null)
            {
                try
                {
                    stream.Close();
                }
                finally
                {
                    stream = null;
                }
            }
        }

        public long? GetFileLength()
        {
            if (stream == null)
            {
                return null;
            }

            try
            {
                return stream.Length;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        public char? ReadChar()
        {
            if (stream == null)
                return null;

            var result = stream.ReadByte();

            return result == -1 ? (char?)null : (char)result;
        }
    }

    #endregion

    #region Monad Types

    [BuiltinType(StdAtom.ATOM, PrimType.ATOM)]
    class ZilAtom : ZilObject
    {
        private readonly string text;
        private readonly ObList list;
        private readonly StdAtom stdAtom;

        public ZilAtom(string text, ObList list, StdAtom stdAtom)
        {
            this.text = text;
            this.list = list;
            this.stdAtom = stdAtom;
        }

        [ChtypeMethod]
        public static ZilAtom FromAtom(Context ctx, ZilAtom other)
        {
            // we can't construct a new atom since it wouldn't be equal to the old one
            return other;
        }

        public string Text
        {
            get { return text; }
        }

        public ObList ObList
        {
            get { return list; }
        }

        public StdAtom StdAtom
        {
            get { return stdAtom; }
        }

        private static string Unquote(string text)
        {
            StringBuilder sb = new StringBuilder(text);

            for (int i = 0; i < sb.Length; i++)
                if (sb[i] == '\\')
                    sb.Remove(i, 1);

            return sb.ToString();
        }

        /// <summary>
        /// Parses an atom name, including !- separators, and returns the atom
        /// object. Creates the atom or oblist(s) if necessary.
        /// </summary>
        /// <param name="text">The atom name.</param>
        /// <param name="ctx">The current context.</param>
        /// <returns>The parsed atom.</returns>
        public static ZilAtom Parse(string text, Context ctx)
        {
            Contract.Requires(text != null);
            Contract.Requires(ctx != null);

            ObList list;
            ZilAtom result;
            int idx = text.IndexOf("!-");

            text = Unquote(text);

            if (idx == -1)
            {
                // look for it in <1 .OBLIST>, <2 .OBLIST>...
                ZilObject pathspec = ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST));
                if (pathspec is IEnumerable<ZilObject>)
                {
                    ObList insertList = null;
                    bool gotDefault = false;

                    foreach (ZilObject obj in (IEnumerable<ZilObject>)pathspec)
                    {
                        list = obj as ObList;
                        if (list != null)
                        {
                            if (list.Contains(text))
                                return list[text];

                            if (insertList == null || gotDefault)
                            {
                                insertList = list;
                                gotDefault = false;
                            }
                        }
                        else if (obj is ZilAtom && ((ZilAtom)obj).StdAtom == StdAtom.DEFAULT)
                            gotDefault = true;
                    }

                    // not found, insert
                    result = new ZilAtom(text, insertList, StdAtom.None);
                    insertList[text] = result;
                    return result;
                }
                else
                    throw new InterpreterError("no OBLIST path");
            }

            // look for it in the specified oblist
            if (idx == text.Length - 2)
            {
                list = ctx.RootObList;
            }
            else
            {
                ZilAtom olname = Parse(text.Substring(idx + 2), ctx);
                list = ctx.GetProp(olname, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList;
                if (list == null)
                {
                    // create new oblist
                    list = new ObList(ctx.IgnoreCase);
                    ctx.PutProp(olname, ctx.GetStdAtom(StdAtom.OBLIST), list);
                }
            }

            string pname = text.Substring(0, idx);

            if (list.Contains(pname))
                return list[pname];

            result = new ZilAtom(pname, list, StdAtom.None);
            list[pname] = result;
            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                if (c == '\\' || char.IsWhiteSpace(c))
                    sb.Append('\\');

                sb.Append(c);
            }

            return sb.ToString();
        }

        //XXX ToStringContext

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ATOM);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.ATOM; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }
    }

    [BuiltinType(StdAtom.CHARACTER, PrimType.FIX)]
    class ZilChar : ZilObject
    {
        private readonly int value;

        public ZilChar(char ch)
            : this((int)ch)
        {
        }

        private ZilChar(int value)
        {
            this.value = value;
        }

        [ChtypeMethod]
        public static ZilChar FromFix(Context ctx, ZilFix fix)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(fix != null);

            return new ZilChar(fix.Value);
        }

        public char Char
        {
            get { return (char)value; }
        }

        public override string ToString()
        {
            return "!\\" + Char;
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            if (friendly)
                return Char.ToString();
            else
                return ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.CHARACTER);
        }

        public override PrimType PrimType
        {
            get { return PrimType.FIX; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilFix(value);
        }

        public override bool Equals(object obj)
        {
            ZilChar other = obj as ZilChar;
            return other != null && other.value == this.value;
        }

        public override int GetHashCode()
        {
            return value;
        }
    }

    [BuiltinType(StdAtom.FALSE, PrimType.LIST)]
    class ZilFalse : ZilObject, IStructure
    {
        private readonly ZilList value;

        [ChtypeMethod]
        public ZilFalse(ZilList value)
        {
            Contract.Requires(value != null);
            this.value = value;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(value != null);
        }

        public override string ToString()
        {
            return "#FALSE " + value.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FALSE);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return value;
        }

        public override bool IsTrue
        {
            get { return false; }
        }

        public override bool Equals(object obj)
        {
            ZilFalse other = obj as ZilFalse;
            return other != null && other.value.Equals(this.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return value.First;
        }

        public IStructure GetRest(int skip)
        {
            return ((IStructure)value).GetRest(skip);
        }

        public bool IsEmpty()
        {
            return value.IsEmpty;
        }

        public ZilObject this[int index]
        {
            get
            {
                return ((IStructure)value)[index];
            }
            set
            {
                ((IStructure)value)[index] = value;
            }
        }

        public int GetLength()
        {
            return ((IStructure)value).GetLength();
        }

        public int? GetLength(int limit)
        {
            return ((IStructure)value).GetLength(limit);
        }

        #endregion
    }

    [BuiltinType(StdAtom.FIX, PrimType.FIX)]
    class ZilFix : ZilObject, IApplicable
    {
        private readonly int value;

        public ZilFix(int value)
        {
            this.value = value;
        }

        [ChtypeMethod]
        public ZilFix(ZilFix other)
            : this(other.value)
        {
            Contract.Requires(other != null);
        }

        public int Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FIX);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.FIX; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            ZilFix other = obj as ZilFix;
            return other != null && other.value == this.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IApplicable Members

        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return ApplyNoEval(ctx, EvalSequence(ctx, args).ToArray());
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            if (args.Length == 1)
                return Subrs.NTH(ctx, new ZilObject[] { args[0], this });
            else if (args.Length == 2)
                return Subrs.PUT(ctx, new ZilObject[] { args[0], this, args[1] });
            else
                throw new InterpreterError("expected 1 or 2 args after a FIX");
        }

        #endregion
    }

    #endregion

    #region Structured Types

    /// <summary>
    /// Provides methods common to structured types (LIST, STRING, possibly others).
    /// </summary>
    [ContractClass(typeof(IStructureContracts))]
    interface IStructure
    {
        /// <summary>
        /// Gets the first element of the structure.
        /// </summary>
        /// <returns>The first element.</returns>
        ZilObject GetFirst();
        /// <summary>
        /// Gets the remainder of the structure, after skipping the first few elements.
        /// </summary>
        /// <param name="skip">The number of elements to skip.</param>
        /// <returns>A structure containing the unskipped elements, or null if no elements are left.</returns>
        IStructure GetRest(int skip);

        /// <summary>
        /// Determines whether the structure is empty.
        /// </summary>
        /// <returns>true if the structure has no elements; false if it has any elements.</returns>
        [Pure]
        bool IsEmpty();

        /// <summary>
        /// Gets or sets an element by its numeric index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to access.</param>
        /// <returns>The element value, or null if the specified element is past the end of
        /// the structure.</returns>
        /// <exception cref="InterpreterError">
        /// An attempt was made to set an element past the end of the structure.
        /// </exception>
        ZilObject this[int index] { get; set; }

        /// <summary>
        /// Measures the length of the structure.
        /// </summary>
        /// <returns>The number of elements in the structure.</returns>
        /// <remarks>This method may loop indefinitely if the structure contains
        /// a reference to itself.</remarks>
        int GetLength();
        /// <summary>
        /// Measures the length of the structure, up to a specified maximum.
        /// </summary>
        /// <param name="limit">The maximum length to allow.</param>
        /// <returns>The number of elements in the structure, or null if the structure
        /// contains more than <paramref name="limit"/> elements.</returns>
        [Pure]
        int? GetLength(int limit);
    }

    [ContractClassFor(typeof(IStructure))]
    abstract class IStructureContracts : IStructure
    {
        public ZilObject GetFirst()
        {
            throw new NotImplementedException();
        }

        public IStructure GetRest(int skip)
        {
            Contract.Requires(skip >= 0);
            return default(IStructure);
        }
        
        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public ZilObject this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int GetLength()
        {
            Contract.Ensures(Contract.Result<int>() > 0 || IsEmpty());
            return default(int);
        }

        public int? GetLength(int limit)
        {
            Contract.Ensures(Contract.Result<int?>() == null || Contract.Result<int?>().Value <= limit);
            return default(int?);
        }
    }

    // TODO: ZilList should be sealed; the other list-based types should derive from an abstract base
    [BuiltinType(StdAtom.LIST, PrimType.LIST)]
    class ZilList : ZilObject, IEnumerable<ZilObject>, IStructure
    {
        public ZilObject First;
        public ZilList Rest;

        public ZilList(IEnumerable<ZilObject> sequence)
        {
            Contract.Requires(sequence != null);

            using (IEnumerator<ZilObject> tor = sequence.GetEnumerator())
            {
                if (tor.MoveNext())
                {
                    this.First = tor.Current;
                    this.Rest = MakeRest(tor);
                }
                else
                {
                    this.First = null;
                    this.Rest = null;
                }
            }
        }

        public ZilList(ZilObject current, ZilList rest)
        {
            Contract.Requires((current == null && rest == null) || (current != null && rest != null));
            Contract.Ensures(First == current);
            Contract.Ensures(Rest == rest);

            this.First = current;
            this.Rest = rest;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant((First == null && Rest == null) || (First != null && Rest != null));
        }

        [ChtypeMethod]
        public static ZilList FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);

            return new ZilList(list.First, list.Rest);
        }

        private ZilList MakeRest(IEnumerator<ZilObject> tor)
        {
            Contract.Requires(tor != null);
            Contract.Ensures(Contract.Result<ZilList>() != null);

            if (tor.MoveNext())
            {
                ZilObject cur = tor.Current;
                ZilList rest = MakeRest(tor);
                return new ZilList(cur, rest);
            }
            else
                return new ZilList(null, null);
        }

        public bool IsEmpty
        {
            get
            {
                Contract.Ensures(
                    (Contract.Result<bool>() == false && First != null && Rest != null) ||
                    (Contract.Result<bool>() == true && First == null && Rest == null));
                return First == null && Rest == null;
            }
        }

        public static string SequenceToString(IEnumerable<ZilObject> items,
            string start, string end, Func<ZilObject, string> convert)
        {
            Contract.Requires(items != null);
            Contract.Requires(start != null);
            Contract.Requires(end != null);
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);

            StringBuilder sb = new StringBuilder(2);
            sb.Append(start);

            foreach (ZilObject obj in items)
            {
                if (sb.Length > 1)
                    sb.Append(' ');

                sb.Append(convert(obj));
            }

            sb.Append(end);
            return sb.ToString();
        }

        public override string ToString()
        {
            return SequenceToString(this, "(", ")", zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return SequenceToString(this, "(", ")", zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.LIST);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            if (this.GetType() == typeof(ZilList))
                return this;
            else
                return new ZilList(First, Rest);
        }

        public override ZilObject Eval(Context ctx)
        {
            return new ZilList(EvalSequence(ctx, this)) { SourceLine = this.SourceLine };
        }

        public IEnumerator<ZilObject> GetEnumerator()
        {
            ZilList r = this;
            while (r.First != null)
            {
                yield return r.First;
                r = r.Rest;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            ZilList other = obj as ZilList;
            if (other == null)
                return false;

            if (this.First == null)
                return other.First == null;
            if (!this.First.Equals(other.First))
                return false;

            if (this.Rest == null)
                return other.Rest == null;

            return this.Rest.Equals(other.Rest);
        }

        public override int GetHashCode()
        {
            int result = (int)StdAtom.LIST;
            foreach (ZilObject obj in this)
                result = result * 31 + obj.GetHashCode();
            return result;
        }

        ZilObject IStructure.GetFirst()
        {
            return First;
        }

        IStructure IStructure.GetRest(int skip)
        {
            ZilList result = this;
            while (skip-- > 0 && result != null)
                result = (ZilList)result.Rest;
            return result;
        }

        bool IStructure.IsEmpty()
        {
            return First == null;
        }

        ZilObject IStructure.this[int index]
        {
            get
            {
                IStructure rested = ((IStructure)this).GetRest(index);
                if (rested == null)
                    return null;
                else
                    return rested.GetFirst();
            }
            set
            {
                IStructure rested = ((IStructure)this).GetRest(index);
                if (rested == null)
                    throw new ArgumentOutOfRangeException("index", "writing past end of list");
                ((ZilList)rested).First = value;
            }
        }

        int IStructure.GetLength()
        {
            return this.Count();
        }

        int? IStructure.GetLength(int limit)
        {
            int count = 0;

            foreach (ZilObject obj in this)
            {
                count++;
                if (count > limit)
                    return null;
            }

            return count;
        }
    }

    [BuiltinType(StdAtom.FORM, PrimType.LIST)]
    class ZilForm : ZilList
    {
        public ZilForm(IEnumerable<ZilObject> sequence)
            : base(sequence)
        {
            Contract.Requires(sequence != null);
        }

        protected ZilForm(ZilObject first, ZilList rest)
            : base(first, rest)
        {
            Contract.Requires((first == null && rest == null) || (first != null && rest != null));
            Contract.Ensures(First == first);
            Contract.Ensures(Rest == rest);
        }

        public override ISourceLine SourceLine
        {
            get
            {
                Contract.Ensures(Contract.Result<ISourceLine>() != null);

                return base.SourceLine ?? SourceLines.Unknown;
            }
            set
            {
                base.SourceLine = value;
            }
        }

        [ChtypeMethod]
        public static new ZilForm FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);

            return new ZilForm(list.First, list.Rest);
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            // check for special forms
            if (First is ZilAtom && Rest.Rest != null && Rest.Rest.First == null)
            {
                ZilObject arg = ((ZilList)Rest).First;

                switch (((ZilAtom)First).StdAtom)
                {
                    case StdAtom.GVAL:
                        return "," + arg.ToString();
                    case StdAtom.LVAL:
                        return "." + arg.ToString();
                    case StdAtom.QUOTE:
                        return "'" + arg.ToString();
                }
            }

            // otherwise display like a list with angle brackets
            return ZilList.SequenceToString(this, "<", ">", convert);
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FORM);
        }

        private static ZilObject[] EmptyObjArray = new ZilObject[0];

        public override ZilObject Eval(Context ctx)
        {
            if (First == null)
                throw new NotImplementedException("Can't evaluate null");

            ZilObject target;
            if (First is ZilAtom)
            {
                ZilAtom fa = (ZilAtom)First;
                target = ctx.GetGlobalVal(fa);
                if (target == null)
                    target = ctx.GetLocalVal(fa);
                if (target == null)
                    throw new InterpreterError(this, "calling undefined atom: " + fa.ToStringContext(ctx, false));
            }
            else
                target = First.Eval(ctx);

            if (target is IApplicable)
            {
                ZilForm oldCF = ctx.CallingForm;
                ctx.CallingForm = this;
                try
                {
                    return ((IApplicable)target).Apply(ctx, ((ZilList)Rest).ToArray());
                }
                catch (ZilError ex)
                {
                    if (ex.SourceLine == null)
                        ex.SourceLine = this.SourceLine;
                    throw;
                }
                finally
                {
                    ctx.CallingForm = oldCF;
                }
            }
            else
                throw new InterpreterError(this, "not an applicable type: " +
                    target.GetTypeAtom(ctx).ToStringContext(ctx, false));
        }

        public override ZilObject Expand(Context ctx)
        {
            if (First is ZilAtom)
            {
                ZilAtom fa = (ZilAtom)First;
                ZilObject target = ctx.GetGlobalVal(fa);
                if (target == null)
                    target = ctx.GetLocalVal(fa);
                if (target != null && target.GetTypeAtom(ctx).StdAtom == StdAtom.MACRO)
                {
                    ZilForm oldCF = ctx.CallingForm;
                    ctx.CallingForm = this;
                    try
                    {
                        ZilObject result = ((ZilEvalMacro)target).Expand(ctx,
                            Rest == null ? EmptyObjArray : ((ZilList)Rest).ToArray());

                        ZilForm resultForm = result as ZilForm;
                        if (resultForm == null || resultForm == this)
                            return result;

                        // set the source info on the expansion to match the macro invocation
                        resultForm = DeepRewriteSourceInfo(resultForm, this.SourceLine);
                        return resultForm.Expand(ctx);
                    }
                    catch (ZilError ex)
                    {
                        if (ex.SourceLine == null)
                            ex.SourceLine = this.SourceLine;
                        throw;
                    }
                    finally
                    {
                        ctx.CallingForm = oldCF;
                    }
                }
            }
            else if (First is ZilFix)
            {
                if (Rest.First != null)
                {
                    if (Rest.Rest.First == null)
                    {
                        // <1 FOO> => <GET FOO 1>
                        Rest = new ZilList(Rest.First,
                               new ZilList(First,
                               new ZilList(null, null)));
                        First = ctx.GetStdAtom(StdAtom.GET);
                    }
                    else
                    {
                        // <1 FOO BAR> => <PUT FOO 1 BAR>
                        Rest = new ZilList(Rest.First,
                               new ZilList(First,
                               Rest.Rest));
                        First = ctx.GetStdAtom(StdAtom.PUT);
                    }
                }
            }

            return this;
        }

        private static ZilForm DeepRewriteSourceInfo(ZilForm other, ISourceLine src)
        {
            return new ZilForm(DeepRewriteSourceInfoContents(other, src)) { SourceLine = src };
        }

        private static IEnumerable<ZilObject> DeepRewriteSourceInfoContents(
            IEnumerable<ZilObject> contents, ISourceLine src)
        {
            foreach (var item in contents)
            {
                if (item is ZilForm)
                {
                    yield return DeepRewriteSourceInfo((ZilForm)item, src);
                }
                else
                {
                    yield return item;
                }
            }
        }

        public override bool IsLVAL()
        {
            var atom = First as ZilAtom;
            return (atom != null && atom.StdAtom == StdAtom.LVAL && Rest.Rest != null && Rest.Rest.First == null);
        }

        public override bool IsGVAL()
        {
            var atom = First as ZilAtom;
            return (atom != null && atom.StdAtom == StdAtom.GVAL && Rest.Rest != null && Rest.Rest.First == null);
        }
    }

    [BuiltinType(StdAtom.STRING, PrimType.STRING)]
    class ZilString : ZilObject, IStructure
    {
        public string Text;

        public ZilString(string text)
        {
            this.Text = text;
        }

        [ChtypeMethod]
        public ZilString(ZilString other)
            : this(other.Text)
        {
            Contract.Requires(other != null);
        }

        public static ZilString Parse(string str)
        {
            Contract.Requires(str != null);
            Contract.Requires(str.Length >= 2);

            StringBuilder sb = new StringBuilder(str.Length - 2);

            for (int i = 1; i < str.Length - 1; i++)
            {
                char ch = str[i];
                switch (ch)
                {
                    case '\\':
                        sb.Append(str[++i]);
                        break;

                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return new ZilString(sb.ToString());
        }

        public override string ToString()
        {
            return Quote(Text);
        }

        public static string Quote(string text)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<string>() != null);

            StringBuilder sb = new StringBuilder(text.Length + 2);
            sb.Append('"');

            foreach (char c in text)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            if (friendly)
                return Text;
            else
                return ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is ZilString)
                return ((ZilString)obj).Text.Equals(this.Text);

            if (obj is OffsetString)
                return obj.Equals(this);

            return false;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.STRING);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.STRING; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        ZilObject IStructure.GetFirst()
        {
            if (Text.Length == 0)
                return null;
            else
                return new ZilChar(Text[0]);
        }

        IStructure IStructure.GetRest(int skip)
        {
            if (Text.Length < skip)
                return null;
            else
                return new OffsetString(this, skip);
        }

        bool IStructure.IsEmpty()
        {
            return Text.Length == 0;
        }

        ZilObject IStructure.this[int index]
        {
            get
            {
                if (index >= 0 && index < Text.Length)
                    return new ZilChar(Text[index]);
                else
                    return null;
            }
            set
            {
                ZilChar ch = value as ZilChar;
                if (ch == null)
                    throw new InterpreterError("elements of a string must be characters");
                if (index >= 0 && index < Text.Length)
                    Text = Text.Substring(0, index) + ch.Char +
                        Text.Substring(index + 1, Text.Length - index - 1);
                else
                    throw new InterpreterError("writing past end of string");
            }
        }

        int IStructure.GetLength()
        {
            return Text.Length;
        }

        int? IStructure.GetLength(int limit)
        {
            int length = Text.Length;
            if (length > limit)
                return null;
            else
                return length;
        }

        private class OffsetString : ZilObject, IStructure
        {
            private readonly ZilString orig;
            private readonly int offset;

            public OffsetString(ZilString orig, int offset)
            {
                this.orig = orig;
                this.offset = offset;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder(orig.Text.Length - offset + 2);
                sb.Append('"');

                for (int i = offset; i < orig.Text.Length; i++)
                {
                    char c = orig.Text[i];
                    switch (c)
                    {
                        case '"':
                            sb.Append("\\\"");
                            break;
                        case '\\':
                            sb.Append("\\\\");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }

                sb.Append('"');
                return sb.ToString();
            }

            public override string ToStringContext(Context ctx, bool friendly)
            {
                if (friendly)
                    return orig.Text.Substring(offset);
                else
                    return ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj is OffsetString)
                {
                    OffsetString other = (OffsetString)obj;
                    if (other.orig == this.orig && other.offset == this.offset)
                        return true;

                    return other.orig.Text.Substring(other.offset).Equals(
                        this.orig.Text.Substring(this.offset));
                }

                if (obj is ZilString)
                    return orig.Text.Substring(offset).Equals(((ZilString)obj).Text);

                return false;
            }

            public override int GetHashCode()
            {
                return orig.Text.Substring(offset).GetHashCode();
            }

            public override ZilAtom GetTypeAtom(Context ctx)
            {
                return ctx.GetStdAtom(StdAtom.STRING);
            }

            public override PrimType PrimType
            {
                get { return Zilf.PrimType.STRING; }
            }

            public override ZilObject GetPrimitive(Context ctx)
            {
                return new ZilString(orig.Text.Substring(offset));
            }

            ZilObject IStructure.GetFirst()
            {
                if (offset >= orig.Text.Length)
                    return null;
                else
                    return new ZilChar(orig.Text[offset]);
            }

            IStructure IStructure.GetRest(int skip)
            {
                if (offset > orig.Text.Length - skip)
                    return null;
                else
                    return new OffsetString(orig, offset + skip);
            }

            bool IStructure.IsEmpty()
            {
                return offset >= orig.Text.Length;
            }

            ZilObject IStructure.this[int index]
            {
                get
                {
                    index += offset;
                    if (index >= 0 && index < orig.Text.Length)
                        return new ZilChar(orig.Text[index]);
                    else
                        return null;
                }
                set
                {
                    ZilChar ch = value as ZilChar;
                    if (ch == null)
                        throw new InterpreterError("elements of a string must be characters");
                    index += offset;
                    if (index >= 0 && index < orig.Text.Length)
                        orig.Text = orig.Text.Substring(0, index) + ch.Char +
                            orig.Text.Substring(index + 1, orig.Text.Length - index - 1);
                    else
                        throw new InterpreterError("writing past end of string");
                }
            }

            int IStructure.GetLength()
            {
                return Math.Max(orig.Text.Length - offset, 0);
            }

            int? IStructure.GetLength(int limit)
            {
                int length = Math.Max(orig.Text.Length - offset, 0);
                if (length > limit)
                    return null;
                else
                    return length;
            }
        }
    }

    [BuiltinType(StdAtom.VECTOR, PrimType.VECTOR)]
    sealed class ZilVector : ZilObject, IEnumerable<ZilObject>, IStructure
    {
        #region Storage

        private class VectorStorage
        {
            private int baseOffset = 0;
            private readonly ZilObject[] items;

            public VectorStorage()
                : this(new ZilObject[0])
            {
            }

            public VectorStorage(ZilObject[] items)
            {
                Contract.Requires(items != null);

                this.items = items;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(items != null);
            }

            public IEnumerable<ZilObject> GetSequence(int offset)
            {
                return items.Skip(offset - baseOffset);
            }

            public int GetLength(int offset)
            {
                return items.Length - offset - baseOffset;
            }

            public ZilObject GetItem(int offset, int index)
            {
                return items[index + offset - baseOffset];
            }

            public void PutItem(int offset, int index, ZilObject value)
            {
                items[index + offset - baseOffset] = value;
            }
        }

        #endregion

        private readonly VectorStorage storage;
        private readonly int offset;

        public ZilVector()
        {
            storage = new VectorStorage();
            offset = 0;
        }

        [ChtypeMethod]
        public ZilVector(ZilVector other)
            : this(other.storage, other.offset)
        {
            Contract.Requires(other != null);
        }

        private ZilVector(VectorStorage storage, int offset)
        {
            Contract.Requires(storage != null);

            this.storage = storage;
            this.offset = offset;
        }

        public ZilVector(params ZilObject[] items)
        {
            Contract.Requires(items != null);

            storage = new VectorStorage(items);
            offset = 0;
        }

        public override bool Equals(object obj)
        {
            ZilVector other = obj as ZilVector;
            if (other == null)
                return false;

            return this.SequenceEqual(other);
        }

        public override int GetHashCode()
        {
            int result = (int)StdAtom.VECTOR;
            foreach (ZilObject obj in this)
                result = result * 31 + obj.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return ZilList.SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ZilList.SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.VECTOR);
        }

        public override PrimType PrimType
        {
            get { return PrimType.VECTOR; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        public override ZilObject Eval(Context ctx)
        {
            return new ZilVector(EvalSequence(ctx, this).ToArray()) { SourceLine = this.SourceLine };
        }

        #region IEnumerable<ZilObject> Members

        public IEnumerator<ZilObject> GetEnumerator()
        {
            return storage.GetSequence(offset).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return storage.GetItem(offset, 0);
        }

        public IStructure GetRest(int skip)
        {
            return new ZilVector(this.storage, this.offset + skip);
        }

        public bool IsEmpty()
        {
            return storage.GetLength(offset) <= 0;
        }

        public ZilObject this[int index]
        {
            get { return storage.GetItem(offset, index); }
            set { storage.PutItem(offset, index, value); }
        }

        public int GetLength()
        {
            return storage.GetLength(offset);
        }

        public int? GetLength(int limit)
        {
            var length = storage.GetLength(offset);
            if (length <= limit)
                return length;
            else
                return null;
        }

        #endregion
    }

    [BuiltinType(StdAtom.ADECL, PrimType.VECTOR)]
    sealed class ZilAdecl : ZilObject, IStructure
    {
        public ZilObject First;
        public ZilObject Second;

        [ChtypeMethod]
        public ZilAdecl(ZilVector vector)
        {
            Contract.Requires(vector != null);

            if (vector.GetLength() != 2)
                throw new InterpreterError("vector coerced to ADECL must have length 2");

            First = vector[0];
            Second = vector[1];
        }

        public ZilAdecl(ZilObject first, ZilObject second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");

            this.First = first;
            this.Second = second;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ZilAdecl;
            if (other == null)
                return false;

            return other.First.Equals(First) && other.Second.Equals(Second);
        }

        public override int GetHashCode()
        {
            var result = (int)StdAtom.ADECL;
            result = result * 31 + First.GetHashCode();
            result = result * 31 + Second.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return First.ToString() + ":" + Second.ToString();
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return First.ToStringContext(ctx, friendly) + ":" + Second.ToStringContext(ctx, friendly);
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ADECL);
        }

        public override PrimType PrimType
        {
            get { return PrimType.VECTOR; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilVector(First, Second);
        }

        public override ZilObject Eval(Context ctx)
        {
            // TODO: check decl (Second) after evaluating First
            return First.Eval(ctx);
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            throw new NotImplementedException();
        }

        public IStructure GetRest(int skip)
        {
            throw new NotImplementedException();
        }

        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public ZilObject this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int GetLength()
        {
            throw new NotImplementedException();
        }

        public int? GetLength(int limit)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

    #region Applicable Types

    /// <summary>
    /// Provides a method to apply a <see cref="ZilObject"/>, such as a SUBR or FUNCTION,
    /// to a set of arguments.
    /// </summary>
    [ContractClass(typeof(IApplicableContracts))]
    interface IApplicable
    {
        /// <summary>
        /// Applies the object to the given arguments.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="args">The unevaluated arguments.</param>
        /// <returns>The result of the application.</returns>
        /// <remarks>
        /// For FSUBRs, this is the same as <see cref="ApplyNoEval"/>.
        /// </remarks>
        ZilObject Apply(Context ctx, ZilObject[] args);

        /// <summary>
        /// Applies the object to the given arguments, which have already been evaluated.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The result of the application.</returns>
        ZilObject ApplyNoEval(Context ctx, ZilObject[] args);
    }

    [ContractClassFor(typeof(IApplicable))]
    abstract class IApplicableContracts : IApplicable
    {
        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            return default(ZilObject);
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            return default(ZilObject);
        }
    }

    [BuiltinType(StdAtom.SUBR, PrimType.STRING)]
    class ZilSubr : ZilObject, IApplicable
    {
        protected readonly Subrs.SubrDelegate handler;

        public ZilSubr(Subrs.SubrDelegate handler)
        {
            this.handler = handler;
        }

        [ChtypeMethod]
        public static ZilSubr FromString(Context ctx, ZilString str)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(str != null);

            var name = str.ToStringContext(ctx, true);
            MethodInfo mi = typeof(Subrs).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
            if (mi != null)
            {
                object[] attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                if (attrs.Length == 1)
                {
                    Subrs.SubrDelegate del = (Subrs.SubrDelegate)Delegate.CreateDelegate(
                        typeof(Subrs.SubrDelegate), mi);

                    return new ZilSubr(del);
                }
            }
            throw new InterpreterError("unrecognized SUBR name: " + name);
        }

        public override string ToString()
        {
            return "#SUBR \"" + handler.Method.Name + "\"";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.SUBR);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.STRING; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilString(handler.Method.Name);
        }

        public virtual ZilObject Apply(Context ctx, ZilObject[] args)
        {
            var result = handler(ctx, EvalSequence(ctx, args).ToArray());
            Contract.Assume(result != null);
            return result;
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            var result = handler(ctx, args);
            Contract.Assume(result != null);
            return result;
        }

        public override bool Equals(object obj)
        {
            ZilSubr other = obj as ZilSubr;
            return other != null && other.GetType() == this.GetType() && other.handler.Equals(this.handler);
        }

        public override int GetHashCode()
        {
            return handler.GetHashCode();
        }
    }

    [BuiltinType(StdAtom.FSUBR, PrimType.STRING)]
    class ZilFSubr : ZilSubr, IApplicable
    {
        public ZilFSubr(Subrs.SubrDelegate handler)
            : base(handler)
        {
        }

        [ChtypeMethod]
        public static new ZilFSubr FromString(Context ctx, ZilString str)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(str != null);
            Contract.Ensures(Contract.Result<ZilFSubr>() != null);

            var name = str.ToStringContext(ctx, true);
            MethodInfo mi = typeof(Subrs).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
            if (mi != null)
            {
                object[] attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                if (attrs.Length == 1)
                {
                    Subrs.SubrDelegate del = (Subrs.SubrDelegate)Delegate.CreateDelegate(
                        typeof(Subrs.SubrDelegate), mi);

                    return new ZilFSubr(del);
                }
            }
            throw new InterpreterError("unrecognized FSUBR name: " + name);
        }

        public override string ToString()
        {
            return "#FSUBR \"" + handler.Method.Name + "\"";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FSUBR);
        }

        public override ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return ApplyNoEval(ctx, args);
        }
    }

    struct ArgItem
    {
        public enum ArgType { Required, Optional, Auxiliary }

        public readonly ZilAtom Atom;
        public readonly bool Quoted;
        public readonly ZilObject DefaultValue;
        public readonly ArgType Type;

        public ArgItem(ZilAtom atom, bool quoted, ZilObject defaultValue, ArgType type)
        {
            this.Atom = atom;
            this.Quoted = quoted;
            this.DefaultValue = defaultValue;
            this.Type = type;
        }
    }

    class ArgSpec : IEnumerable<ArgItem>
    {
        private readonly ZilAtom name;
        private readonly ZilAtom[] argAtoms;
        private readonly ZilObject[] argDecls;
        private readonly bool[] argQuoted;
        private readonly ZilObject[] argDefaults;
        private readonly int optArgsStart, auxArgsStart;
        private readonly ZilAtom varargsAtom;
        private readonly bool varargsQuoted;
        private readonly ZilAtom quoteAtom;

        public ArgSpec(ArgSpec prev, IEnumerable<ZilObject> argspec)
            : this(prev.name, argspec)
        {
            Contract.Requires(prev != null);
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
        }

        public ArgSpec(ZilAtom name, IEnumerable<ZilObject> argspec)
        {
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));

            this.name = name;

            optArgsStart = -1;
            auxArgsStart = -1;

            List<ZilAtom> argAtoms = new List<ZilAtom>();
            List<ZilObject> argDecls = new List<ZilObject>();
            List<bool> argQuoted = new List<bool>();
            List<ZilObject> argDefaults = new List<ZilObject>();

            int cur = 0;
            bool gotVarargs = false;
            foreach (ZilObject arg in argspec)
            {
                // check for arg clause separators: "OPT", "AUX", etc.
                if (arg is ZilString)
                {
                    string sep = ((ZilString)arg).Text;
                    switch (sep)
                    {
                        case "OPT":
                        case "OPTIONAL":
                            if (optArgsStart != -1)
                                throw new InterpreterError("multiple \"OPT\" clauses");
                            if (auxArgsStart != -1)
                                throw new InterpreterError("\"OPT\" after \"AUX\"");
                            optArgsStart = cur;
                            continue;
                        case "AUX":
                        case "EXTRA":
                            if (auxArgsStart != -1)
                                throw new InterpreterError("multiple \"AUX\" clauses");
                            auxArgsStart = cur;
                            continue;
                        case "ARGS":
                        case "TUPLE":
                            if (varargsAtom != null)
                                throw new InterpreterError("multiple \"ARGS\" or \"TUPLE\" clauses");
                            gotVarargs = true;
                            varargsQuoted = (sep == "ARGS");
                            continue;
                        default:
                            throw new InterpreterError("unexpected clause in arg spec: " + arg.ToString());
                    }
                }

                if (gotVarargs)
                {
                    varargsAtom = arg as ZilAtom;
                    if (varargsAtom == null)
                        throw new InterpreterError("\"ARGS\" or \"TUPLE\" must be followed by an atom");

                    gotVarargs = false;
                    continue;
                }

                // it's a real arg
                cur++;

                bool quoted = false;
                ZilObject argName, argValue, argDecl;

                // could be an atom or a list: (atom defaultValue)
                if (arg is ZilList && !(arg is ZilForm))
                {
                    ZilList al = (ZilList)arg;

                    if (al.IsEmpty)
                        throw new InterpreterError("empty list in arg spec");

                    argName = al.First;
                    argValue = al.Rest.First;
                }
                else
                {
                    argName = arg;
                    argValue = null;
                }

                // could be quoted
                if (argName is ZilForm)
                {
                    ZilForm af = (ZilForm)argName;
                    if (af.First is ZilAtom && ((ZilAtom)af.First).StdAtom == StdAtom.QUOTE &&
                        !af.Rest.IsEmpty)
                    {
                        quoted = true;
                        quoteAtom = (ZilAtom)af.First;
                        argName = af.Rest.First;
                    }
                    else
                        throw new InterpreterError("unexpected FORM in arg spec: " + argName.ToString());
                }

                // could be an ADECL
                if (argName is ZilAdecl)
                {
                    var adecl = (ZilAdecl)argName;
                    argDecl = adecl.Second;
                    argName = adecl.First;
                }
                else
                {
                    argDecl = null;
                }

                // it'd better be an atom by now
                if (!(argName is ZilAtom))
                {
                    throw new InterpreterError("expected atom in arg spec but found " + argName.ToString());
                }

                argAtoms.Add((ZilAtom)argName);
                argDecls.Add(argDecl);
                argDefaults.Add(argValue);
                argQuoted.Add(quoted);
            }

            if (auxArgsStart == -1)
                auxArgsStart = cur;
            if (optArgsStart == -1)
                optArgsStart = auxArgsStart;

            this.argAtoms = argAtoms.ToArray();
            this.argDecls = argDecls.ToArray();
            this.argQuoted = argQuoted.ToArray();
            this.argDefaults = argDefaults.ToArray();
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(argAtoms != null && Contract.ForAll(argAtoms, a => a != null));
            Contract.Invariant(argDecls != null && argDecls.Length == argAtoms.Length);
            Contract.Invariant(argDefaults != null && argDefaults.Length == argAtoms.Length);
            Contract.Invariant(argQuoted != null && argQuoted.Length == argAtoms.Length);
            Contract.Invariant(argDefaults != null && argDefaults.Length == argAtoms.Length);
            Contract.Invariant(optArgsStart >= 0 && optArgsStart <= argAtoms.Length);
            Contract.Invariant(auxArgsStart >= optArgsStart && auxArgsStart <= argAtoms.Length);
            Contract.Invariant(varargsAtom != null || !varargsQuoted);
            Contract.Invariant(MinArgCount >= 0);
            Contract.Invariant(MaxArgCount == 0 || MaxArgCount >= MinArgCount);
        }

        public int MinArgCount
        {
            get { return optArgsStart; }
        }

        public int MaxArgCount
        {
            get { return (varargsAtom != null) ? 0 : auxArgsStart; }
        }

        public IEnumerator<ArgItem> GetEnumerator()
        {
            ArgItem.ArgType type = ArgItem.ArgType.Required;

            for (int i = 0; i < argAtoms.Length; i++)
            {
                if (i == auxArgsStart)
                    type = ArgItem.ArgType.Auxiliary;
                else if (i == optArgsStart)
                    type = ArgItem.ArgType.Optional;

                yield return new ArgItem(argAtoms[i], argQuoted[i], argDefaults[i], type);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string ToString(Func<ZilObject, string> convert)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            bool first = true;
            foreach (var item in this.AsZilListBody())
            {
                if (!first)
                    sb.Append(' ');

                first = false;

                sb.Append(convert(item));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString()
        {
            return this.ToString(zo => zo.ToString());
        }

        public string ToStringContext(Context ctx, bool friendly)
        {
            return this.ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override bool Equals(object obj)
        {
            ArgSpec other = obj as ArgSpec;
            if (other == null)
                return false;

            int numArgs = this.argAtoms.Length;
            if (other.argAtoms.Length != numArgs ||
                other.optArgsStart != this.optArgsStart ||
                other.auxArgsStart != this.auxArgsStart ||
                other.varargsAtom != this.varargsAtom ||
                other.varargsQuoted != this.varargsQuoted)
                return false;

            for (int i = 0; i < numArgs; i++)
            {
                if (other.argAtoms[i] != this.argAtoms[i])
                    return false;
                if (other.argQuoted[i] != this.argQuoted[i])
                    return false;
                if (!object.Equals(other.argDefaults[i], this.argDefaults[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = (argAtoms.Length << 1) ^ (optArgsStart << 2) ^ (auxArgsStart << 3);
            result ^= varargsAtom.GetHashCode();
            result ^= varargsQuoted.GetHashCode();

            for (int i = 0; i < argAtoms.Length; i++)
            {
                result ^= argAtoms[i].GetHashCode();

                if (argDecls[i] != null)
                    result ^= argDecls[i].GetHashCode();

                result ^= argQuoted[i].GetHashCode();

                if (argDefaults[i] != null)
                    result ^= argDefaults[i].GetHashCode();
            }

            return result;
        }

        public void BeginApply(Context ctx, ZilObject[] args, bool eval)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null && Contract.ForAll(args, a => a != null));

            if (args.Length < optArgsStart || (args.Length > auxArgsStart && varargsAtom == null))
                throw new InterpreterError(
                    name == null ? "user-defined function" : name.ToString(),
                    optArgsStart, auxArgsStart);

            for (int i = 0; i < optArgsStart; i++)
            {
                ZilObject value = (!eval || argQuoted[i]) ? args[i] : args[i].Eval(ctx);
                ctx.PushLocalVal(argAtoms[i], value);
            }

            for (int i = optArgsStart; i < auxArgsStart; i++)
            {
                if (i < args.Length)
                {
                    ZilObject value = (!eval || argQuoted[i]) ? args[i] : args[i].Eval(ctx);
                    ctx.PushLocalVal(argAtoms[i], value);
                }
                else
                {
                    ctx.PushLocalVal(argAtoms[i], argDefaults[i] == null ? null : argDefaults[i].Eval(ctx));
                }
            }

            for (int i = auxArgsStart; i < argAtoms.Length; i++)
            {
                ctx.PushLocalVal(argAtoms[i], argDefaults[i] == null ? null : argDefaults[i].Eval(ctx));
            }

            if (varargsAtom != null)
            {
                var extras = args.Skip(auxArgsStart);
                if (eval && !varargsQuoted)
                    extras = ZilObject.EvalSequence(ctx, extras);
                ctx.PushLocalVal(varargsAtom, new ZilList(extras));
            }
        }

        public void EndApply(Context ctx)
        {
            Contract.Requires(ctx != null);

            foreach (ZilAtom atom in argAtoms)
                ctx.PopLocalVal(atom);
        }

        public ZilList ToZilList()
        {
            Contract.Ensures(Contract.Result<ZilList>() != null);

            return new ZilList(this.AsZilListBody());
        }

        public IEnumerable<ZilObject> AsZilListBody()
        {
            for (int i = 0; i < argAtoms.Length; i++)
            {
                // TODO: include "ARGS" or "TUPLE"
                // TODO: return ADECLs for args with decls
                if (i == auxArgsStart)
                    yield return new ZilString("AUX");
                else if (i == optArgsStart)
                    yield return new ZilString("OPT");

                ZilObject arg = argAtoms[i];

                if (argQuoted[i])
                {
                    arg = new ZilForm(new ZilObject[] { quoteAtom, arg });
                }

                if (argDefaults[i] != null)
                {
                    arg = new ZilList(arg,
                        new ZilList(argDefaults[i],
                            new ZilList(null, null)));
                }

                yield return arg;
            }
        }
    }

    [BuiltinType(StdAtom.FUNCTION, PrimType.LIST)]
    class ZilFunction : ZilObject, IApplicable, IStructure
    {
        private ArgSpec argspec;
        private readonly ZilObject[] body;

        public ZilFunction(ZilAtom name, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body)
        {
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
            Contract.Requires(body != null && Contract.ForAll(body, b => b != null));

            this.argspec = new ArgSpec(name, argspec);
            this.body = body.ToArray();
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(argspec != null);
            Contract.Invariant(body != null);
            Contract.Invariant(Contract.ForAll(body, b => b != null));
        }

        [ChtypeMethod]
        public static ZilFunction FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilFunction>() != null);

            if (list.First != null && list.First.GetTypeAtom(ctx).StdAtom == StdAtom.LIST &&
                list.Rest != null && list.Rest.First != null)
            {
                return new ZilFunction(
                    null,
                    (ZilList)list.First,
                    list.Rest);
            }

            throw new InterpreterError("List does not match FUNCTION pattern");
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);

            StringBuilder sb = new StringBuilder();

            sb.Append("#FUNCTION (");
            sb.Append(argspec.ToString(convert));

            foreach (ZilObject expr in body)
            {
                sb.Append(' ');
                sb.Append(convert(expr));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FUNCTION);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(1 + body.Length);
            result.Add(argspec.ToZilList());
            result.AddRange(body);
            return new ZilList(result);
        }

        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            argspec.BeginApply(ctx, args, true);
            try
            {
                return ZilObject.EvalProgram(ctx, body);
            }
            finally
            {
                argspec.EndApply(ctx);
            }
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            argspec.BeginApply(ctx, args, false);
            try
            {
                return ZilObject.EvalProgram(ctx, body);
            }
            finally
            {
                argspec.EndApply(ctx);
            }
        }

        public override bool Equals(object obj)
        {
            ZilFunction other = obj as ZilFunction;
            if (other == null)
                return false;

            if (!other.argspec.Equals(this.argspec))
                return false;

            if (other.body.Length != this.body.Length)
                return false;

            for (int i = 0; i < body.Length; i++)
                if (!other.body[i].Equals(this.body[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = argspec.GetHashCode();

            foreach (ZilObject obj in body)
                result ^= obj.GetHashCode();

            return result;
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return argspec.ToZilList();
        }

        public IStructure GetRest(int skip)
        {
            return new ZilList(body.Skip(skip - 1));
        }

        public bool IsEmpty()
        {
            return false;
        }

        public ZilObject this[int index]
        {
            get
            {
                if (index == 0)
                    return argspec.ToZilList();
                else
                    return body[index - 1];
            }
            set
            {
                if (index == 0)
                    argspec = new ArgSpec(argspec, (IEnumerable<ZilObject>)value);
                else
                    body[index - 1] = value;
            }
        }

        public int GetLength()
        {
            return body.Length + 1;
        }

        public int? GetLength(int limit)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    [BuiltinType(StdAtom.MACRO, PrimType.LIST)]
    class ZilEvalMacro : ZilObject, IApplicable, IStructure
    {
        private ZilObject value;

        public ZilEvalMacro(ZilObject value)
        {
            if (!(value is IApplicable))
                throw new ArgumentException("Arg must be an applicable object");

            this.value = value;
        }

        [ChtypeMethod]
        public static ZilEvalMacro FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilEvalMacro>() != null);

            if (list.First != null && list.First is IApplicable &&
                list.Rest != null && list.Rest.First == null)
            {
                return new ZilEvalMacro(list.First);
            }

            throw new InterpreterError("List does not match MACRO pattern");
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return "#MACRO (" + convert(value) + ")";
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.MACRO);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(value, new ZilList(null, null));
        }

        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            ZilObject expanded = Expand(ctx, args);
            return expanded.Eval(ctx);
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            ZilObject expanded = ExpandNoEval(ctx, args);
            return expanded.Eval(ctx);
        }

        public ZilObject Expand(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            Context expandCtx = ctx.CloneWithNewLocals();
            return ((IApplicable)value).Apply(expandCtx, args);
        }

        public ZilObject ExpandNoEval(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            Context expandCtx = ctx.CloneWithNewLocals();
            return ((IApplicable)value).ApplyNoEval(expandCtx, args);
        }

        public override bool Equals(object obj)
        {
            ZilEvalMacro other = obj as ZilEvalMacro;
            return other != null && other.value.Equals(this.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return value;
        }

        public IStructure GetRest(int skip)
        {
            return null;
        }

        public bool IsEmpty()
        {
            return false;
        }

        public ZilObject this[int index]
        {
            get
            {
                return index == 0 ? value : null;
            }
            set
            {
                if (index == 0)
                    this.value = value;
            }
        }

        public int GetLength()
        {
            return 1;
        }

        public int? GetLength(int limit)
        {
            return limit >= 1 ? 1 : (int?)null;
        }

        #endregion
    }

    #endregion
}
