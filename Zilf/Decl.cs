using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Zilf
{
    class Decl
    {
        public static bool Check(Context ctx, ZilObject value, ZilObject pattern)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            Contract.Requires(pattern != null);

            ZilAtom atom;

            switch (pattern.GetTypeAtom(ctx).StdAtom)
            {
                case StdAtom.ATOM:
                    atom = (ZilAtom)pattern;
                    switch (atom.StdAtom)
                    {
                        case StdAtom.ANY:
                            return true;
                        case StdAtom.APPLICABLE:
                            return (value is IApplicable);
                        case StdAtom.STRUCTURED:
                            return (value is IStructure);
                        default:
                            return (value.GetTypeAtom(ctx) == atom);
                    }

                default:
                    throw new NotImplementedException("non-ATOM in DECL pattern");
            }
        }
    }
}
