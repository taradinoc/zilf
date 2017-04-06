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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.SEGMENT, PrimType.LIST)]
    class ZilSegment : ZilObject, IStructure, IMayExpandBeforeEvaluation
    {
        readonly ZilForm form;

        public ZilSegment(ZilObject obj)
        {
            Contract.Requires(obj != null);

            if (obj is ZilForm form)
                this.form = form;
            else
                throw new ArgumentException("Segment must be based on a FORM");
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(form != null);
        }

        [ChtypeMethod]
        public static ZilSegment FromList(Context ctx, ZilListBase list)
        {
            Contract.Requires(ctx != null);

            if (!(list is ZilForm form))
            {
                form = new ZilForm(list) { SourceLine = SourceLines.Chtyped };
            }

            return new ZilSegment(form);
        }

        public ZilForm Form => form;

        public override string ToString() => "!" + form;

        public override StdAtom StdTypeAtom => StdAtom.SEGMENT;

        public override PrimType PrimType => PrimType.LIST;

        public bool ShouldExpandBeforeEvaluation => true;

        public override ZilObject GetPrimitive(Context ctx) => new ZilList(form);

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType) =>
            throw new InterpreterError(InterpreterMessages.A_SEGMENT_Can_Only_Be_Evaluated_Inside_A_Structure);

        public override bool Equals(object obj) =>
            obj is ZilSegment other && other.form.Equals(this.form);

        public override int GetHashCode() => form.GetHashCode();

        #region IStructure Members

        public ZilObject GetFirst() => ((IStructure)form).GetFirst();

        public IStructure GetRest(int skip) => ((IStructure)form).GetRest(skip);

        public IStructure GetBack(int skip) => throw new NotSupportedException();

        public IStructure GetTop() => throw new NotSupportedException();

        public void Grow(int end, int beginning, ZilObject defaultValue) =>
            throw new NotSupportedException();

        public bool IsEmpty => form.IsEmpty;

        public ZilObject this[int index]
        {
            get => form[index];
            set => form[index] = value;
        }

        public int GetLength() => ((IStructure)form).GetLength();

        public int? GetLength(int limit) => ((IStructure)form).GetLength(limit);

        #endregion

        public IEnumerator<ZilObject> GetEnumerator() => form.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<ZilObject> ExpandBeforeEvaluation(Context ctx, LocalEnvironment env)
        {
            if (Form.Eval(ctx, env) is IEnumerable<ZilObject> result)
                return result;

            throw new InterpreterError(
                InterpreterMessages._0_1_Must_Return_2,
                InterpreterMessages.NoFunction,
                "segment evaluation",
                "a structure");
        }
    }
}