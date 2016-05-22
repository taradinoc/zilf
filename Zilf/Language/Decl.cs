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

            var ptype = pattern.GetTypeAtom(ctx).StdAtom;

            switch (ptype)
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

                case StdAtom.SEGMENT:
                    pattern = ((ZilSegment)pattern).Form;
                    goto case StdAtom.FORM;

                case StdAtom.FORM:
                    atom = ((ZilForm)pattern).First as ZilAtom;
                    if (atom == null)
                        throw new InterpreterError("FORM in DECL must start with an ATOM");

                    switch (atom.StdAtom)
                    {
                        case StdAtom.OR:
                            foreach (var subpattern in ((ZilForm)pattern).Rest)
                                if (Check(ctx, value, subpattern))
                                    return true;
                            return false;

                        case StdAtom.QUOTE:
                            return ((ZilForm)pattern).Rest.First.Equals(value);

                        default:
                            if (ctx.IsRegisteredType(atom))
                            {
                                if (!Check(ctx, value, atom))
                                    return false;

                                var structure = value as IStructure;
                                if (structure == null)
                                    throw new NotImplementedException("expected a structure for this FORM in DECL: " + pattern.ToStringContext(ctx, false));

                                foreach (var subpattern in ((ZilForm)pattern).Rest)
                                {
                                    if (subpattern is ZilVector)
                                    {
                                        var vector = (ZilVector)subpattern;
                                        var len = vector.GetLength();
                                        if (len > 0 && vector[0] == ctx.GetStdAtom(StdAtom.REST))
                                        {
                                            var i = 1;
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
                                            if (ptype == StdAtom.SEGMENT && i != 1)
                                            {
                                                return false;
                                            }

                                            return true;
                                        }
                                        else
                                        {
                                            throw new NotImplementedException("unhandled VECTOR in FORM: " + vector.ToStringContext(ctx, false));
                                        }
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

                                if (ptype == StdAtom.SEGMENT && !structure.IsEmpty())
                                {
                                    return false;
                                }

                                return true;
                            }
                            throw new NotImplementedException("unhandled FORM in DECL pattern: " + pattern.ToStringContext(ctx, false));
                    }

                default:
                    throw new NotImplementedException("non-ATOM in DECL pattern: " + pattern.ToStringContext(ctx, false));
            }
        }
    }
}
