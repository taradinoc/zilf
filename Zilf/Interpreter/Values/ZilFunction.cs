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
}