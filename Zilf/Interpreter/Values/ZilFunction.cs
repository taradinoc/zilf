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
using System.Linq;
using System.Text;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FUNCTION, PrimType.LIST)]
    class ZilFunction : ZilObject, IApplicable, IStructure
    {
        private ArgSpec argspec;
        private readonly ZilObject[] body;

        public ZilFunction(ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body)
        {
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
            Contract.Requires(body != null && Contract.ForAll(body, b => b != null));

            this.argspec = new ArgSpec(name, activationAtom, argspec);
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

            return (ZilFunction)Subrs.FUNCTION(ctx, list.ToArray());
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

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FUNCTION);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
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
            return ApplyImpl(ctx, args, true);
        }

        private ZilObject ApplyImpl(Context ctx, ZilObject[] args, bool eval)
        {
            var activation = argspec.BeginApply(ctx, args, eval);
            bool wasTopLevel = ctx.AtTopLevel;
            try
            {
                ctx.AtTopLevel = false;
                do
                {
                    try
                    {
                        return ZilObject.EvalProgram(ctx, body);
                    }
                    catch (ReturnException ex) when (activation != null && ex.Activation == activation)
                    {
                        return ex.Value;
                    }
                    catch (AgainException ex) when (activation != null && ex.Activation == activation)
                    {
                        // repeat
                    }
                } while (true);
            }
            finally
            {
                ctx.AtTopLevel = wasTopLevel;
                argspec.EndApply(ctx);
            }
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            return ApplyImpl(ctx, args, false);
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
}