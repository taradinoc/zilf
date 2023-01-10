/* Copyright 2010-2023 Tara McGrew
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
using System.Diagnostics;
using System.Linq;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Signatures
{
    interface IConstraint
    {
        bool Allows(Context ctx, ZilObject arg);
    }

    interface IConstraintVisitor
    {
        void VisitAnyObjectConstraint();
        void VisitApplicableConstraint();
        void VisitBooleanConstraint();
        void VisitConjunctionConstraint(IEnumerable<Constraint> parts);
        void VisitDeclConstraint(ZilObject pattern);
        void VisitDisjunctionConstraint(IEnumerable<Constraint> alts);
        void VisitForbiddenConstraint();
        void VisitPrimTypeConstraint(PrimType primType);
        void VisitStructuredConstraint();
        void VisitTypeConstraint(StdAtom type);
    }
    abstract class Constraint : IConstraint
    {
        public static readonly Constraint AnyObject = new AnyObjectConstraint();
        public static readonly Constraint Forbidden = new ForbiddenConstraint();
        public static readonly Constraint Structured = new StructuredConstraint();
        public static readonly Constraint Applicable = new ApplicableConstraint();
        public static readonly Constraint Boolean = new BooleanConstraint();

        public static Constraint OfType(StdAtom typeAtom) => new TypeConstraint(typeAtom);

        public static Constraint OfPrimType(PrimType primtype) => new PrimTypeConstraint(primtype);

        public static Constraint FromDecl(Context ctx, ZilObject pattern)
        {
            switch (pattern)
            {
                case ZilForm { First: ZilAtom head, Rest: var tail }:
                    Debug.Assert(tail != null);

                    switch (head.StdAtom)
                    {
                        case StdAtom.OR:
                            return tail
                                .Select(zo => FromDecl(ctx, zo))
                                .Aggregate(Forbidden, Disjunction.From);

                        case StdAtom.PRIMTYPE:
                            Debug.Assert(tail.First != null);
                            return OfPrimType(ctx.GetTypePrim((ZilAtom)tail.First));

                        case StdAtom.None:
                            break;

                        // XXX may need to combine this with a contents constraint
                        //default:
                        //    return OfType(head.StdAtom);
                    }
                    break;

                case ZilAtom { StdAtom: StdAtom.None }:
                    break;

                case ZilAtom atom:
                    return atom.StdAtom switch
                    {
                        StdAtom.ANY => AnyObject,
                        StdAtom.APPLICABLE => Applicable,
                        StdAtom.STRUCTURED => Structured,
                        _ => OfType(atom.StdAtom)
                    };
            }

            return new DeclConstraint(pattern);
        }

        public Constraint And(Constraint other)
        {
            return CompareTo(other) switch
            {
                CompareOutcome.Looser => other,
                CompareOutcome.Stricter => this,
                _ => Conjunction.From(this, other)
            };
        }

        public Constraint Or(Constraint other)
        {
            return CompareTo(other) switch
            {
                CompareOutcome.Looser => this,
                CompareOutcome.Stricter => other,
                _ => Disjunction.From(this, other)
            };
        }

        protected CompareOutcome? CompareTo(Constraint other) => CompareImpl(other) ?? Invert(other.CompareImpl(this));

        protected abstract CompareOutcome? CompareImpl(Constraint other);

        public abstract bool Allows(Context ctx, ZilObject arg);
        public abstract override string ToString();
        public abstract void Accept(IConstraintVisitor visitor);

        protected enum CompareOutcome
        {
            Looser,
            Equal,
            Stricter,
        }

        protected static CompareOutcome? Invert(CompareOutcome? co)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            return co switch
            {
                CompareOutcome.Looser => CompareOutcome.Stricter,
                CompareOutcome.Stricter => CompareOutcome.Looser,
                _ => co,
            };
        }

        static string EnglishList(IEnumerable<string> items, string connector)
        {
            var array = items.ToArray();

            return array.Length switch
            {
                1 => array[0],
                2 => array[0] + " " + connector + " " + array[1],
                _ => string.Join(", ", array.Take(array.Length - 1)) + ", " + connector + " " +
                     array[^1],
            };
        }

        class AnyObjectConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg) => true;

            protected override CompareOutcome? CompareImpl(Constraint other) =>
                other is AnyObjectConstraint ? CompareOutcome.Equal : CompareOutcome.Looser;

            public override string ToString() => "anything";

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitAnyObjectConstraint();
        }

        class BooleanConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg) => true;

            protected override CompareOutcome? CompareImpl(Constraint other) =>
                other switch
                {
                    AnyObjectConstraint _ => CompareOutcome.Stricter,
                    BooleanConstraint _ => CompareOutcome.Equal,
                    _ => CompareOutcome.Looser
                };

            public override string ToString() => "anything";

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitAnyObjectConstraint();
        }

        class ForbiddenConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg) => false;

            public override string ToString() => "nothing";

            protected override CompareOutcome? CompareImpl(Constraint other) =>
                other is ForbiddenConstraint ? CompareOutcome.Equal : CompareOutcome.Stricter;

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitForbiddenConstraint();
        }

        class TypeConstraint : Constraint
        {
            public StdAtom TypeAtom { get; }

            public TypeConstraint(StdAtom typeAtom)
            {
                TypeAtom = typeAtom;
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                if (other is TypeConstraint otherType && otherType.TypeAtom == TypeAtom)
                {
                    return CompareOutcome.Equal;
                }

                return null;
            }

            public override bool Allows(Context ctx, ZilObject arg) => arg.StdTypeAtom == TypeAtom;

            public override string ToString() => TypeAtom.ToString();

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitTypeConstraint(TypeAtom);
        }

        class PrimTypeConstraint : Constraint
        {
            PrimType PrimType { get; }

            public PrimTypeConstraint(PrimType primtype)
            {
                PrimType = primtype;
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                return other switch
                {
                    PrimTypeConstraint { PrimType: var pt } when pt == PrimType => CompareOutcome.Equal,
                    TypeConstraint { TypeAtom: var ta } when Context.GetTypePrim(ta) == PrimType => CompareOutcome.Looser,
                    _ => null,
                };
            }

            public override bool Allows(Context ctx, ZilObject arg) => arg.PrimType == PrimType;

            public override string ToString() => "PRIMTYPE " + PrimType.ToString();

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitPrimTypeConstraint(PrimType);
        }

        class StructuredConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg) => arg is IStructure;

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                return other switch
                {
                    StructuredConstraint _ => CompareOutcome.Equal,
                    TypeConstraint { TypeAtom: var ta } when Context.IsStructuredType(ta) => CompareOutcome.Looser,
                    _ => null,
                };
            }

            public override string ToString() => "structured value";

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitStructuredConstraint();
        }

        class ApplicableConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg) => arg.IsApplicable(ctx);

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                return other switch
                {
                    ApplicableConstraint _ => CompareOutcome.Equal,
                    TypeConstraint { TypeAtom: var ta } when Context.IsApplicableType(ta) => CompareOutcome.Looser,
                    _ => null,
                };
            }

            public override string ToString() => "applicable value";

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitApplicableConstraint();
        }

        class DeclConstraint : Constraint
        {
            ZilObject Pattern { get; }

            public DeclConstraint(ZilObject pattern)
            {
                Pattern = pattern;
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                return other switch
                {
                    DeclConstraint { Pattern: var p } when Pattern.StructurallyEquals(p) => CompareOutcome.Equal,
                    _ => null,
                };
            }

            public override bool Allows(Context ctx, ZilObject arg) => Decl.Check(ctx, arg, Pattern);

            public override string ToString() => Pattern.ToString();

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitDeclConstraint(Pattern);
        }

        class Conjunction : Constraint
        {
            IEnumerable<Constraint> Constraints { get; }

            Conjunction(IEnumerable<Constraint> constraints)
            {
                Constraints = constraints;
            }

            public static Conjunction From(Constraint left,
                 Constraint right)
            {
                var parts = new List<Constraint>();

                if (left is Conjunction lc)
                {
                    parts.AddRange(lc.Constraints);
                }
                else
                {
                    parts.Add(left);
                }

                IEnumerable<Constraint> todo;
                if (right is Conjunction rc)
                {
                    todo = rc.Constraints;
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
                        switch (c.CompareTo(p))
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
                        parts.RemoveAll(p => c.CompareTo(p) == CompareOutcome.Stricter);
                    }

                    parts.Add(c);
                }

                return new Conjunction(parts);
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                int count = 0, looser = 0, equal = 0;

                foreach (var c in Constraints)
                {
                    count++;

                    switch (c.CompareTo(other))
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
                return Constraints.All(c => c.Allows(ctx, arg));
            }

            public override string ToString()
            {
                return EnglishList(Constraints.Select(c => c.ToString()).OrderBy(s => s, StringComparer.Ordinal), "and");
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitConjunctionConstraint(Constraints);
        }

        class Disjunction : Constraint
        {
            IEnumerable<Constraint> Constraints { get; }

            Disjunction(IEnumerable<Constraint> constraints)
            {
                Constraints = constraints;
            }

            public static Disjunction From(Constraint left,
                Constraint right)
            {
                var parts = new List<Constraint>();

                if (left is Disjunction ld)
                {
                    parts.AddRange(ld.Constraints);
                }
                else
                {
                    parts.Add(left);
                }

                IEnumerable<Constraint> todo;
                if (right is Disjunction rd)
                {
                    todo = rd.Constraints;
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
                        switch (c.CompareTo(p))
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
                        parts.RemoveAll(p => c.CompareTo(p) == CompareOutcome.Looser);
                    }

                    parts.Add(c);
                }

                return new Disjunction(parts);
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                int count = 0, stricter = 0, equal = 0;

                foreach (var c in Constraints)
                {
                    count++;

                    switch (c.CompareTo(other))
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
                return Constraints.Any(c => c.Allows(ctx, arg));
            }

            public override string ToString()
            {
                return EnglishList(Constraints.Select(c => c.ToString()).OrderBy(s => s, StringComparer.Ordinal), "or");
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitDisjunctionConstraint(Constraints);
        }
    }
}
