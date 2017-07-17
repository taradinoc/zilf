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

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ATOM, PrimType.ATOM)]
    class ZilAtom : ZilObject
    {
        ObList list;

        public ZilAtom(string text, ObList list, StdAtom stdAtom)
        {
            Text = text;
            this.list = list;
            StdAtom = stdAtom;
        }

        [ChtypeMethod]
        public static ZilAtom FromAtom([NotNull] Context ctx, ZilAtom other)
        {
            Contract.Requires(ctx != null);

            // we can't construct a new atom since it wouldn't be equal to the old one
            return other;
        }

        public string Text { get; }

        [CanBeNull]
        public ObList ObList
        {
            get => list;

            set
            {
                if (value == list)
                    return;

                var oldValue = list;
                list = value;

                oldValue?.Remove(this);

                value?.Add(this);
            }
        }

        public StdAtom StdAtom { get; }

        /// <summary>
        /// Parses an atom name, including !- separators, and returns the atom
        /// object. Creates the atom or oblist(s) if necessary.
        /// </summary>
        /// <param name="text">The atom name.</param>
        /// <param name="ctx">The current context.</param>
        /// <returns>The parsed atom.</returns>
        /// <remarks>This method does not strip backslashes from <see cref="text"/>.</remarks>
        /// <exception cref="InterpreterError">No OBLIST path.</exception>
        public static ZilAtom Parse([NotNull] string text, [NotNull] Context ctx)
        {
            Contract.Requires(text != null);
            Contract.Requires(ctx != null);

            ZilAtom result;
            var idx = text.IndexOf("!-", System.StringComparison.Ordinal);

            if (idx == -1)
            {
                // look for it in <1 .OBLIST>, <2 .OBLIST>...
                var pathspec = ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST));
                if (!(pathspec is IEnumerable<ZilObject> zos))
                    throw new InterpreterError(InterpreterMessages.No_OBLIST_Path);

                ObList insertList = null;
                var gotDefault = false;

                foreach (var obj in zos)
                {
                    switch (obj)
                    {
                        case ObList oblist:
                            if (oblist.Contains(text))
                                return oblist[text];

                            if (insertList == null || gotDefault)
                            {
                                insertList = oblist;
                                gotDefault = false;
                            }
                            break;

                        case ZilAtom atom when atom.StdAtom == StdAtom.DEFAULT:
                            gotDefault = true;
                            break;
                    }
                }

                if (insertList == null)
                    throw new InterpreterError(InterpreterMessages.No_OBLIST_Path);

                // not found, insert
                result = new ZilAtom(text, insertList, StdAtom.None);
                insertList[text] = result;
                return result;
            }

            // look for it in the specified oblist
            ObList list;
            if (idx == text.Length - 2)
            {
                list = ctx.RootObList;
            }
            else
            {
                var olname = Parse(text.Substring(idx + 2), ctx);
                list = ctx.GetProp(olname, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(olname);
            }

            var pname = text.Substring(0, idx);

            if (list.Contains(pname))
                return list[pname];

            result = new ZilAtom(pname, list, StdAtom.None);
            list[pname] = result;
            return result;
        }

        bool NeedsObListTrailer([ItemNotNull] [NotNull] [InstantHandle] IEnumerable<ZilObject> obListPath)
        {
            // if this atom can be found by looking up its name in the oblist path, no trailer is needed.
            // thus, the trailer is only needed if (1) looking up that name returns a different atom first
            // or (2) no atom by that name can be found in the path.

            Contract.Requires(obListPath != null);
            // if this atom can be found by looking up its name in the oblist path, no trailer is needed.
            // thus, the trailer is only needed if (1) looking up that name returns a different atom first
            // or (2) no atom by that name can be found in the path.

            foreach (var oblist in obListPath.OfType<ObList>())
                if (oblist.Contains(Text))
                    return oblist[Text] != this;

            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Text.Length);

            foreach (char c in Text)
            {
                switch (c) {
                    case '<':
                    case '>':
                    case '(':
                    case ')':
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case ':':
                    case ';':
                    case '"':
                    case '\'':
                    case '\\':
                    case ',':
                    case '%':
                    case '#':
                        sb.Append('\\');
                        break;

                    default:
                        if (char.IsWhiteSpace(c))
                            sb.Append('\\');
                        break;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        protected override string ToStringContextImpl([ProvidesContext] Context ctx, bool friendly)
        {
            if (friendly)
                return ToString();

            var sb = new StringBuilder(ToString());
            var oblistAtom = ctx.GetStdAtom(StdAtom.OBLIST);
            var oblistPath = ctx.GetLocalVal(oblistAtom) as IStructure;

            var name = this;
            var oblist = ObList;

            if (oblist == null)
            {
                sb.Append("!-#FALSE ()");
            }
            else
            {
                while (oblist != null && (oblistPath == null || name.NeedsObListTrailer(oblistPath)))
                {
                    if (oblist == ctx.RootObList)
                    {
                        sb.Append("!-");
                        break;
                    }

                    name = ctx.GetProp(oblist, oblistAtom) as ZilAtom;
                    if (name == null)
                        break;

                    sb.Append("!-");
                    sb.Append(name);

                    oblist = name.ObList;
                }
            }

            return sb.ToString();
        }

        public override StdAtom StdTypeAtom => StdAtom.ATOM;

        public override PrimType PrimType => PrimType.ATOM;

        [NotNull]
        public override ZilObject GetPrimitive(Context ctx)
        {
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            return this;
        }
    }
}