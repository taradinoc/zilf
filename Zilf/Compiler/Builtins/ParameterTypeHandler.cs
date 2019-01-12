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
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Language.Signatures;

namespace Zilf.Compiler.Builtins
{
    abstract class ParameterTypeHandler
    {
        public abstract BuiltinArg Process([NotNull] Compilation cc, [NotNull] [InstantHandle] Action<string> error,
            [NotNull] ZilObject arg, [NotNull] ParameterInfo pi);
        public virtual bool IsVariable => false;

        /// <remarks>
        /// Does not need to handle optional or varargs.
        /// </remarks>
        [NotNull]
        public abstract SignaturePart ToSignaturePart([NotNull] ParameterInfo pi);

        [NotNull]
        public static readonly IReadOnlyDictionary<Type, ParameterTypeHandler> Handlers =
            new Dictionary<Type, ParameterTypeHandler>
            {
                { typeof(Block), new BlockHandler() },
                { typeof(int), new IntHandler() },
                { typeof(IOperand), new OperandHandler() },
                { typeof(IVariable), new VariableHandler() },
                { typeof(SoftGlobal), new VariableHandler() },
                { typeof(string), new StringHandler() },
                { typeof(ZilAtom), new AtomHandler() },
                { typeof(ZilObject), new ZilObjectHandler() },
            };

        static VariableRef? GetVariable([NotNull] Compilation cc, [NotNull] ZilObject expr, QuirksMode quirks = QuirksMode.None)
        {
            if (!(expr is ZilAtom atom) &&
                ((quirks & QuirksMode.Global) == 0 || !expr.IsGVAL(out atom)) &&
                ((quirks & QuirksMode.Local) == 0 || !expr.IsLVAL(out atom)))
            {
                return null;
            }

            if (quirks == QuirksMode.Global)
            {
                // prefer global over local
                if (cc.Globals.TryGetValue(atom, out var gb))
                    return new VariableRef(gb);
                if (cc.Locals.TryGetValue(atom, out var lbr))
                    return new VariableRef(lbr.LocalBuilder);
            }
            else
            {
                if (cc.Locals.TryGetValue(atom, out var lbr))
                    return new VariableRef(lbr.LocalBuilder);
                if (cc.Globals.TryGetValue(atom, out var gb))
                    return new VariableRef(gb);
            }

            if (cc.SoftGlobals.TryGetValue(atom, out var sg))
                return new VariableRef(sg);

            return null;
        }

        sealed class BlockHandler : ParameterTypeHandler
        {
            public override BuiltinArg Process(Compilation cc, Action<string> error, ZilObject arg, ParameterInfo pi)
            {
                // arg must be an LVAL reference
                if (arg.IsLVAL(out var atom))
                {
                    var block = cc.Blocks.FirstOrDefault(b => b.Name == atom);
                    if (block == null)
                    {
                        error("argument must be bound to a block");
                    }

                    return new BuiltinArg(BuiltinArgType.Operand, block);
                }

                error("argument must be a local variable reference");
                return new BuiltinArg(BuiltinArgType.Operand, null);
            }

            public override SignaturePart ToSignaturePart(ParameterInfo pi)
            {
                return SignatureBuilder.Constrained(
                    SignatureBuilder.Identifier(pi.Name),
                    Constraint.OfType(StdAtom.ACTIVATION));
            }
        }

        sealed class IntHandler : ParameterTypeHandler
        {
            public override BuiltinArg Process(Compilation cc, Action<string> error, ZilObject arg, ParameterInfo pi)
            {
                if (arg.StdTypeAtom != StdAtom.FIX)
                    error("argument must be a FIX");

                return new BuiltinArg(BuiltinArgType.Operand, ((ZilFix)arg).Value);
            }

            public override SignaturePart ToSignaturePart(ParameterInfo pi)
            {
                return SignatureBuilder.Constrained(
                    SignatureBuilder.Identifier(pi.Name),
                    Constraint.OfType(StdAtom.FIX));
            }
        }

        sealed class AtomHandler : ParameterTypeHandler
        {
            public override BuiltinArg Process(Compilation cc, Action<string> error, ZilObject arg, ParameterInfo pi)
            {
                if (arg.StdTypeAtom != StdAtom.ATOM)
                    error("argument must be an atom");

                return new BuiltinArg(BuiltinArgType.Operand, arg);
            }

            public override SignaturePart ToSignaturePart(ParameterInfo pi)
            {
                return SignatureBuilder.Constrained(
                    SignatureBuilder.Identifier(pi.Name),
                    Constraint.OfType(StdAtom.ATOM));
            }
        }

        sealed class ZilObjectHandler : ParameterTypeHandler
        {
            public override BuiltinArg Process(Compilation cc, Action<string> error, ZilObject arg, ParameterInfo pi)
            {
                return new BuiltinArg(BuiltinArgType.Operand, arg);
            }

            public override SignaturePart ToSignaturePart(ParameterInfo pi)
            {
                return SignatureBuilder.Identifier(pi.Name);
            }
        }

        sealed class OperandHandler : ParameterTypeHandler
        {
            public override BuiltinArg Process(Compilation cc, Action<string> error, ZilObject arg, ParameterInfo pi)
            {
                // if marked with [Variable], allow a variable reference and forbid a non-variable bare atom
                var varAttr = pi.GetCustomAttributes<VariableAttribute>().SingleOrDefault();
                if (varAttr != null)
                {
                    if (GetVariable(cc, arg, varAttr.QuirksMode) is VariableRef variable)
                    {
                        if (!variable.IsHard)
                        {
                            error("soft variable may not be used here");
                            return new BuiltinArg(BuiltinArgType.Operand, null);
                        }
                        else
                        {
                            return new BuiltinArg(BuiltinArgType.Operand, variable.Hard.Indirect);
                        }
                    }
                    else if (arg is ZilAtom)
                    {
                        error("bare atom argument must be a variable name");
                        return new BuiltinArg(BuiltinArgType.Operand, null);
                    }
                    else
                    {
                        return new BuiltinArg(BuiltinArgType.NeedsEval, arg);
                    }
                }
                else
                {
                    return new BuiltinArg(BuiltinArgType.NeedsEval, arg);
                }
            }

            public override SignaturePart ToSignaturePart(ParameterInfo pi)
            {
                return SignatureBuilder.Identifier(pi.Name);
            }
        }

        sealed class StringHandler : ParameterTypeHandler
        {
            public override BuiltinArg Process(Compilation cc, Action<string> error, ZilObject arg, ParameterInfo pi)
            {
                // arg must be a string
                if (!(arg is ZilString zstr))
                {
                    error("argument must be a literal string");
                    return new BuiltinArg(BuiltinArgType.Operand, null);
                }

                return new BuiltinArg(BuiltinArgType.Operand, Compilation.TranslateString(zstr, cc.Context));
            }

            public override SignaturePart ToSignaturePart(ParameterInfo pi)
            {
                return SignatureBuilder.Constrained(
                    SignatureBuilder.Identifier(pi.Name),
                    Constraint.OfType(StdAtom.STRING));
            }
        }

        sealed class VariableHandler : ParameterTypeHandler
        {
            public override bool IsVariable => true;

            public override BuiltinArg Process(Compilation cc, Action<string> error, ZilObject arg, ParameterInfo pi)
            {
                var quirks = pi.GetCustomAttributes<VariableAttribute>().Single().QuirksMode;

                // arg must be an atom, or <GVAL atom> or <LVAL atom> in quirks mode
                var atom = arg as ZilAtom;
                if (atom == null && !((quirks & QuirksMode.Global) != 0 && arg.IsGVAL(out atom) ||
                                      (quirks & QuirksMode.Local) != 0 && arg.IsLVAL(out atom)))
                {
                    error("argument must be a variable");
                    return new BuiltinArg(BuiltinArgType.Operand, null);
                }

                if (pi.ParameterType == typeof(IVariable))
                {
                    if (!cc.Locals.ContainsKey(atom) && !cc.Globals.ContainsKey(atom))
                        error("no such variable: " + atom);

                    var variableRef = GetVariable(cc, arg, quirks);
                    return new BuiltinArg(BuiltinArgType.Operand, variableRef?.Hard);
                }
                else // if (pi.ParameterType == typeof(SoftGlobal))
                {
                    if (!cc.SoftGlobals.ContainsKey(atom))
                        error("no such variable: " + atom);

                    var variableRef = GetVariable(cc, arg, quirks);
                    return new BuiltinArg(BuiltinArgType.Operand, variableRef?.Soft);
                }
            }

            public override SignaturePart ToSignaturePart(ParameterInfo pi)
            {
                var constraint = Constraint.OfType(StdAtom.ATOM);

                var quirks = pi.GetCustomAttributes<VariableAttribute>().Single().QuirksMode;

                if ((quirks & QuirksMode.Global) != 0)
                {
                    //XXX
                    constraint = constraint.Or(Constraint.OfType(StdAtom.GVAL));
                }

                if ((quirks & QuirksMode.Local) != 0)
                {
                    //XXX
                    constraint = constraint.Or(Constraint.OfType(StdAtom.LVAL));
                }

                return SignatureBuilder.Constrained(
                    SignatureBuilder.Identifier(pi.Name),
                    constraint);
            }
        }
    }
}
