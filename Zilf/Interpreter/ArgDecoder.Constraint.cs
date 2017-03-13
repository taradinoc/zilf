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
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    partial class ArgDecoder
    {
        abstract class Constraint
        {
            public static readonly Constraint AnyObject = new AnyObjectConstraint();
            public static readonly Constraint Forbidden = new ForbiddenConstraint();
            public static readonly Constraint Structured = new StructuredConstraint();
            public static readonly Constraint Applicable = new ApplicableConstraint();
            public static Constraint OfType(StdAtom typeAtom) => new TypeConstraint(typeAtom);
            public static Constraint OfPrimType(PrimType primtype) => new PrimTypeConstraint(primtype);

            public static Constraint FromDecl(Context ctx, ZilObject pattern)
            {
                var form = pattern as ZilForm;
                if (form != null)
                {
                    var head = form.First as ZilAtom;
                    if (head != null)
                    {
                        switch (head.StdAtom)
                        {
                            case StdAtom.OR:
                                return form.Rest
                                    .Select(zo => FromDecl(ctx, zo))
                                    .Aggregate(Forbidden, (a, b) => Disjunction.From(ctx, a, b));

                            case StdAtom.PRIMTYPE:
                                return OfPrimType(ctx.GetTypePrim((ZilAtom)form.Rest.First));

                            case StdAtom.None:
                                break;

                            // XXX may need to combine this with a contents constraint
                            //default:
                            //    return OfType(head.StdAtom);
                        }
                    }
                }

                var atom = pattern as ZilAtom;
                if (atom != null)
                {
                    switch (atom.StdAtom)
                    {
                        case StdAtom.ANY:
                            return AnyObject;

                        case StdAtom.APPLICABLE:
                            return Applicable;

                        case StdAtom.STRUCTURED:
                            return Structured;

                        case StdAtom.None:
                            break;

                        default:
                            return OfType(atom.StdAtom);
                    }
                }

                return new DeclConstraint(pattern);
            }

            public virtual Constraint And(Context ctx, Constraint other)
            {
                switch (this.CompareImpl(ctx, other) ?? Invert(other.CompareImpl(ctx, this)))
                {
                    case CompareOutcome.Looser:
                        return other;

                    case CompareOutcome.Stricter:
                        return this;

                    default:
                        return Conjunction.From(ctx, this, other);
                }
            }

            public virtual Constraint Or(Context ctx, Constraint other)
            {
                switch (this.CompareImpl(ctx, other) ?? Invert(other.CompareImpl(ctx, this)))
                {
                    case CompareOutcome.Looser:
                        return this;

                    case CompareOutcome.Stricter:
                        return other;

                    default:
                        return Disjunction.From(ctx, this, other);
                }
            }

            protected CompareOutcome? CompareTo(Context ctx, Constraint other)
            {
                return CompareImpl(ctx, other) ?? Invert(other.CompareImpl(ctx, this));
            }

            protected abstract CompareOutcome? CompareImpl(Context ctx, Constraint other);
            public abstract bool Allows(Context ctx, ZilObject arg);
            public abstract override string ToString();

            protected enum CompareOutcome
            {
                Looser,
                Equal,
                Stricter,
            }

            protected static CompareOutcome? Invert(CompareOutcome? co)
            {
                switch (co)
                {
                    case CompareOutcome.Looser:
                        return CompareOutcome.Stricter;

                    case CompareOutcome.Stricter:
                        return CompareOutcome.Looser;

                    default:
                        return co;
                }
            }

            static string EnglishList(IEnumerable<string> items, string connector)
            {
                var array = items.ToArray();

                Contract.Assert(array.Length > 0);

                switch (array.Length)
                {
                    case 1:
                        return array[0];

                    case 2:
                        return array[0] + " " + connector + " " + array[1];

                    default:
                        return string.Join(", ", items.Take(array.Length - 1)) + ", " + connector + " " + array[array.Length - 1];
                }
            }

            class AnyObjectConstraint : Constraint
            {
                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return true;
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    return (other is AnyObjectConstraint) ? CompareOutcome.Equal : CompareOutcome.Looser;
                }

                public override string ToString()
                {
                    return "anything";
                }
            }

            class ForbiddenConstraint : Constraint
            {
                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return false;
                }

                public override string ToString()
                {
                    return "nothing";
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    return (other is AnyObjectConstraint) ? CompareOutcome.Equal : CompareOutcome.Stricter;
                }
            }

            class TypeConstraint : Constraint
            {
                public StdAtom TypeAtom { get; }

                public TypeConstraint(StdAtom typeAtom)
                {
                    this.TypeAtom = typeAtom;
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    var otherType = other as TypeConstraint;

                    if (otherType != null && otherType.TypeAtom == this.TypeAtom)
                    {
                        return CompareOutcome.Equal;
                    }

                    return null;
                }

                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return arg.StdTypeAtom == this.TypeAtom;
                }

                public override string ToString()
                {
                    return this.TypeAtom.ToString();
                }
            }

            class PrimTypeConstraint : Constraint
            {
                public PrimType PrimType { get; }

                public PrimTypeConstraint(PrimType primtype)
                {
                    this.PrimType = primtype;
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    var otherPrimType = other as PrimTypeConstraint;

                    if (otherPrimType != null)
                    {
                        if (otherPrimType.PrimType == this.PrimType)
                            return CompareOutcome.Equal;

                        return null;
                    }

                    var otherType = other as TypeConstraint;

                    if (otherType != null &&
                        ctx.GetTypePrim(ctx.GetStdAtom(otherType.TypeAtom)) == this.PrimType)
                    {
                        return CompareOutcome.Looser;
                    }

                    return null;
                }

                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return arg.PrimType == this.PrimType;
                }

                public override string ToString()
                {
                    return "PRIMTYPE " + this.PrimType.ToString();
                }
            }

            class StructuredConstraint : Constraint
            {
                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return arg is IStructure;
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    if (other is StructuredConstraint)
                    {
                        return CompareOutcome.Equal;
                    }

                    var otherType = other as TypeConstraint;

                    if (otherType != null && ctx.IsStructuredType(ctx.GetStdAtom(otherType.TypeAtom)))
                    {
                        return CompareOutcome.Looser;
                    }

                    return null;
                }

                public override string ToString()
                {
                    return "structured value";
                }
            }

            class ApplicableConstraint : Constraint
            {
                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return arg.IsApplicable(ctx);
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    if (other is ApplicableConstraint)
                    {
                        return CompareOutcome.Equal;
                    }

                    var otherType = other as TypeConstraint;

                    if (otherType != null && ctx.IsApplicableType(ctx.GetStdAtom(otherType.TypeAtom)))
                    {
                        return CompareOutcome.Looser;
                    }

                    return null;
                }

                public override string ToString()
                {
                    return "applicable value";
                }
            }

            class DeclConstraint : Constraint
            {
                public ZilObject Pattern { get; }

                public DeclConstraint(ZilObject pattern)
                {
                    this.Pattern = pattern;
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    if (other is DeclConstraint &&
                        this.Pattern.Equals(((DeclConstraint)other).Pattern))
                    {
                        return CompareOutcome.Equal;
                    }

                    return null;
                }

                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return Decl.Check(ctx, arg, this.Pattern);
                }

                public override string ToString()
                {
                    return this.Pattern.ToString();
                }
            }

            class Conjunction : Constraint
            {
                public IEnumerable<Constraint> Constraints { get; }

                Conjunction(IEnumerable<Constraint> constraints)
                {
                    this.Constraints = constraints;
                }

                public static Conjunction From(Context ctx, Constraint left, Constraint right)
                {
                    var parts = new List<Constraint>();

                    if (left is Conjunction)
                    {
                        parts.AddRange(((Conjunction)left).Constraints);
                    }
                    else
                    {
                        parts.Add(left);
                    }

                    IEnumerable<Constraint> todo;
                    if (right is Conjunction)
                    {
                        todo = ((Conjunction)right).Constraints;
                    }
                    else
                    {
                        todo = Enumerable.Repeat(right, 1);
                    }

                    foreach (var c in todo)
                    {
                        int stricter = 0, looserOrEqual = 0;

                        foreach (var p in parts)
                        {
                            switch (c.CompareTo(ctx, p))
                            {
                                case CompareOutcome.Stricter:
                                    stricter++;
                                    break;

                                case CompareOutcome.Looser:
                                case CompareOutcome.Equal:
                                    looserOrEqual++;
                                    break;
                            }
                        }

                        if (looserOrEqual > 0)
                        {
                            // we already have this one, skip it
                            continue;
                        }

                        if (stricter > 0)
                        {
                            // this one is stricter than some we currently have, remove them
                            parts.RemoveAll(p => c.CompareTo(ctx, p) == CompareOutcome.Stricter);
                        }

                        parts.Add(c);
                    }

                    return new Conjunction(parts);
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    int count = 0, looser = 0, equal = 0;

                    foreach (var c in this.Constraints)
                    {
                        count++;

                        switch (c.CompareTo(ctx, other))
                        {
                            case CompareOutcome.Stricter:
                                // done
                                return CompareOutcome.Stricter;

                            case CompareOutcome.Looser:
                                looser++;
                                break;

                            case CompareOutcome.Equal:
                                equal++;
                                break;
                        }
                    }

                    if (equal == count)
                        return CompareOutcome.Equal;

                    if (looser == count)
                        return CompareOutcome.Looser;

                    return null;
                }

                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return this.Constraints.All(c => c.Allows(ctx, arg));
                }

                public override string ToString()
                {
                    return EnglishList(this.Constraints.Select(c => c.ToString()).OrderBy(s => s), "and");
                }
            }

            class Disjunction : Constraint
            {
                public IEnumerable<Constraint> Constraints { get; }

                Disjunction(IEnumerable<Constraint> constraints)
                {
                    this.Constraints = constraints;
                }

                public static Disjunction From(Context ctx, Constraint left, Constraint right)
                {
                    var parts = new List<Constraint>();

                    if (left is Disjunction)
                    {
                        parts.AddRange(((Disjunction)left).Constraints);
                    }
                    else
                    {
                        parts.Add(left);
                    }

                    IEnumerable<Constraint> todo;
                    if (right is Disjunction)
                    {
                        todo = ((Disjunction)right).Constraints;
                    }
                    else
                    {
                        todo = Enumerable.Repeat(right, 1);
                    }

                    foreach (var c in todo)
                    {
                        int looser = 0, stricterOrEqual = 0;

                        foreach (var p in parts)
                        {
                            switch (c.CompareTo(ctx, p))
                            {
                                case CompareOutcome.Looser:
                                    looser++;
                                    break;

                                case CompareOutcome.Stricter:
                                case CompareOutcome.Equal:
                                    stricterOrEqual++;
                                    break;
                            }
                        }

                        if (stricterOrEqual > 0)
                        {
                            // we already have this one, skip it
                            continue;
                        }

                        if (looser > 0)
                        {
                            // this one is looser than some we currently have, remove them
                            parts.RemoveAll(p => c.CompareTo(ctx, p) == CompareOutcome.Looser);
                        }

                        parts.Add(c);
                    }

                    return new Disjunction(parts);
                }

                protected override CompareOutcome? CompareImpl(Context ctx, Constraint other)
                {
                    int count = 0, stricter = 0, equal = 0;

                    foreach (var c in this.Constraints)
                    {
                        count++;

                        switch (c.CompareTo(ctx, other))
                        {
                            case CompareOutcome.Looser:
                                // done
                                return CompareOutcome.Looser;

                            case CompareOutcome.Stricter:
                                stricter++;
                                break;

                            case CompareOutcome.Equal:
                                equal++;
                                break;
                        }
                    }

                    if (equal == count)
                        return CompareOutcome.Equal;

                    if (stricter == count)
                        return CompareOutcome.Stricter;

                    return null;
                }

                public override bool Allows(Context ctx, ZilObject arg)
                {
                    return this.Constraints.Any(c => c.Allows(ctx, arg));
                }

                public override string ToString()
                {
                    return EnglishList(this.Constraints.Select(c => c.ToString()).OrderBy(s => s), "or");
                }
            }
        }
    }
}
