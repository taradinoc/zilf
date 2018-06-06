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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Diagnostics;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Zilf.Language
{
    interface IProvideStructureForDeclCheck
    {
        [NotNull]
        IStructure GetStructureForDeclCheck([NotNull] Context ctx);
    }

    /// <summary>
    /// Raised when a user-defined DECL check fails.
    /// </summary>
    [Serializable]
    class DeclCheckError : InterpreterError
    {
        const int DiagnosticCode = InterpreterMessages.Expected_0_To_Match_DECL_1_But_Got_2;

        public DeclCheckError([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] ZilObject pattern,
            [NotNull] string usage)
            : base(DiagnosticCode, usage, pattern.ToStringContext(ctx, false), value.ToStringContext(ctx, false))
        {
        }

        public DeclCheckError([NotNull] IProvideSourceLine src, [NotNull] Context ctx, [NotNull] ZilObject value,
            [NotNull] ZilObject pattern, string usage)
            : base(src, DiagnosticCode, usage, pattern.ToStringContext(ctx, false), value.ToStringContext(ctx, false))
        {
        }

        [StringFormatMethod("usageFormat")]
        public DeclCheckError([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] ZilObject pattern,
            [NotNull] string usageFormat, [NotNull] object arg0)
            : this(ctx, value, pattern, string.Format(usageFormat, arg0))
        {
        }

        [StringFormatMethod("usageFormat")]
        public DeclCheckError([NotNull] IProvideSourceLine src, [NotNull] Context ctx, [NotNull] ZilObject value,
            [NotNull] ZilObject pattern, [NotNull] string usageFormat, [NotNull] object arg0)
            : this(src, ctx, value, pattern, string.Format(usageFormat, arg0))
        {
        }

        protected DeclCheckError([NotNull] SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }

    static class Decl
    {
        /// <exception cref="InterpreterError">The syntax is incorrect.</exception>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public static bool Check([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] ZilObject pattern,
            bool ignoreErrors = false)
        {
            switch (pattern)
            {
                case ZilAtom atom:
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (atom.StdAtom)
                    {
                        case StdAtom.ANY:
                            return true;

                        case StdAtom.APPLICABLE:
                            return value.IsApplicable(ctx);

                        case StdAtom.STRUCTURED:
                            return (value is IStructure);

                        case StdAtom.TUPLE:
                            // special case
                            return (value.StdTypeAtom == StdAtom.LIST);

                        default:
                            // arbitrary atoms can be type names...
                            if (ctx.IsRegisteredType(atom))
                            {
                                var typeAtom = value.GetTypeAtom(ctx);

                                if (typeAtom == atom)
                                    return true;

                                // special cases: a raw TABLE value can substitute for a TABLE-based type, or VECTOR
                                return typeAtom.StdAtom == StdAtom.TABLE &&
                                       (atom.StdAtom == StdAtom.VECTOR || ctx.GetTypePrim(atom) == PrimType.TABLE);
                            }

                            // ...or aliases
                            if (IsNonCircularAlias(ctx, atom, out var aliased))
                                return Check(ctx, value, aliased, ignoreErrors);

                            // special cases for GVAL and LVAL
                            // ReSharper disable once SwitchStatementMissingSomeCases
                            switch (atom.StdAtom)
                            {
                                case StdAtom.GVAL:
                                    return value.IsGVAL(out _);

                                case StdAtom.LVAL:
                                    return value.IsLVAL(out _);

                                default:
                                    return ignoreErrors
                                        ? false
                                        : throw new InterpreterError(
                                            InterpreterMessages.Unrecognized_0_1,
                                            "atom in DECL pattern",
                                            atom);
                            }
                    }

                case ZilSegment seg:
                    return CheckFormOrSegment(ctx, value, seg.Form, true, ignoreErrors);

                case ZilForm form:
                    return CheckFormOrSegment(ctx, value, form, false, ignoreErrors);

                default:
                    if (ignoreErrors)
                        return false;

                    throw new InterpreterError(
                        InterpreterMessages.Unrecognized_0_1,
                        "value in DECL pattern",
                        pattern.ToStringContext(ctx, false));
            }
        }

        static bool CheckFormOrSegment([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] ZilForm form,
            bool segment, bool ignoreErrors)
        {
            var (first, rest) = form;

            // special forms
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch ((first as ZilAtom)?.StdAtom)
            {
                case StdAtom.OR:
                    return rest.Any(subpattern => Check(ctx, value, subpattern, ignoreErrors));

                case StdAtom.QUOTE:
                    return rest.First?.StructurallyEquals(value) ?? false;

                case StdAtom.PRIMTYPE when rest.First is ZilAtom primType:
                    // special case for GVAL and LVAL, which can substitute for <PRIMTYPE ATOM>
                    return
                        value.PrimType == ctx.GetTypePrim(primType) ||
                        primType.StdAtom == StdAtom.ATOM &&
                        (value.IsGVAL(out _) || value.IsLVAL(out _));
            }

            // structure form: first pattern element is a DECL matched against the whole structure
            // (usually a type atom), remaining elements are matched against the structure elements
            if (first == null || !Check(ctx, value, first, ignoreErrors))
                return false;

            if (value is IStructure valueAsStructure)
            {
                // yay
            }
            else if (value is IProvideStructureForDeclCheck structProvider)
            {
                valueAsStructure = structProvider.GetStructureForDeclCheck(ctx);
            }
            else
            {
                return false;
            }

            return CheckElements(ctx, valueAsStructure, rest, segment, ignoreErrors);
        }

        [ContractAnnotation("=> true, decl: notnull; => false, decl: null")]
        static bool IsNonCircularAlias([NotNull] Context ctx, [NotNull] ZilAtom atom, out ZilObject decl)
        {
            var seen = new HashSet<ZilAtom>();
            var declAtom = ctx.GetStdAtom(StdAtom.DECL);
            ZilObject value;

            do
            {
                seen.Add(atom);

                value = ctx.GetProp(atom, declAtom);
                atom = value as ZilAtom;
            } while (atom != null && !seen.Contains(atom));

            if (atom != null)
            {
                // circular
                decl = null;
                return false;
            }

            // noncircular, or not an alias
            decl = value;
            return value != null;
        }

        static bool CheckElements([NotNull] Context ctx, [NotNull] IStructure structure,
            [NotNull] ZilListoidBase elements, bool segment, bool ignoreErrors)
        {
            foreach (var subpattern in elements)
            {
                ZilObject first;

                if (subpattern is ZilVector vector)
                {
                    var len = vector.GetLength();
                    if (len > 0 && vector[0] is ZilAtom atom)
                    {
                        int i;

                        // ReSharper disable once SwitchStatementMissingSomeCases
                        switch (atom.StdAtom)
                        {
                            case StdAtom.REST:
                                i = 1;
                                while (!structure.IsEmpty)
                                {
                                    first = structure.GetFirst();
                                    Debug.Assert(first != null);

                                    if (!Check(ctx, first, vector[i]))
                                        return false;

                                    i++;
                                    if (i >= len)
                                        i = 1;

                                    structure = structure.GetRest(1);
                                    Debug.Assert(structure != null);
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
                                    if (structure.IsEmpty)
                                        break;

                                    first = structure.GetFirst();
                                    Debug.Assert(first != null);

                                    if (!Check(ctx, first, vector[i]))
                                        break;

                                    structure = structure.GetRest(1);
                                    Debug.Assert(structure != null);
                                }

                                // move on to the next subpattern, if any
                                continue;
                        }
                    }
                    else if (len > 0 && vector[0] is ZilFix fix)
                    {
                        var count = fix.Value;

                        for (int i = 0; i < count; i++)
                        {
                            for (int j = 1; j < vector.GetLength(); j++)
                            {
                                if (structure.IsEmpty)
                                    return false;

                                first = structure.GetFirst();
                                Debug.Assert(first != null);

                                if (!Check(ctx, first, vector[j]))
                                    return false;

                                structure = structure.GetRest(1);
                                Debug.Assert(structure != null);
                            }
                        }

                        // move on to the next subpattern, if any
                        continue;
                    }

                    if (ignoreErrors)
                        return false;

                    throw new InterpreterError(
                        InterpreterMessages.Unrecognized_0_1,
                        "vector in DECL pattern",
                        vector.ToStringContext(ctx, false));
                }

                if (structure.IsEmpty)
                    return false;

                first = structure.GetFirst();
                Debug.Assert(first != null);

                if (!Check(ctx, first, subpattern))
                    return false;

                structure = structure.GetRest(1);
                Debug.Assert(structure != null);
            }

            return !segment || structure.IsEmpty;
        }
    }
}
