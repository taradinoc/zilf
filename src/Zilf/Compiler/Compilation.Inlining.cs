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
using System.Linq;
using Zilf.Compiler.Builtins;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;

namespace Zilf.Compiler
{
    sealed partial class Compilation
    {
        private sealed record InlineRoutine(ZilObject Body, IReadOnlyList<ArgItem> Parameters, bool HasNestedForm,
            Dictionary<ZilAtom, int> ParameterUseCounts);

        private readonly Dictionary<ZilRoutine, ZilRoutine> rewrittenRoutines = [];

#if DEBUG
        private int inlineCalls;
        private int inlineCallsRejectedForLocalLimit;
#endif

        private ZilRoutine GetRewrittenRoutine(ZilRoutine routine)
        {
            if (!rewrittenRoutines.TryGetValue(routine, out var rewritten))
            {
                rewritten = MaybeRewriteRoutine(Context, routine);
                rewrittenRoutines.Add(routine, rewritten);
            }

            return rewritten;
        }

        private void PrepareInlineRoutines()
        {
            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var candidates = new Dictionary<ZilAtom, InlineRoutine>(comparer);

            foreach (var original in Context.ZEnvironment.Routines)
            {
                var routine = GetRewrittenRoutine(original);
                if (routine.Name == null || routine.Name == Context.ZEnvironment.EntryRoutineName ||
                    Context.TraceRoutines || routine.ActivationAtom != null || routine.BodyLength != 1)
                    continue;

                var parameters = routine.ArgSpec.ToArray();
                if (parameters.Any(parameter => parameter.Type == ArgItem.ArgType.Auxiliary || parameter.Quoted ||
                    parameter.DefaultValue is ZilForm))
                    continue;

                var body = routine.Body.Single().Unwrap(Context);
                if (parameters.Any(parameter => body.ModifiesLocal(parameter.Atom)) || ContainsString(body))
                    continue;
                if (IsInlineExpression(body))
                    candidates.Add(routine.Name, new InlineRoutine(body, parameters, HasNestedInlineForm(body),
                        CountParameterUses(body, parameters)));
            }

            var recursive = FindRecursiveInlineRoutines(candidates);
            foreach (var name in recursive)
                candidates.Remove(name);
            _inlineRoutines = candidates;
        }

        private bool IsInlineExpression(ZilObject expression)
        {
            if (expression.IsVariableRef())
                return true;
            if (expression is not ZilForm)
                return true;
            if (expression is not ZilForm { First: ZilAtom head, Rest: { } rest })
                return false;
            if (head.StdAtom is StdAtom.BIND or StdAtom.PROG or StdAtom.REPEAT)
                return false;
            if (head.Text is "AGAIN" or "ASSIGNED?" or "PRINTR" or "QUIT" or "RETURN" or "RFALSE" or
                "RFATAL" or "RSTACK" or "RTRUE")
                return false;

            var arguments = rest.Select(argument => argument.Unwrap(Context)).ToArray();
            if (arguments.Any(argument => argument is ZilListBase and not ZilForm))
                return false;
            var nested = arguments.Where(argument => argument.IsNonVariableForm()).ToArray();
            if (nested.Length > 1 || nested.Any(argument => !IsInlineExpression(argument)))
                return false;

            var platform = ZBuiltins.GetCurrentBuiltinPlatform(Context.ZEnvironment.TargetPlatform);
            if (ZBuiltins.IsBuiltinValueCall(head.Text, Context.ZEnvironment.ZVersion, arguments.Length, platform) ||
                ZBuiltins.IsBuiltinValuePredCall(head.Text, Context.ZEnvironment.ZVersion, arguments.Length,
                    platform) ||
                ZBuiltins.IsBuiltinPredCall(head.Text, Context.ZEnvironment.ZVersion, arguments.Length, platform) ||
                ZBuiltins.IsBuiltinVoidCall(head.Text, Context.ZEnvironment.ZVersion, arguments.Length, platform))
                return true;

            var interned = Context.ZEnvironment.InternGlobalName(head);
            var value = Context.GetZVal(interned);
            while (value is ZilConstant constant)
                value = constant.Value;
            return value is ZilRoutine;
        }

        private bool HasNestedInlineForm(ZilObject expression) =>
            expression is ZilForm { Rest: { } arguments } &&
            arguments.Select(argument => argument.Unwrap(Context)).Any(argument => argument.IsNonVariableForm());

        private static bool ContainsString(ZilObject expression) => expression is ZilString ||
            expression is IEnumerable<ZilObject> sequence && sequence.Any(ContainsString);

        private Dictionary<ZilAtom, int> CountParameterUses(ZilObject expression,
            IReadOnlyList<ArgItem> parameters)
        {
            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var result = parameters.ToDictionary(parameter => parameter.Atom, _ => 0, comparer);
            Count(expression.Unwrap(Context));
            return result;

            void Count(ZilObject item)
            {
                item = item.Unwrap(Context);
                if (item.IsLVAL(out var atom) && result.TryGetValue(atom, out var uses))
                    result[atom] = uses + 1;
                if (item is IEnumerable<ZilObject> children)
                {
                    foreach (var child in children)
                        Count(child);
                }
            }
        }

        private HashSet<ZilAtom> FindRecursiveInlineRoutines(IReadOnlyDictionary<ZilAtom, InlineRoutine> candidates)
        {
            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var recursive = new HashSet<ZilAtom>(comparer);
            var visiting = new HashSet<ZilAtom>(comparer);
            var visited = new HashSet<ZilAtom>(comparer);
            var path = new Stack<ZilAtom>();

            foreach (var name in candidates.Keys)
                Visit(name);
            return recursive;

            void Visit(ZilAtom name)
            {
                if (visited.Contains(name))
                    return;
                if (!visiting.Add(name))
                {
                    foreach (var item in path)
                    {
                        recursive.Add(item);
                        if (comparer.Equals(item, name))
                            break;
                    }
                    return;
                }

                path.Push(name);
                foreach (var called in GetDirectRoutineCalls(candidates[name].Body))
                {
                    if (candidates.ContainsKey(called))
                        Visit(called);
                }
                path.Pop();
                visiting.Remove(name);
                visited.Add(name);
            }
        }

        private IEnumerable<ZilAtom> GetDirectRoutineCalls(ZilObject expression)
        {
            expression = expression.Unwrap(Context);
            if (expression is not ZilForm { First: ZilAtom head } form)
                yield break;

            var interned = Context.ZEnvironment.InternGlobalName(head);
            var value = Context.GetZVal(interned);
            while (value is ZilConstant constant)
                value = constant.Value;
            if (value is ZilRoutine { Name: not null } routine)
                yield return routine.Name;

            foreach (var child in form.Skip(1))
            {
                foreach (var called in GetDirectRoutineCalls(child))
                    yield return called;
            }
        }

        private bool TryCompileInlineCall(IRoutineBuilder rb, ZilAtom name, ZilObject[] arguments,
            bool wantResult, IVariable? resultStorage, ISourceLine sourceLine, out IOperand? result)
            => TryCompileInlineCall(rb, name, arguments, wantResult, resultStorage, sourceLine, null, false,
                out result);

        private bool TryCompileInlineCondition(IRoutineBuilder rb, ZilAtom name, ZilObject[] arguments,
            ISourceLine sourceLine, ILabel label, bool polarity) =>
            TryCompileInlineCall(rb, name, arguments, false, null, sourceLine, label, polarity, out _);

        private bool TryCompileInlineCall(IRoutineBuilder rb, ZilAtom name, ZilObject[] arguments,
            bool wantResult, IVariable? resultStorage, ISourceLine sourceLine, ILabel? predicateLabel,
            bool predicatePolarity, out IOperand? result)
        {
            result = null;
            if (_inlineRoutines == null || !_inlineRoutines.TryGetValue(name, out var candidate))
                return false;

            var minimumArguments = candidate.Parameters.Count(parameter =>
                parameter.Type == ArgItem.ArgType.Required);
            var maximumArguments = candidate.Parameters.Count(parameter =>
                parameter.Type != ArgItem.ArgType.Auxiliary);
            if (arguments.Length < minimumArguments || arguments.Length > maximumArguments)
                return false;

            if (!HasInlineTemporaryCapacity(candidate, arguments))
            {
#if DEBUG
                inlineCallsRejectedForLocalLimit++;
#endif
                return false;
            }

            if (!activeInlineRoutines.Add(name))
                return false;

            if (compilingEntryPoint && candidate.Body is ZilForm)
            {
                activeInlineRoutines.Remove(name);
                return false;
            }

            try
            {
                var bindings = new List<ZilAtom>();
                var temporaryBindings = new List<ZilAtom>();
                try
                {
                    var preallocated = new Dictionary<int, ILocalBuilder>();
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        var argument = arguments[i].Unwrap(Context);
                        if (!NeedsInlineTemporary(candidate, i, argument))
                            continue;
                        var temporaryAtom = ZilAtom.Parse("?TMP", Context);
                        preallocated.Add(i, PushInnerLocal(rb, temporaryAtom,
                            LocalBindingType.CompilerTemporary, sourceLine));
                        temporaryBindings.Add(temporaryAtom);
                    }

                    using var operands = CompileOperands(rb, sourceLine, arguments);
                    var supplied = operands.AsArray();
                    foreach (var pair in preallocated)
                    {
                        rb.EmitStore(pair.Value, supplied[pair.Key]);
                        supplied[pair.Key] = pair.Value;
                    }

                    var suppliedIndex = 0;
                    foreach (var parameter in candidate.Parameters)
                    {
                        IOperand value;
                        if (parameter.Type != ArgItem.ArgType.Auxiliary && suppliedIndex < supplied.Length)
                        {
                            value = supplied[suppliedIndex++];
                        }
                        else if (parameter.DefaultValue != null)
                            value = CompileConstant(parameter.DefaultValue, AmbiguousConstantMode.Pessimistic)!;
                        else
                            value = Game.Zero;

                        PushInlineLocal(parameter.Atom, value);
                        bindings.Add(parameter.Atom);
                    }

                    if (predicateLabel != null)
                    {
                        CompileCondition(rb, candidate.Body, sourceLine, predicateLabel, predicatePolarity);
                        result = null;
                    }
                    else
                    {
                        result = candidate.Body is ZilForm form
                            ? CompileForm(rb, form, wantResult, resultStorage)
                            : wantResult ? CompileAsOperand(rb, candidate.Body, sourceLine, resultStorage) : null;
                    }
#if DEBUG
                    inlineCalls++;
#endif
                    return true;
                }
                finally
                {
                    for (var i = bindings.Count - 1; i >= 0; i--)
                        PopInlineLocal(bindings[i]);
                    for (var i = temporaryBindings.Count - 1; i >= 0; i--)
                        PopInnerLocal(temporaryBindings[i]);
                }
            }
            finally
            {
                activeInlineRoutines.Remove(name);
            }
        }

        private bool HasInlineTemporaryCapacity(InlineRoutine candidate, ZilObject[] arguments)
        {
            // A nested operand is compiled before the outer operation and therefore needs a local of its own.
            var required = candidate.HasNestedForm ? 1 : 0;
            for (var i = 0; i < arguments.Length && i < candidate.Parameters.Count; i++)
            {
                var argument = arguments[i].Unwrap(Context);
                if (NeedsInlineTemporary(candidate, i, argument))
                    required++;
            }

            var maximum = Context.ZEnvironment.MaxRoutineLocals;
            if (maximum == int.MaxValue)
                return true;
            var used = AllLocalBindingRecords.Select(binding => binding.LocalBuilder).Distinct().Count();
            var available = maximum - used + SpareLocals.Count;
            return required <= available;
        }

        private static bool NeedsInlineTemporary(InlineRoutine candidate, int argumentIndex, ZilObject argument)
        {
            var parameter = candidate.Parameters[argumentIndex];
            var uses = candidate.ParameterUseCounts.GetValueOrDefault(parameter.Atom);
            return argument is ZilForm && !argument.IsVariableRef() && (candidate.HasNestedForm || uses != 1) ||
                candidate.HasNestedForm && argument.IsGVAL(out _);
        }

#if DEBUG
        private void RecordInliningStatistics()
        {
            Game.RecordCompilerOptimizationStatistic("calls inlined", inlineCalls);
            Game.RecordCompilerOptimizationStatistic("calls not inlined: local variable limit",
                inlineCallsRejectedForLocalLimit);
        }
#endif

        private void PushInlineLocal(ZilAtom atom, IOperand value)
        {
            if (!inlineLocalBindings.TryGetValue(atom, out var bindings))
            {
                bindings = new Stack<IOperand>();
                inlineLocalBindings.Add(atom, bindings);
            }
            bindings.Push(value);
        }

        private void PopInlineLocal(ZilAtom atom)
        {
            var bindings = inlineLocalBindings[atom];
            bindings.Pop();
            if (bindings.Count == 0)
                inlineLocalBindings.Remove(atom);
        }

        internal bool TryGetInlineLocal(ZilAtom atom, out IOperand value)
        {
            if (inlineLocalBindings.TryGetValue(atom, out var bindings) && bindings.TryPeek(out value!))
                return true;
            value = null!;
            return false;
        }
    }
}
