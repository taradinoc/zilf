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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language
{
    class Decl
    {
        public static bool Check(Context ctx, ZilObject value, ZilObject pattern)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            Contract.Requires(pattern != null);

            ZilAtom atom;
            bool segment = false;

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
                            if (ctx.IsRegisteredType(atom))
                                return (value.GetTypeAtom(ctx) == atom);

                            throw new NotImplementedException("unhandled ATOM in DECL pattern: " + atom);
                    }

                case StdAtom.SEGMENT:
                    pattern = ((ZilSegment)pattern).Form;
                    segment = true;
                    goto case StdAtom.FORM;

                case StdAtom.FORM:
                    var form = (ZilForm)pattern;
                    var first = form.First;

                    // special forms
                    atom = first as ZilAtom;
                    if (atom != null)
                    {
                        switch (atom.StdAtom)
                        {
                            case StdAtom.OR:
                                foreach (var subpattern in form.Rest)
                                    if (Check(ctx, value, subpattern))
                                        return true;
                                return false;

                            case StdAtom.QUOTE:
                                return form.Rest.First.Equals(value);

                            case StdAtom.PRIMTYPE:
                                return value.PrimType == ctx.GetTypePrim((ZilAtom)form.Rest.First);
                        }
                    }

                    // structure form: first pattern element is a DECL matched against the whole structure
                    // (usually a type atom), remaining elements are matched against the structure elements
                    if (!(value is IStructure) || !Check(ctx, value, first))
                        return false;

                    return CheckElements(ctx, (IStructure)value, (ZilForm)pattern, segment);

                default:
                    throw new NotImplementedException("non-ATOM in DECL pattern: " + pattern.ToStringContext(ctx, false));
            }
        }

        private static bool CheckElements(Context ctx, IStructure structure, ZilForm pattern, bool segment)
        {
            foreach (var subpattern in pattern.Rest)
            {
                if (subpattern is ZilVector)
                {
                    var vector = (ZilVector)subpattern;
                    var len = vector.GetLength();
                    if (len > 0 && vector[0] is ZilAtom)
                    {
                        int i;

                        switch (((ZilAtom)vector[0]).StdAtom)
                        {
                            case StdAtom.REST:
                                i = 1;
                                while (!structure.IsEmpty())
                                {
                                    if (!Check(ctx, structure.GetFirst(), vector[i]))
                                        return false;

                                    i++;
                                    if (i >= len)
                                        i = 1;

                                    structure = structure.GetRest(1);
                                }

                                // !<FOO [REST A B C]> must repeat A B C a whole number of times
                                // (ZILF extension)
                                if (segment && i != 1)
                                {
                                    return false;
                                }

                                return true;

                            case StdAtom.OPT:
                            case StdAtom.OPTIONAL:
                                // greedily match OPT elements until the structure ends or a match fails
                                for (i = 1; i < len; i++)
                                {
                                    if (structure.IsEmpty() || !Check(ctx, structure.GetFirst(), vector[i]))
                                        break;

                                    structure = structure.GetRest(1);
                                }

                                // move on to the next subpattern, if any
                                continue;
                        }
                    }

                    throw new NotImplementedException("unhandled VECTOR in FORM: " + vector.ToStringContext(ctx, false));
                }
                else
                {
                    if (structure.IsEmpty())
                        return false;

                    if (!Check(ctx, structure.GetFirst(), subpattern))
                        return false;

                    structure = structure.GetRest(1);
                }
            }

            if (segment && !structure.IsEmpty())
            {
                return false;
            }

            return true;
        }
    }
}
