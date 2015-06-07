using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
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
            get { return PrimType.ATOM; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }
    }
}