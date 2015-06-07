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
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
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
            get { return PrimType.LIST; }
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
}