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

namespace Zilf
{
    abstract class ZilObject
    {
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
        /// <returns>The translated object.</returns>
        private static ZilObject ReadOneFromAST(ITree tree, Context ctx)
        {
            switch (tree.Type)
            {
                case ZilLexer.ATOM:
                    return ZilAtom.Parse(tree.Text, ctx);
                case ZilLexer.CHAR:
                    return new ZilChar(tree.Text[2]);
                case ZilLexer.COMMENT:
                    // ignore comments
                    return null;
                case ZilLexer.FORM:
                    ZilObject[] children = ReadChildrenFromAST(tree, ctx);
                    if (children.Length == 0)
                        return ctx.FALSE;
                    else
                        return new ZilForm(ctx.CurrentFile, tree.Line, children);
                case ZilLexer.HASH:
                    return ZilHash.Parse(ctx, ReadChildrenFromAST(tree, ctx));
                case ZilLexer.LIST:
                case ZilLexer.VECTOR:   // TODO: a real ZilVector type?
                case ZilLexer.UVECTOR:
                    return new ZilList(ReadChildrenFromAST(tree, ctx));
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
                            ex.SourceLine = inner as ISourceLine;
                        throw;
                    }
                    catch (ControlException ex)
                    {
                        throw new InterpreterError(inner as ISourceLine, "misplaced " + ex.Message);
                    }
                case ZilLexer.NUM:
                    return new ZilFix(ParseNumber(tree.Text));
                case ZilLexer.SEGMENT:
                    return new ZilSegment(ReadOneFromAST(tree.GetChild(0), ctx));
                case ZilLexer.STRING:
                    return ZilString.Parse(tree.Text);
                default:
                    throw new ArgumentException("Unexpected tree type: " + tree.Type.ToString(), "tree");
            }
        }

        public static ZilObject ChangeType(Context ctx, ZilObject value, ZilAtom type)
        {
            // value might already be the right type
            ZilAtom vtype = value.GetTypeAtom(ctx);
            if (vtype == type)
                return value;

            switch (type.StdAtom)
            {
                case StdAtom.FALSE:
                    // #FALSE (any value)
                    return new ZilFalse(value);

                case StdAtom.SUBR:
                case StdAtom.FSUBR:
                    // #[F]SUBR "method name"
                    if (vtype.StdAtom != StdAtom.STRING)
                        throw new InterpreterError("value cast to [F]SUBR must be a string");

                    string name = value.ToStringContext(ctx, true);
                    MethodInfo mi = typeof(Subrs).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
                    if (mi != null)
                    {
                        object[] attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                        if (attrs.Length == 1)
                        {
                            Subrs.SubrDelegate del = (Subrs.SubrDelegate)Delegate.CreateDelegate(
                                typeof(Subrs.SubrDelegate), mi);

                            return type.StdAtom == StdAtom.SUBR ? new ZilSubr(del) : new ZilFSubr(del);
                        }
                    }
                    throw new InterpreterError("unrecognized [F]SUBR name: " + name);

                case StdAtom.FORM:
                    // #FORM (commands...)
                    if (vtype.StdAtom == StdAtom.LIST)
                        return new ZilForm((IEnumerable<ZilObject>)value);
                    else
                        throw new InterpreterError("value cast to FORM must be a list");

                case StdAtom.GVAL:
                    // #GVAL atom
                    // implemented as an alias for <GVAL atom>
                    if (vtype.StdAtom == StdAtom.ATOM)
                        return new ZilForm(
                            new ZilObject[] { ctx.GetStdAtom(StdAtom.GVAL), value });
                    else
                        throw new InterpreterError("value cast to GVAL must be an atom");

                case StdAtom.CONSTANT:
                case StdAtom.FUNCTION:
                case StdAtom.GLOBAL:
                case StdAtom.MACRO:
                case StdAtom.OBJECT:
                case StdAtom.OBLIST:
                case StdAtom.ROUTINE:
                case StdAtom.TABLE:
                    throw new NotImplementedException("unimplemented cast to " + type.StdAtom);
            }

            return new ZilHash(type, value);
        }

        private static int ParseNumber(string text)
        {
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
            return ToString();
        }

        /// <summary>
        /// Gets an atom representing this object's type.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>The type atom.</returns>
        public abstract ZilAtom GetTypeAtom(Context ctx);

        /// <summary>
        /// Evaluates an object: performs function calls, duplicates lists, etc.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>The result of evaluating this object, which may be the same object.</returns>
        public virtual ZilObject Eval(Context ctx)
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
        /// Evaluates a series of expressions, returning the value of the last expression.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="prog">The expressions to evaluate.</param>
        /// <returns>The value of the last expression evaluated.</returns>
        public static ZilObject EvalProgram(Context ctx, ZilObject[] prog)
        {
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
        protected static IEnumerable<ZilObject> EvalSequence(Context ctx, IEnumerable<ZilObject> sequence)
        {
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
                    yield return obj.Eval(ctx);
                }
            }
        }
    }

    #region Special Types

    class ZilHash : ZilObject
    {
        private readonly ZilAtom type;
        private readonly ZilObject value;

        internal ZilHash(ZilAtom type, ZilObject value)
        {
            this.type = type;
            this.value = value;
        }

        public ZilAtom Type
        {
            get { return type; }
        }

        public ZilObject Value
        {
            get { return value; }
        }

        public static ZilObject Parse(Context ctx, ZilObject[] initializer)
        {
            if (initializer == null)
                throw new ArgumentNullException();
            if (initializer.Length != 2 || !(initializer[0] is ZilAtom) || initializer[1] == null)
                throw new ArgumentException("Expected 2 objects, the first a ZilAtom");

            ZilAtom type = (ZilAtom)initializer[0];
            ZilObject value = initializer[1];

            return ZilObject.ChangeType(ctx, value, type);
        }

        public override string ToString()
        {
            return "#" + type.ToString() + " " + value.ToString();
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return "#" + type.ToStringContext(ctx, friendly) + " " + value.ToStringContext(ctx, friendly);
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return type;
        }

        public override ZilObject Eval(Context ctx)
        {
            return this;
        }
    }

    class ZilSegment : ZilObject
    {
        private ZilForm form;

        public ZilSegment(ZilObject obj)
        {
            if (obj is ZilForm)
                this.form = (ZilForm)obj;
            else
                throw new ArgumentException("Segment must be based on a FORM");
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
    }

    #endregion

    #region Monad Types

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
    }

    class ZilChar : ZilObject
    {
        private readonly char ch;

        public ZilChar(char ch)
        {
            this.ch = ch;
        }

        public char Char
        {
            get { return ch; }
        }

        public override string ToString()
        {
            return "!\\" + ch;
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            if (friendly)
                return ch.ToString();
            else
                return ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.CHARACTER);
        }

        public override bool Equals(object obj)
        {
            ZilChar other = obj as ZilChar;
            return other != null && other.ch == this.ch;
        }

        public override int GetHashCode()
        {
            return ch.GetHashCode();
        }
    }

    class ZilFalse : ZilObject
    {
        private ZilObject value;

        public ZilFalse(ZilObject value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            if (value.GetType() == typeof(ZilList) && ((ZilList)value).IsEmpty)
                return "<>";
            else
                return "#FALSE " + value.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FALSE);
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
    }

    class ZilFix : ZilObject
    {
        private readonly int value;

        public ZilFix(int value)
        {
            this.value = value;
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

        public override bool Equals(object obj)
        {
            ZilFix other = obj as ZilFix;
            return other != null && other.value == this.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }

    #endregion

    #region Structured Types

    /// <summary>
    /// Provides methods common to structured types (LIST, STRING, possibly others).
    /// </summary>
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
        /// <returns>A structure containing the unskipped elements.</returns>
        IStructure GetRest(int skip);

        /// <summary>
        /// Determines whether the structure is empty.
        /// </summary>
        /// <returns>true if the structure has no elements; false if it has any elements.</returns>
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
        int? GetLength(int limit);
    }

    class ZilList : ZilObject, IEnumerable<ZilObject>, IStructure
    {
        public ZilObject First;
        public ZilList Rest;

        public ZilList(IEnumerable<ZilObject> sequence)
        {
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
            this.First = current;
            this.Rest = rest;
        }

        private ZilList MakeRest(IEnumerator<ZilObject> tor)
        {
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
            get { return First == null && Rest == null; }
        }

        protected string ToString(char start, char end, Func<ZilObject, string> convert)
        {
            StringBuilder sb = new StringBuilder(2);
            sb.Append(start);

            foreach (ZilObject obj in this)
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
            return ToString('(', ')', zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString('(', ')', zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.LIST);
        }

        public override ZilObject Eval(Context ctx)
        {
            return new ZilList(EvalSequence(ctx, this));
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
            int result = 0;
            foreach (ZilObject obj in this)
                result ^= obj.GetHashCode();
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

    class ZilForm : ZilList, ISourceLine
    {
        private readonly string filename;
        private readonly int line;

        public ZilForm(IEnumerable<ZilObject> sequence)
            : this(null, 0, sequence)
        {
        }

        public ZilForm(string filename, int line, IEnumerable<ZilObject> sequence)
            : base(sequence)
        {
            this.filename = filename;
            this.line = line;
        }

        public string SourceInfo
        {
            get
            {
                if (filename == null)
                    return null;

                return filename + ":" + line.ToString();
            }
        }

        public string SourceFile
        {
            get { return filename; }
        }

        public int SourceLine
        {
            get { return line; }
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
            return ToString('<', '>', convert);
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
                    return ((IApplicable)target).Apply(ctx,
                        Rest == null ? EmptyObjArray : ((ZilList)Rest).ToArray());
                }
                catch (ZilError ex)
                {
                    if (ex.SourceLine == null)
                        ex.SourceLine = this;
                    throw;
                }
                finally
                {
                    ctx.CallingForm = oldCF;
                }
            }
            else if (target is ZilFix)
            {
                ZilObject[] args = Rest.ToArray();
                if (args.Length == 1)
                    return Subrs.NTH(ctx, new ZilObject[] { args[0].Eval(ctx), target });
                else if (args.Length == 2)
                    return Subrs.PUT(ctx, new ZilObject[] { args[0].Eval(ctx), target, args[1].Eval(ctx) });
                else
                    throw new InterpreterError("expected 1 or 2 args after a FIX");
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
                        else
                            return resultForm.Expand(ctx);
                    }
                    catch (ZilError ex)
                    {
                        if (ex.SourceLine == null)
                            ex.SourceLine = this;
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
                if (Rest != null && Rest.First != null)
                {
                    if (Rest.Rest == null || Rest.Rest.First == null)
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
    }

    class ZilString : ZilObject, IStructure
    {
        public string Text;

        public ZilString(string text)
        {
            this.Text = text;
        }

        public static ZilString Parse(string str)
        {
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
            StringBuilder sb = new StringBuilder(Text.Length + 2);
            sb.Append('"');

            foreach (char c in Text)
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

        ZilObject IStructure.GetFirst()
        {
            if (Text.Length == 0)
                return null;
            else
                return new ZilChar(Text[0]);
        }

        IStructure IStructure.GetRest(int skip)
        {
            if (Text.Length <= skip)
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

            ZilObject IStructure.GetFirst()
            {
                if (offset >= orig.Text.Length)
                    return null;
                else
                    return new ZilChar(orig.Text[offset]);
            }

            IStructure IStructure.GetRest(int skip)
            {
                if (offset >= orig.Text.Length - skip)
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

    #endregion

    #region Applicable Types

    /// <summary>
    /// Provides a method to apply a <see cref="ZilObject"/>, such as a SUBR or FUNCTION,
    /// to a set of arguments.
    /// </summary>
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

    class ZilSubr : ZilObject, IApplicable
    {
        protected readonly Subrs.SubrDelegate handler;

        public ZilSubr(Subrs.SubrDelegate handler)
        {
            this.handler = handler;
        }

        public override string ToString()
        {
            return "#SUBR \"" + handler.Method.Name + "\"";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.SUBR);
        }

        public virtual ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return handler(ctx, EvalSequence(ctx, args).ToArray());
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            return handler(ctx, args);
        }

        public override bool Equals(object obj)
        {
            ZilSubr other = obj as ZilSubr;
            return other != null && other.handler.Equals(this.handler);
        }

        public override int GetHashCode()
        {
            return handler.GetHashCode();
        }
    }

    class ZilFSubr : ZilSubr, IApplicable
    {
        public ZilFSubr(Subrs.SubrDelegate handler)
            : base(handler)
        {
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

        public ZilAtom Atom;
        public bool Quoted;
        public ZilObject DefaultValue;
        public ArgType Type;

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
        private readonly bool[] argQuoted;
        private readonly ZilObject[] argDefaults;
        private readonly int optArgsStart, auxArgsStart;
        private readonly ZilAtom varargsAtom;
        private readonly bool varargsQuoted;

        public ArgSpec(ZilAtom name, IEnumerable<ZilObject> argspec)
        {
            this.name = name;

            optArgsStart = -1;
            auxArgsStart = -1;

            List<ZilAtom> argAtoms = new List<ZilAtom>();
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
                ZilObject argName, argValue;

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
                        argName = af.Rest.First;
                    }
                    else
                        throw new InterpreterError("unexpected FORM in arg spec: " + argName.ToString());
                }

                argAtoms.Add((ZilAtom)argName);
                argDefaults.Add(argValue);
                argQuoted.Add(quoted);
            }

            if (auxArgsStart == -1)
                auxArgsStart = cur;
            if (optArgsStart == -1)
                optArgsStart = auxArgsStart;

            this.argAtoms = argAtoms.ToArray();
            this.argQuoted = argQuoted.ToArray();
            this.argDefaults = argDefaults.ToArray();
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
            StringBuilder sb = new StringBuilder();
            sb.Append('(');

            for (int i = 0; i < argAtoms.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');

                if (i == auxArgsStart)
                    sb.Append("\"AUX\" ");
                else if (i == optArgsStart)
                    sb.Append("\"OPT\" ");

                if (argDefaults[i] == null)
                {
                    sb.Append(convert(argAtoms[i]));
                }
                else
                {
                    sb.Append('(');
                    sb.Append(convert(argAtoms[i]));
                    sb.Append(' ');
                    sb.Append(convert(argDefaults[i]));
                    sb.Append(')');
                }
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
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
                if (other.argDefaults[i] == null && this.argDefaults[i] != null)
                    return false;
                if (!other.argDefaults[i].Equals(this.argDefaults[i]))
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
                result ^= argQuoted[i].GetHashCode();
                if (argDefaults[i] != null)
                    result ^= argDefaults[i].GetHashCode();
            }

            return result;
        }

        public void BeginApply(Context ctx, ZilObject[] args, bool eval)
        {
            if (args.Length < optArgsStart || (args.Length > auxArgsStart && varargsAtom == null))
                throw new InterpreterError(null,
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
                    extras = extras.Select(x => x.Eval(ctx));
                ctx.PushLocalVal(varargsAtom, new ZilList(extras));
            }
        }

        public void EndApply(Context ctx)
        {
            foreach (ZilAtom atom in argAtoms)
                ctx.PopLocalVal(atom);
        }
    }

    class ZilFunction : ZilObject, IApplicable
    {
        private readonly ArgSpec argspec;
        private readonly ZilObject[] body;

        public ZilFunction(ZilAtom name, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body)
        {
            this.argspec = new ArgSpec(name, argspec);
            this.body = body.ToArray();
        }

        private string ToString(Func<ZilObject, string> convert)
        {
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
    }

    class ZilEvalMacro : ZilObject, IApplicable
    {
        private ZilObject value;

        public ZilEvalMacro(ZilObject value)
        {
            if (!(value is IApplicable))
                throw new ArgumentException("Arg must be an applicable object");

            this.value = value;
        }

        private string ToString(Func<ZilObject, string> convert)
        {
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
            Context expandCtx = ctx.CloneWithNewLocals();
            return ((IApplicable)value).Apply(expandCtx, args);
        }

        public ZilObject ExpandNoEval(Context ctx, ZilObject[] args)
        {
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
    }

    #endregion
}
