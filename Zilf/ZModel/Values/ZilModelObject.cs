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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    class ZilModelObject : ZilObject
    {
        private readonly ZilAtom name;
        private readonly ZilList[] props;
        private readonly bool isRoom;

        public ZilModelObject(ZilAtom name, ZilList[] props, bool isRoom)
        {
            this.name = name;
            this.props = props;
            this.isRoom = isRoom;
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilList[] Properties
        {
            get { return props; }
        }

        public bool IsRoom
        {
            get { return isRoom; }
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);

            StringBuilder sb = new StringBuilder("#OBJECT (");
            sb.Append(convert(name));

            foreach (ZilList p in props)
            {
                sb.Append(' ');
                sb.Append(convert(p));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.OBJECT);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(1 + props.Length);
            result.Add(name);
            result.AddRange(props);
            return new ZilList(result);
        }
    }
}