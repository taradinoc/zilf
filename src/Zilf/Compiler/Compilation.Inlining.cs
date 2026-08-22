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
            Dictionary<ZilAtom, int> ParameterUseCounts, HashSet<ZilAtom> ParametersReadAfterSideEffect,
            bool LegacyEligible);

        private const int MaximumInlineCandidateForms = 32;
        private const int MaximumInlineEstimateDepth = 3;
        private const int MaximumSpeedInlineSiteGrowth = 8;
        private const int MaximumSpeedInlineCallerGrowth = 64;

        private readonly Dictionary<ZilRoutine, ZilRoutine> rewrittenRoutines = [];

#if DEBUG
        private int inlineCalls;
        private int inlineCallsRejectedForLocalLimit;
        private int inlineCallsRejectedForSize;
        private int inlineCallsRejectedForSpeed;
        private int inlineCallsRejectedForCallerBudget;
        private int inlineCallsRejectedForUnsupportedCost;
        private int inlineCallsAcceptedWithConstantSpecialization;
        private int inlineEstimatedBytesSaved;
        private int inlineEstimatedBytesGrown;
        private int inlineEliminatedRoutines;
        private int inlineEstimatedCalleeBytesRemoved;
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
            if (Context.OptimizationLevel == 0 && !Context.OptimizeForSize ||
                Context.OptimizeForSize && Game is not IInliningCostModel)
            {
                _inlineRoutines = null;
                return;
            }

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
                var hasNestedForm = HasNestedInlineForm(body);
                var formCount = CountInlineForms(body);
                var legacyEligible = formCount <= 2 && (Context.OptimizationLevel >= 3 || !hasNestedForm);
                if (IsInlineExpression(body) &&
                    (Game is IInliningCostModel ? formCount <= MaximumInlineCandidateForms : legacyEligible))
                    candidates.Add(routine.Name, new InlineRoutine(body, parameters, hasNestedForm,
                        CountParameterUses(body, parameters), FindParametersReadAfterSideEffect(body, parameters),
                        legacyEligible));
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

        private int CountInlineForms(ZilObject expression)
        {
            expression = expression.Unwrap(Context);
            if (expression.IsVariableRef() || expression is not ZilForm form)
                return 0;
            return 1 + form.Skip(1).Sum(CountInlineForms);
        }

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

        private HashSet<ZilAtom> FindParametersReadAfterSideEffect(ZilObject expression,
            IReadOnlyList<ArgItem> parameters)
        {
            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var parameterAtoms = parameters.Select(parameter => parameter.Atom).ToHashSet(comparer);
            var result = new HashSet<ZilAtom>(comparer);
            var sideEffectOccurred = false;

            if (expression.Unwrap(Context) is ZilForm { Rest: { } arguments })
            {
                foreach (var argument in arguments)
                {
                    Visit(argument.Unwrap(Context), ref sideEffectOccurred);
                    if (HasSideEffects(argument))
                        sideEffectOccurred = true;
                }
            }

            return result;

            void Visit(ZilObject item, ref bool precedingSideEffect)
            {
                item = item.Unwrap(Context);
                if (item.IsLVAL(out var atom))
                {
                    if (precedingSideEffect && parameterAtoms.Contains(atom))
                        result.Add(atom);
                    return;
                }

                if (item is not ZilForm { Rest: { } children })
                    return;

                foreach (var child in children)
                {
                    Visit(child.Unwrap(Context), ref precedingSideEffect);
                    if (HasSideEffects(child))
                        precedingSideEffect = true;
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

            if (Game is IInliningCostModel costModel &&
                (Context.OptimizeForSize || !candidate.LegacyEligible))
            {
                if (!TryEvaluateInlineProfitability(costModel, name, candidate, arguments, wantResult,
                    predicateLabel != null, out var estimatedGrowth, out var constantSpecialization))
                {
                    activeInlineRoutines.Remove(name);
                    return false;
                }
                inlineCallerGrowth += Math.Max(0, estimatedGrowth);
#if DEBUG
                if (constantSpecialization)
                    inlineCallsAcceptedWithConstantSpecialization++;
                if (estimatedGrowth > 0)
                    inlineEstimatedBytesGrown += estimatedGrowth;
                else
                    inlineEstimatedBytesSaved -= estimatedGrowth;
#endif
            }
            else if (Game is not IInliningCostModel && !candidate.LegacyEligible)
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
                    if (IsWholeProgramInlineSite(name))
                        eliminatedInlineRoutines.Add(name);
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

        private void PlanWholeProgramInlineCandidates(HashSet<ZilAtom> externallyReferenced)
        {
            _wholeProgramInlineCallers = null;
            _wholeProgramInlineRemovalCredit = null;
            if (_inlineRoutines == null)
                return;

            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var callCounts = _inlineRoutines.Keys.ToDictionary(name => name, _ => 0, comparer);
            var callers = _inlineRoutines.Keys.ToDictionary(
                name => name, _ => new HashSet<ZilAtom>(comparer), comparer);
            var escaped = new HashSet<ZilAtom>(externallyReferenced, comparer);

            foreach (var original in Context.ZEnvironment.Routines)
            {
                if (original.Name is not { } routineName)
                    continue;
                var routine = GetRewrittenRoutine(original);
                foreach (var parameter in routine.ArgSpec)
                {
                    if (parameter.DefaultValue != null)
                        Visit(parameter.DefaultValue, routineName, false);
                }
                foreach (var expression in routine.Body)
                    Visit(expression, routineName, true);
            }

            var eligible = new HashSet<ZilAtom>(comparer);
            foreach (var pair in callCounts)
            {
                if (pair.Value > 0 && callers[pair.Key].Count == 1 && !escaped.Contains(pair.Key) &&
                    CountSyntacticCalls(pair.Key) == pair.Value)
                    eligible.Add(pair.Key);
            }
            var result = new Dictionary<ZilAtom, HashSet<ZilAtom>>(comparer);
            var removalCredit = new HashSet<ZilAtom>(comparer);
            foreach (var name in eligible)
            {
                var candidateCallers = callers[name];
                var allCallersScheduled = _routinesToCompile == null || candidateCallers.All(_routinesToCompile.Contains);
                if (allCallersScheduled && !candidateCallers.Overlaps(eligible))
                {
                    result.Add(name, candidateCallers);
                    if (callCounts[name] == 1)
                        removalCredit.Add(name);
                }
            }
            _wholeProgramInlineCallers = result;
            _wholeProgramInlineRemovalCredit = removalCredit;
            return;

            void Visit(ZilObject expression, ZilAtom caller, bool allowDirectCall)
            {
                expression = expression.Unwrap(Context);
                if (expression is ZilForm { First: ZilAtom head } form)
                {
                    if (allowDirectCall && TryResolveRoutineName(head, out var called) &&
                        callCounts.TryGetValue(called, out var count))
                    {
                        callCounts[called] = count + 1;
                        callers[called].Add(caller);
                    }
                    foreach (var argument in form.Skip(1))
                        Visit(argument, caller, true);
                    return;
                }
                if (expression is ZilRoutine { Name: not null } routine && callCounts.ContainsKey(routine.Name))
                {
                    escaped.Add(routine.Name);
                    return;
                }
                if (expression is ZilAtom atom && TryResolveRoutineName(atom, out var referenced) &&
                    callCounts.ContainsKey(referenced))
                {
                    escaped.Add(referenced);
                    return;
                }
                if (expression.IsVariableRef())
                    return;
                if (expression is IEnumerable<ZilObject> children)
                {
                    foreach (var child in children)
                        Visit(child, caller, true);
                }
            }

            bool TryResolveRoutineName(ZilAtom atom, out ZilAtom name)
            {
                var interned = Context.ZEnvironment.InternGlobalName(atom);
                var value = Context.GetZVal(interned);
                while (value is ZilConstant constant)
                    value = constant.Value;
                if (value is ZilRoutine { Name: not null } routine)
                {
                    name = routine.Name;
                    return true;
                }
                if (_inlineRoutines.ContainsKey(interned))
                {
                    name = interned;
                    return true;
                }
                name = null!;
                return false;
            }

            int CountSyntacticCalls(ZilAtom target)
            {
                var count = 0;
                foreach (var routine in Context.ZEnvironment.Routines)
                {
                    foreach (var expression in routine.Body)
                        Count(expression);
                }
                return count;

                void Count(ZilObject expression)
                {
                    if (expression is ZilForm { First: ZilAtom head } form)
                    {
                        if (comparer.Equals(Context.ZEnvironment.InternGlobalName(head), target))
                            count++;
                        CountList(form.Rest);
                        return;
                    }
                    if (expression is ZilListoidBase list)
                    {
                        CountList(list);
                        return;
                    }
                    if (expression is IEnumerable<ZilObject> children)
                    {
                        foreach (var child in children)
                            Count(child);
                    }
                }

                void CountList(ZilListoidBase? list)
                {
                    while (list is { IsEmpty: false, First: not null, Rest: not null })
                    {
                        Count(list.First);
                        list = list.Rest;
                    }
                }
            }
        }

        private readonly record struct InlineEstimate(InliningCost Cost, InliningOperandClass Operand,
            int? Constant = null, bool ConstantSpecialization = false);

        private bool TryEvaluateInlineProfitability(IInliningCostModel model, ZilAtom name, InlineRoutine candidate,
            ZilObject[] arguments, bool wantResult, bool predicateContext, out int growth,
            out bool constantSpecialization)
        {
            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var bindings = new Dictionary<ZilAtom, InlineEstimate>(comparer);
            var argumentCost = new InliningCost();
            var callOperands = new List<InliningOperandClass> { InliningOperandClass.UnresolvedConstant };
            constantSpecialization = false;

            for (var i = 0; i < candidate.Parameters.Count; i++)
            {
                var parameter = candidate.Parameters[i];
                var argument = i < arguments.Length ? arguments[i].Unwrap(Context) : parameter.DefaultValue;
                argument ??= ZilFix.Zero;
                if (!TryEstimateInlineExpression(model, argument,
                    new Dictionary<ZilAtom, InlineEstimate>(comparer), 0, true, false, out var estimate))
                {
#if DEBUG
                    inlineCallsRejectedForUnsupportedCost++;
#endif
                    growth = 0;
                    return false;
                }

                argumentCost += estimate.Cost;
                callOperands.Add(estimate.Operand);
                var bound = estimate with { Cost = new InliningCost() };
                if (i < arguments.Length && NeedsInlineTemporary(candidate, i, argument))
                {
                    // The snapshot replaces the call argument's stack/operand handoff. Charge its local operand
                    // through uses in the body, but do not charge a second complete operation here.
                    bound = bound with { Operand = InliningOperandClass.Local, Constant = null };
                }
                bindings[parameter.Atom] = bound;
            }

            if (!TryEstimateInlineExpression(model, candidate.Body, bindings, 0, wantResult, predicateContext,
                out var bodyEstimate))
            {
#if DEBUG
                inlineCallsRejectedForUnsupportedCost++;
#endif
                growth = 0;
                return false;
            }

            var callCost = argumentCost + model.EstimateOperation(InliningOperationClass.DirectCall,
                callOperands, wantResult, predicateContext);
            var inlineCost = argumentCost + bodyEstimate.Cost;
            growth = inlineCost.Bytes - callCost.Bytes;
            var removableBytes = 0;
            if (Context.OptimizeForSize && _wholeProgramInlineRemovalCredit?.Contains(name) == true &&
                IsWholeProgramInlineSite(name) &&
                TryEstimateRemovableRoutineCost(model, candidate, out var removableCost))
            {
                removableBytes = removableCost.Bytes;
                growth -= removableBytes;
            }
            constantSpecialization = bodyEstimate.ConstantSpecialization ||
                bindings.Values.Any(binding => binding.Constant != null);

            if (Context.OptimizeForSize)
            {
                if (growth < 0)
                {
#if DEBUG
                    inlineEstimatedCalleeBytesRemoved += removableBytes;
#endif
                    return true;
                }
#if DEBUG
                inlineCallsRejectedForSize++;
#endif
                return false;
            }

            if (Context.OptimizationLevel < 3)
            {
                if (growth <= 0)
                    return true;
#if DEBUG
                inlineCallsRejectedForSize++;
#endif
                return false;
            }

            if (inlineCost.Instructions >= callCost.Instructions)
            {
#if DEBUG
                inlineCallsRejectedForSpeed++;
#endif
                return false;
            }
            if (growth > MaximumSpeedInlineSiteGrowth)
            {
#if DEBUG
                inlineCallsRejectedForSize++;
#endif
                return false;
            }
            if (inlineCallerGrowth + Math.Max(0, growth) > MaximumSpeedInlineCallerGrowth)
            {
#if DEBUG
                inlineCallsRejectedForCallerBudget++;
#endif
                return false;
            }
            return true;
        }

        private bool IsWholeProgramInlineSite(ZilAtom name) =>
            compilingRoutineName != null && _wholeProgramInlineCallers != null &&
            _wholeProgramInlineCallers.TryGetValue(name, out var callers) &&
            callers.Contains(compilingRoutineName);

        private void RecordNonInlinedCall(ZilAtom name)
        {
            nonInlinedCallCounts.TryGetValue(name, out var count);
            nonInlinedCallCounts[name] = count + 1;
        }

        private bool TryEstimateRemovableRoutineCost(IInliningCostModel model, InlineRoutine candidate,
            out InliningCost cost)
        {
            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var bindings = candidate.Parameters.ToDictionary(parameter => parameter.Atom,
                _ => new InlineEstimate(new InliningCost(), InliningOperandClass.Local), comparer);
            if (!TryEstimateInlineExpression(model, candidate.Body, bindings, 0, true, false, out var body))
            {
                cost = new InliningCost();
                return false;
            }
            cost = body.Cost + model.EstimateOperation(InliningOperationClass.Return,
                [body.Operand], false, false);
            return true;
        }

        private bool TryEstimateInlineExpression(IInliningCostModel model, ZilObject expression,
            IReadOnlyDictionary<ZilAtom, InlineEstimate> bindings, int depth, bool wantResult,
            bool predicateContext, out InlineEstimate estimate)
        {
            expression = expression.Unwrap(Context);
            if (expression.IsLVAL(out var local))
            {
                if (bindings.TryGetValue(local, out var binding))
                    estimate = binding;
                else if (TryGetInlineLocal(local, out var inlineValue))
                    estimate = ClassifyOperand(inlineValue);
                else
                    estimate = new InlineEstimate(new InliningCost(), InliningOperandClass.Local);
                return true;
            }
            if (expression.IsGVAL(out var global))
            {
                var globalValue = Context.GetZVal(Context.ZEnvironment.InternGlobalName(global));
                while (globalValue is ZilConstant constant)
                    globalValue = constant.Value;
                if (globalValue is ZilFix globalFix)
                {
                    estimate = new InlineEstimate(new InliningCost(), ClassifyConstant(globalFix.Value),
                        globalFix.Value);
                    return true;
                }
                estimate = new InlineEstimate(new InliningCost(), InliningOperandClass.Global);
                return true;
            }
            if (expression is ZilFix fix)
            {
                estimate = new InlineEstimate(new InliningCost(), ClassifyConstant(fix.Value), fix.Value);
                return true;
            }
            if (expression is not ZilForm { First: ZilAtom head } form)
            {
                estimate = new InlineEstimate(new InliningCost(), InliningOperandClass.UnresolvedConstant);
                return expression is not ZilString;
            }

            var operands = new List<InliningOperandClass>();
            var cost = new InliningCost();
            var constants = new List<int>();
            var allConstant = true;
            var specialized = false;
            foreach (var argument in form.Skip(1))
            {
                if (!TryEstimateInlineExpression(model, argument, bindings, depth + 1, true, false,
                    out var argumentEstimate))
                {
                    estimate = default;
                    return false;
                }
                cost += argumentEstimate.Cost;
                operands.Add(argumentEstimate.Operand);
                specialized |= argumentEstimate.ConstantSpecialization;
                if (argumentEstimate.Constant is { } constant)
                    constants.Add(constant);
                else
                    allConstant = false;
            }

            if (allConstant && TryFoldInlineOperation(head.Text, constants, out var folded))
            {
                estimate = new InlineEstimate(cost, ClassifyConstant(folded), folded, true);
                return true;
            }

            var interned = Context.ZEnvironment.InternGlobalName(head);
            var calledValue = Context.GetZVal(interned);
            while (calledValue is ZilConstant calledConstant)
                calledValue = calledConstant.Value;
            if (depth < MaximumInlineEstimateDepth && calledValue is ZilRoutine { Name: not null } calledRoutine &&
                _inlineRoutines != null && _inlineRoutines.TryGetValue(calledRoutine.Name, out var nestedCandidate) &&
                TryEstimateNestedInline(model, nestedCandidate, form.Skip(1).ToArray(), bindings, depth + 1,
                    wantResult, predicateContext, cost, operands, out estimate))
                return true;

            var operation = ClassifyInlineOperation(head, form.Skip(1).Count());
            if (operation == null)
            {
                estimate = default;
                return false;
            }
            cost += model.EstimateOperation(operation.Value, operands, wantResult, predicateContext);
            estimate = new InlineEstimate(cost,
                predicateContext ? InliningOperandClass.SmallConstant : InliningOperandClass.Stack,
                ConstantSpecialization: specialized);
            return true;
        }

        private bool TryEstimateNestedInline(IInliningCostModel model, InlineRoutine candidate,
            ZilObject[] arguments, IReadOnlyDictionary<ZilAtom, InlineEstimate> outerBindings, int depth,
            bool wantResult, bool predicateContext, InliningCost argumentCost,
            IReadOnlyList<InliningOperandClass> argumentOperands, out InlineEstimate estimate)
        {
            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var bindings = new Dictionary<ZilAtom, InlineEstimate>(comparer);
            for (var i = 0; i < candidate.Parameters.Count; i++)
            {
                InlineEstimate argument;
                if (i < arguments.Length)
                {
                    if (!TryEstimateInlineExpression(model, arguments[i], outerBindings, depth, true, false,
                        out argument))
                    {
                        estimate = default;
                        return false;
                    }
                    argument = argument with { Cost = new InliningCost() };
                }
                else
                {
                    var defaultValue = candidate.Parameters[i].DefaultValue ?? ZilFix.Zero;
                    if (!TryEstimateInlineExpression(model, defaultValue, outerBindings, depth, true, false,
                        out argument))
                    {
                        estimate = default;
                        return false;
                    }
                    argument = argument with { Cost = new InliningCost() };
                }
                bindings[candidate.Parameters[i].Atom] = argument;
            }

            if (!TryEstimateInlineExpression(model, candidate.Body, bindings, depth, wantResult, predicateContext,
                out var body))
            {
                estimate = default;
                return false;
            }

            var callOperands = new List<InliningOperandClass> { InliningOperandClass.UnresolvedConstant };
            callOperands.AddRange(argumentOperands);
            var call = argumentCost + model.EstimateOperation(InliningOperationClass.DirectCall, callOperands,
                wantResult, predicateContext);
            var inline = argumentCost + body.Cost;
            var growth = inline.Bytes - call.Bytes;
            var profitable = Context.OptimizationLevel < 3
                ? growth <= 0
                : inline.Instructions < call.Instructions && growth <= MaximumSpeedInlineSiteGrowth;
            if (!profitable)
            {
                estimate = default;
                return false;
            }

            estimate = body with
            {
                Cost = inline,
                ConstantSpecialization = body.ConstantSpecialization || bindings.Values.Any(value => value.Constant != null),
            };
            return true;
        }

        private InliningOperationClass? ClassifyInlineOperation(ZilAtom head, int argumentCount)
        {
            var platform = ZBuiltins.GetCurrentBuiltinPlatform(Context.ZEnvironment.TargetPlatform);
            if (ZBuiltins.IsBuiltinPredCall(head.Text, Context.ZEnvironment.ZVersion, argumentCount, platform) ||
                ZBuiltins.IsBuiltinValuePredCall(head.Text, Context.ZEnvironment.ZVersion, argumentCount, platform))
                return InliningOperationClass.Predicate;
            if (ZBuiltins.IsBuiltinValueCall(head.Text, Context.ZEnvironment.ZVersion, argumentCount, platform))
                return head.Text is "GET" or "GETB" or "GETP" or "GETPT" or "NEXTP" or "FIRST?" or "NEXT?"
                    ? InliningOperationClass.MemoryRead
                    : InliningOperationClass.Arithmetic;
            if (ZBuiltins.IsBuiltinVoidCall(head.Text, Context.ZEnvironment.ZVersion, argumentCount, platform))
                return InliningOperationClass.MemoryWrite;

            var interned = Context.ZEnvironment.InternGlobalName(head);
            var value = Context.GetZVal(interned);
            while (value is ZilConstant constant)
                value = constant.Value;
            return value is ZilRoutine ? InliningOperationClass.DirectCall : null;
        }

        private static InliningOperandClass ClassifyConstant(int value) => value is >= 0 and <= byte.MaxValue
            ? InliningOperandClass.SmallConstant
            : InliningOperandClass.LargeConstant;

        private static InlineEstimate ClassifyOperand(IOperand operand) => operand switch
        {
            INumericOperand numeric => new InlineEstimate(new InliningCost(), ClassifyConstant(numeric.Value),
                numeric.Value),
            ILocalBuilder => new InlineEstimate(new InliningCost(), InliningOperandClass.Local),
            IGlobalBuilder => new InlineEstimate(new InliningCost(), InliningOperandClass.Global),
            IIndirectOperand => new InlineEstimate(new InliningCost(), InliningOperandClass.Indirect),
            IConstantOperand => new InlineEstimate(new InliningCost(), InliningOperandClass.UnresolvedConstant),
            _ => new InlineEstimate(new InliningCost(), InliningOperandClass.Stack),
        };

        private static bool TryFoldInlineOperation(string name, List<int> operands, out int result)
        {
            result = 0;
            if (operands.Count == 0)
                return false;
            long value = operands[0];
            try
            {
                switch (name)
                {
                    case "+":
                        value = operands.Aggregate(0L, static (current, operand) => current + operand);
                        break;
                    case "*":
                        value = operands.Aggregate(1L, static (current, operand) => current * operand);
                        break;
                    case "-":
                        for (var i = 1; i < operands.Count; i++)
                            value -= operands[i];
                        break;
                    case "/" when operands.Skip(1).All(static operand => operand != 0):
                        for (var i = 1; i < operands.Count; i++)
                            value /= operands[i];
                        break;
                    default:
                        return false;
                }
                result = (short)value;
                return true;
            }
            catch (OverflowException)
            {
                return false;
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
                argument.IsGVAL(out _) && candidate.ParametersReadAfterSideEffect.Contains(parameter.Atom);
        }

#if DEBUG
        private void RecordInliningStatistics()
        {
            Game.RecordCompilerOptimizationStatistic("calls inlined", inlineCalls);
            Game.RecordCompilerOptimizationStatistic("calls not inlined: local variable limit",
                inlineCallsRejectedForLocalLimit);
            Game.RecordCompilerOptimizationStatistic("calls not inlined: estimated size", inlineCallsRejectedForSize);
            Game.RecordCompilerOptimizationStatistic("calls not inlined: estimated speed", inlineCallsRejectedForSpeed);
            Game.RecordCompilerOptimizationStatistic("calls not inlined: caller growth budget",
                inlineCallsRejectedForCallerBudget);
            Game.RecordCompilerOptimizationStatistic("calls not inlined: unsupported cost", inlineCallsRejectedForUnsupportedCost);
            Game.RecordCompilerOptimizationStatistic("calls inlined: constant-specialized",
                inlineCallsAcceptedWithConstantSpecialization);
            Game.RecordCompilerOptimizationStatistic("inlining estimated bytes saved", inlineEstimatedBytesSaved);
            Game.RecordCompilerOptimizationStatistic("inlining estimated bytes grown", inlineEstimatedBytesGrown);
            Game.RecordCompilerOptimizationStatistic("routines removed by inlining", inlineEliminatedRoutines);
            Game.RecordCompilerOptimizationStatistic("inlining estimated callee bytes removed",
                inlineEstimatedCalleeBytesRemoved);
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
