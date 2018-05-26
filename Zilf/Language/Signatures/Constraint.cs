/* Copyright 2010-2018 Jesse McGrew
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
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Signatures
{
    interface IConstraint
    {
        bool Allows([NotNull] [ProvidesContext] Context ctx, [NotNull] ZilObject arg);
    }

    interface IConstraintVisitor
    {
        void VisitAnyObjectConstraint();
        void VisitApplicableConstraint();
        void VisitBooleanConstraint();
        void VisitConjunctionConstraint([ItemNotNull] [NotNull] [InstantHandle] IEnumerable<Constraint> parts);
        void VisitDeclConstraint([NotNull] ZilObject pattern);
        void VisitDisjunctionConstraint([ItemNotNull] [NotNull] [InstantHandle] IEnumerable<Constraint> alts);
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

        [NotNull]
        public static Constraint OfType(StdAtom typeAtom) => new TypeConstraint(typeAtom);

        [NotNull]
        public static Constraint OfPrimType(PrimType primtype) => new PrimTypeConstraint(primtype);

        [NotNull]
        public static Constraint FromDecl([NotNull] [ProvidesContext] Context ctx, [NotNull] ZilObject pattern)
        {
            if (pattern is ZilForm form)
            {
                if (form.First is ZilAtom head)
                {
                    Debug.Assert(form.Rest != null);

                    switch (head.StdAtom)
                    {
                        case StdAtom.OR:
                            return form.Rest
                                .Select(zo => FromDecl(ctx, zo))
                                .Aggregate(Forbidden, (a, b) => Disjunction.From(a, b));

                        case StdAtom.PRIMTYPE:
                            Debug.Assert(form.Rest.First != null);
                            return OfPrimType(ctx.GetTypePrim((ZilAtom)form.Rest.First));

                        case StdAtom.None:
                            break;

                        // XXX may need to combine this with a contents constraint
                        //default:
                        //    return OfType(head.StdAtom);
                    }
                }
            }

            if (pattern is ZilAtom atom)
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

        [NotNull]
        public Constraint And([NotNull] Constraint other)
        {
            switch (CompareImpl(other) ?? Invert(other.CompareImpl(this)))
            {
                case CompareOutcome.Looser:
                    return other;

                case CompareOutcome.Stricter:
                    return this;

                default:
                    return Conjunction.From(this, other);
            }
        }

        [NotNull]
        public Constraint Or([NotNull] Constraint other)
        {
            switch (CompareImpl(other) ?? Invert(other.CompareImpl(this)))
            {
                case CompareOutcome.Looser:
                    return this;

                case CompareOutcome.Stricter:
                    return other;

                default:
                    return Disjunction.From(this, other);
            }
        }

        protected CompareOutcome? CompareTo([NotNull] Constraint other)
        {
            return CompareImpl(other) ?? Invert(other.CompareImpl(this));
        }

        protected abstract CompareOutcome? CompareImpl([NotNull] Constraint other);

        public abstract bool Allows([ProvidesContext] Context ctx, ZilObject arg);
        public abstract override string ToString();
        public abstract void Accept([NotNull] IConstraintVisitor visitor);

        protected enum CompareOutcome
        {
            Looser,
            Equal,
            Stricter,
        }

        protected static CompareOutcome? Invert(CompareOutcome? co)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
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

        [NotNull]
        static string EnglishList([ItemNotNull] [NotNull] IEnumerable<string> items, [NotNull] string connector)
        {
            var array = items.ToArray();

            switch (array.Length)
            {
                case 1:
                    return array[0];

                case 2:
                    return array[0] + " " + connector + " " + array[1];

                default:
                    return string.Join(", ", array.Take(array.Length - 1)) + ", " + connector + " " +
                           array[array.Length - 1];
            }
        }

        class AnyObjectConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg)
            {
                return true;
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                return other is AnyObjectConstraint ? CompareOutcome.Equal : CompareOutcome.Looser;
            }

            public override string ToString()
            {
                return "anything";
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitAnyObjectConstraint();
        }

        class BooleanConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg)
            {
                return true;
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                return other is AnyObjectConstraint ? CompareOutcome.Stricter
                    : other is BooleanConstraint ? CompareOutcome.Equal
                    : CompareOutcome.Looser;
            }

            public override string ToString()
            {
                return "anything";
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitAnyObjectConstraint();
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

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                return other is ForbiddenConstraint ? CompareOutcome.Equal : CompareOutcome.Stricter;
            }

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

            public override bool Allows(Context ctx, ZilObject arg)
            {
                return arg.StdTypeAtom == TypeAtom;
            }

            public override string ToString()
            {
                return TypeAtom.ToString();
            }

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
                switch (other)
                {
                    case PrimTypeConstraint otherPrimType when otherPrimType.PrimType == PrimType:
                        return CompareOutcome.Equal;

                    case TypeConstraint otherType when Context.GetTypePrim(otherType.TypeAtom) == PrimType:
                        return CompareOutcome.Looser;

                    default:
                        return null;
                }
            }

            public override bool Allows(Context ctx, ZilObject arg)
            {
                return arg.PrimType == PrimType;
            }

            public override string ToString()
            {
                return "PRIMTYPE " + PrimType.ToString();
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitPrimTypeConstraint(PrimType);
        }

        class StructuredConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg)
            {
                return arg is IStructure;
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                switch (other)
                {
                    case StructuredConstraint _:
                        return CompareOutcome.Equal;

                    case TypeConstraint otherType when Context.IsStructuredType(otherType.TypeAtom):
                        return CompareOutcome.Looser;

                    default:
                        return null;
                }
            }

            public override string ToString()
            {
                return "structured value";
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitStructuredConstraint();
        }

        class ApplicableConstraint : Constraint
        {
            public override bool Allows(Context ctx, ZilObject arg)
            {
                return arg.IsApplicable(ctx);
            }

            protected override CompareOutcome? CompareImpl(Constraint other)
            {
                switch (other)
                {
                    case ApplicableConstraint _:
                        return CompareOutcome.Equal;

                    case TypeConstraint otherType when Context.IsApplicableType(otherType.TypeAtom):
                        return CompareOutcome.Looser;

                    default:
                        return null;
                }
            }

            public override string ToString()
            {
                return "applicable value";
            }

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
                switch (other)
                {
                    case DeclConstraint otherDecl when Pattern.StructurallyEquals(otherDecl.Pattern):
                        return CompareOutcome.Equal;

                    default:
                        return null;
                }
            }

            public override bool Allows(Context ctx, ZilObject arg)
            {
                return Decl.Check(ctx, arg, Pattern);
            }

            public override string ToString()
            {
                return Pattern.ToString();
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitDeclConstraint(Pattern);
        }

        class Conjunction : Constraint
        {
            IEnumerable<Constraint> Constraints { get; }

            Conjunction(IEnumerable<Constraint> constraints)
            {
                Constraints = constraints;
            }

            [NotNull]
            public static Conjunction From([NotNull] Constraint left,
                [NotNull] Constraint right)
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
                return EnglishList(Constraints.Select(c => c.ToString()).OrderBy(s => s), "and");
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

            [NotNull]
            public static Disjunction From([NotNull] Constraint left,
                [NotNull] Constraint right)
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
                return EnglishList(Constraints.Select(c => c.ToString()).OrderBy(s => s), "or");
            }

            public override void Accept(IConstraintVisitor visitor) => visitor.VisitDisjunctionConstraint(Constraints);
        }
    }
}
