using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zilf.Language;

namespace Zilf.Interpreter.Values.Tied
{
    [BuiltinPrimType(PrimType.LIST)]
    abstract class ZilTiedListBase : ZilObject, IStructure
    {
        protected abstract TiedLayout GetLayout();

        private TiedLayout MyLayout
        {
            get => TiedLayout.Layouts[GetType()];
            set => TiedLayout.Layouts[GetType()] = value;
        }

        protected ZilTiedListBase()
        {
            var myType = GetType();

            if (!TiedLayout.Layouts.ContainsKey(myType))
                MyLayout = GetLayout();
        }

        public sealed override PrimType PrimType => PrimType.LIST;

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(this);
        }

        public override string ToString()
        {
            return SequenceToString(
                this,
                "#" + StdTypeAtom + " (",
                ")",
                zo => zo.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return SequenceToString(
                this,
                "#" + GetTypeAtom(ctx).ToStringContext(ctx, friendly) + " (",
                ")",
                zo => zo.ToStringContext(ctx, friendly));
        }

        private static readonly ObList detachedObList = new ObList();

        protected ZilAtom GetStdAtom(StdAtom stdAtom)
        {
            /* Tied values with atoms in their printed representation may need to return
             * atoms from tied properties even when no Context is available.
             */
            if (Diagnostics.DiagnosticContext.Current?.Frame?.Context is Context ctx)
                return ctx.GetStdAtom(stdAtom);

            return detachedObList[stdAtom.ToString()];
        }

        protected ZilObject FALSE
        {
            get
            {
                if (Diagnostics.DiagnosticContext.Current?.Frame?.Context is Context ctx)
                    return ctx.FALSE;

                return new ZilFalse(new ZilList(null, null));
            }
        }

        public ZilObject this[int index]
        {
            get
            {
                var layout = MyLayout;

                if (index >= 0)
                {
                    if (index < layout.MinLength)
                        return (ZilObject)layout.PropertyInfos[index].GetValue(this);

                    if (layout.CatchAllPropertyInfo is PropertyInfo pi)
                    {
                        var catchAll = (IStructure)pi.GetValue(this);
                        return catchAll[index - layout.MinLength];
                    }
                }

                return null;
            }

            set
            {
                var layout = MyLayout;

                if (index >= 0)
                {
                    if (index < layout.MinLength)
                    {
                        var pi = layout.PropertyInfos[index];

                        if (pi.CanWrite)
                        {
                            pi.SetValue(this, value);
                        }
                        else
                        {
                            throw new NotSupportedException("read-only property");
                        }
                    }
                    else if (layout.CatchAllPropertyInfo is PropertyInfo pi)
                    {
                        if (pi.CanWrite)
                        {
                            var catchAll = (IStructure)pi.GetValue(this);
                            catchAll[index - layout.MinLength] = value;
                            pi.SetValue(this, catchAll);
                        }
                        else
                        {
                            throw new NotSupportedException("read-only property");
                        }
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException("index");
                }
            }
        }

        public bool IsEmpty => GetLength(1) != 0;

        public IStructure GetBack(int skip) => throw new NotSupportedException();

        public IEnumerator<ZilObject> GetEnumerator()
        {
            var layout = MyLayout;

            var query = layout.PropertyInfos.Select(pi => (ZilObject)pi.GetValue(this));

            if (layout.CatchAllPropertyInfo is PropertyInfo pi2)
                query = query.Concat((IStructure)pi2.GetValue(this));

            return query.GetEnumerator();
        }

        public ZilObject GetFirst()
        {
            var layout = MyLayout;

            if (layout.MinLength > 0)
                return (ZilObject)layout.PropertyInfos[0].GetValue(this);

            if (layout.CatchAllPropertyInfo is PropertyInfo pi)
                return ((IStructure)pi.GetValue(this)).GetFirst();

            return null;
        }

        public int GetLength()
        {
            var result = MyLayout.PropertyInfos.Count;

            if (MyLayout.CatchAllPropertyInfo is PropertyInfo pi)
                result += ((IStructure)pi.GetValue(this)).GetLength();

            return result;
        }

        public int? GetLength(int limit)
        {
            var result = MyLayout.PropertyInfos.Count;

            if (result > limit)
                return null;

            if (MyLayout.CatchAllPropertyInfo is PropertyInfo pi)
            {
                var more = ((IStructure)pi.GetValue(this)).GetLength(limit - result);

                if (more == null)
                    return null;

                result += (int)more;

                if (result > limit)
                    return null;
            }

            return result;
        }

        public IStructure GetRest(int skip)
        {
            if (GetLength(skip) < skip)
                return null;

            return new Wrapper(this, skip);
        }

        public IStructure GetTop()
        {
            throw new NotSupportedException();
        }

        public void Grow(int end, int beginning, ZilObject defaultValue) =>
            throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //XXX derive from ZilList?
        [BuiltinAlternate(typeof(ZilList))]
        private class Wrapper : ZilObject, IStructure
        {
            readonly ZilTiedListBase orig;
            readonly int offset;

            public Wrapper(ZilTiedListBase orig, int offset = 0)
            {
                this.orig = orig;
                this.offset = offset;
            }

            public ZilObject this[int index]
            {
                get => orig[offset + index];
                set => orig[offset + index] = value;
            }

            public override StdAtom StdTypeAtom => StdAtom.LIST;

            public override PrimType PrimType => PrimType.LIST;

            public bool IsEmpty => GetLength(1) != 0;

            public IStructure GetBack(int skip)
            {
                if (offset >= skip)
                    return new Wrapper(orig, offset - skip);

                return null;
            }

            public IEnumerator<ZilObject> GetEnumerator()
            {
                return orig.Skip(offset).GetEnumerator();
            }

            public ZilObject GetFirst() => orig[offset];

            public int GetLength() => orig.GetLength() - offset;

            public int? GetLength(int limit) => orig.GetLength(limit + offset) - offset;

            public override ZilObject GetPrimitive(Context ctx)
            {
                return new ZilList(this);
            }

            public IStructure GetRest(int skip)
            {
                if (GetLength(skip) < skip)
                    return null;

                return new Wrapper(orig, offset + skip);
            }

            public IStructure GetTop() => throw new NotSupportedException();

            public void Grow(int end, int beginning, ZilObject defaultValue)
                => throw new NotSupportedException();

            public override string ToString()
            {
                return SequenceToString(
                    this,
                    "(",
                    ")",
                    zo => zo.ToString());
            }

            protected override string ToStringContextImpl(Context ctx, bool friendly)
            {
                return SequenceToString(
                    this,
                    "(",
                    ")",
                    zo => zo.ToStringContext(ctx, friendly));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
