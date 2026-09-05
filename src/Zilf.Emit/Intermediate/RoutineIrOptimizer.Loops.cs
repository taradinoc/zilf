/* Copyright 2010-2026 Tara McGrew
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

namespace Zilf.Emit.Intermediate
{
    internal sealed partial class RoutineIrOptimizer
    {
        /// <summary>
        /// Inserts a preheader block before each natural loop that lacks one, rerouting the loop's external
        /// predecessor edges and merging their phi contributions in the new preheader.
        /// </summary>
        /// <param name="routine">The routine to transform.</param>
        private void CreateLoopPreheaders(RoutineIr routine)
        {
            if (createPreheader == null)
                return;
            foreach (var loop in CfgAnalysis.Create(routine).Loops.Where(loop => loop.Preheader == null)
                .OrderBy(loop => loop.Blocks.Count).ToArray())
            {
                var external = loop.Header.Predecessors.Where(predecessor => !loop.Blocks.Contains(predecessor))
                    .ToArray();
                if (external.Length == 0 || external.Any(predecessor => !CanRedirectEdge(predecessor, loop.Header)))
                {
                    Record("Preheaders rejected: unredirectable edge");
                    continue;
                }

                var preheader = createPreheader(loop.Header);
                foreach (var predecessor in external)
                    RedirectEdge(predecessor, loop.Header, preheader);
                foreach (var instruction in loop.Header.Instructions.Where(instruction =>
                    instruction.Opcode == IrOpcode.Phi && instruction.Payload is IrPhi))
                {
                    var phi = (IrPhi)instruction.Payload!;
                    var incoming = external.Select(predecessor => phi.Incoming[predecessor]).ToArray();
                    IrValue merged;
                    if (incoming.Skip(1).All(value => StateValuesEquivalent(value, incoming[0])))
                        merged = incoming[0];
                    else
                    {
                        var preheaderPhi = new IrInstruction(IrOpcode.Phi, routine.CreateValue(), [],
                            payload: new IrPhi(phi.Variable));
                        preheaderPhi.Result!.PhysicalHome = phi.Variable;
                        var payload = (IrPhi)preheaderPhi.Payload!;
                        foreach (var predecessor in external)
                            payload.Incoming[predecessor] = phi.Incoming[predecessor];
                        preheader.Instructions.Insert(0, preheaderPhi);
                        merged = preheaderPhi.Result;
                    }
                    foreach (var predecessor in external)
                        phi.Incoming.Remove(predecessor);
                    phi.Incoming[preheader] = merged;
                }
                routine.RebuildPredecessors();
                Record("Preheaders created");
            }
        }

        /// <summary>
        /// Determines whether an edge from the given predecessor to the target can be redirected, based on the
        /// predecessor's terminator.
        /// </summary>
        /// <param name="predecessor">The source block of the edge.</param>
        /// <param name="target">The current target block.</param>
        /// <returns><see langword="true"/> if the edge can be redirected; otherwise, <see langword="false"/>.</returns>
        private static bool CanRedirectEdge(IrBlock predecessor, IrBlock target) => predecessor.Terminator switch
        {
            IrTerminator.Jump jump when ReferenceEquals(jump.Target, target) =>
                jump.Instruction == null || jump.EmitJump != null,
            IrTerminator.Branch branch when ReferenceEquals(branch.WhenTrue, target) ||
                ReferenceEquals(branch.WhenFalse, target) => branch.Instruction == null ||
                !ReferenceEquals(branch.ExplicitTarget, target),
            _ => false,
        };

        /// <summary>
        /// Redirects a predecessor's edge from the old target to the new target.
        /// </summary>
        /// <param name="predecessor">The source block of the edge.</param>
        /// <param name="oldTarget">The current target block.</param>
        /// <param name="newTarget">The block to target instead.</param>
        /// <exception cref="InvalidOperationException">Thrown when the predecessor has no edge to the old target.</exception>
        private static void RedirectEdge(IrBlock predecessor, IrBlock oldTarget, IrBlock newTarget)
        {
            predecessor.Terminator = predecessor.Terminator switch
            {
                IrTerminator.Jump jump when ReferenceEquals(jump.Target, oldTarget) =>
                    RedirectJump(jump, newTarget),
                IrTerminator.Branch branch => branch with
                {
                    WhenTrue = ReferenceEquals(branch.WhenTrue, oldTarget) ? newTarget : branch.WhenTrue,
                    WhenFalse = ReferenceEquals(branch.WhenFalse, oldTarget) ? newTarget : branch.WhenFalse,
                },
                _ => throw new InvalidOperationException($"Block {predecessor} has no edge to {oldTarget}."),
            };
        }

        /// <summary>
        /// Updates a jump terminator to target the new block, rewrapping its lowering operation so the emitted
        /// jump targets the same block.
        /// </summary>
        /// <param name="jump">The jump terminator to redirect.</param>
        /// <param name="newTarget">The block to jump to instead.</param>
        /// <returns>The redirected jump.</returns>
        private static IrTerminator.Jump RedirectJump(IrTerminator.Jump jump, IrBlock newTarget)
        {
            if (jump.Instruction?.Payload is IrLoweringOperation lowering && jump.EmitJump != null)
                jump.Instruction.Payload = new IrLoweringOperation(_ => jump.EmitJump(newTarget),
                    lowering.ResultHome, lowering.IsStackResult, lowering.EmitTo);
            return jump with { Target = newTarget };
        }

        /// <summary>
        /// Removes redundant induction variables within each loop by rewriting uses of duplicate phis and updates
        /// to a canonical equivalent.
        /// </summary>
        /// <param name="routine">The routine to transform.</param>
        /// <param name="cfg">The control-flow analysis of the routine.</param>
        private void SimplifyInductionVariables(RoutineIr routine, CfgAnalysis cfg)
        {
            var definitions = routine.Blocks.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Result != null).ToDictionary(instruction => instruction.Result!);
            var replacements = new Dictionary<IrValue, IrValue>();
            foreach (var loop in cfg.Loops.Where(loop => loop.Preheader != null && loop.Latches.Count == 1))
            {
                var latch = loop.Latches.Single();
                var inductions = new List<(IrInstruction Phi, IrInstruction Update, IrValue Initial, int Step)>();
                foreach (var phi in loop.Header.Instructions.Where(instruction =>
                    instruction.Opcode == IrOpcode.Phi && instruction.Payload is IrPhi))
                {
                    var payload = (IrPhi)phi.Payload!;
                    if (!payload.Incoming.TryGetValue(loop.Preheader!, out var initial) ||
                        !payload.Incoming.TryGetValue(latch, out var next) || !definitions.TryGetValue(next, out var update) ||
                        !TryGetInductionStep(phi.Result!, update, out var step) ||
                        update.Payload is IrLoweringOperation { RequiredHome: true })
                        continue;
                    inductions.Add((phi, update, initial, step));
                }

                foreach (var group in inductions.GroupBy(item => item.Step))
                {
                    var items = group.ToArray();
                    for (var index = 1; index < items.Length; index++)
                    {
                        var redundant = items[index];
                        var canonical = items.Take(index).FirstOrDefault(candidate =>
                            StateValuesEquivalent(candidate.Initial, redundant.Initial));
                        if (canonical.Phi == null)
                            continue;
                        replacements[redundant.Phi.Result!] = canonical.Phi.Result!;
                        replacements[redundant.Update.Result!] = canonical.Update.Result!;
                        loop.Header.Instructions.Remove(redundant.Phi);
                        foreach (var block in loop.Blocks)
                        {
                            for (var i = block.Instructions.Count - 1; i >= 0; i--)
                            {
                                if (ReferenceEquals(block.Instructions[i], redundant.Update) ||
                                    block.Instructions[i].Payload is IrLoweringOperation
                                    {
                                        IsMaterialization: true,
                                        ResultHome: not null,
                                    } materialization && ReferenceEquals(materialization.ResultHome,
                                        ((IrPhi)redundant.Phi.Payload!).Variable))
                                    block.Instructions.RemoveAt(i);
                            }
                        }
                        Record("Redundant induction variables removed");
                    }
                }
            }
            ReplaceValues(routine, replacements);
            routine.RebuildPredecessors();
        }

        private int NormalizeStep(int value) => Normalize(value);

        private static bool StateValuesEquivalent(IrValue left, IrValue right) => ReferenceEquals(left, right) ||
            left.Constant is int leftConstant && right.Constant == leftConstant;

        /// <summary>
        /// Determines the constant step of an induction variable by recognizing its update as an add or subtract
        /// of the phi value by a constant.
        /// </summary>
        /// <param name="phi">The induction variable's phi value.</param>
        /// <param name="update">The instruction that updates the induction variable.</param>
        /// <param name="step">Receives the normalized step amount.</param>
        /// <returns><see langword="true"/> if the update is a recognized induction step; otherwise, <see langword="false"/>.</returns>
        private bool TryGetInductionStep(IrValue phi, IrInstruction update, out int step)
        {
            step = 0;
            if (update.Opcode == IrOpcode.Add && update.Operands.Count == 2)
            {
                if (ReferenceEquals(update.Operands[0], phi) && update.Operands[1].Constant is int right)
                    step = NormalizeStep(right);
                else if (ReferenceEquals(update.Operands[1], phi) && update.Operands[0].Constant is int left)
                    step = NormalizeStep(left);
                else
                    return false;
                return true;
            }
            if (update.Opcode == IrOpcode.Subtract && update.Operands.Count == 2 &&
                ReferenceEquals(update.Operands[0], phi) && update.Operands[1].Constant is int subtrahend)
            {
                step = NormalizeStep(-subtrahend);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hoists loop-invariant instructions out of each loop into its preheader, when the instruction is safe to
        /// speculate and its memory dependencies do not change within the loop.
        /// </summary>
        /// <param name="routine">The routine to transform.</param>
        /// <param name="cfg">The control-flow analysis of the routine.</param>
        private void LoopInvariantCodeMotion(RoutineIr routine, CfgAnalysis cfg)
        {
            Record("Loops detected", cfg.Loops.Count);
            if (cfg.Loops.Count == 0)
                return;

            var stateDependencies = FindUnknownStateDependencies(routine.Blocks);
            var identityDependencies = FindIdentityDependencies(routine.Blocks);
            var definitions = routine.Blocks.SelectMany(block => block.Instructions.Select(instruction =>
                (instruction, block))).Where(pair => pair.instruction.Result != null)
                .ToDictionary(pair => pair.instruction.Result!, pair => pair);

            foreach (var loop in cfg.Loops.OrderBy(loop => loop.Blocks.Count))
            {
                if (loop.Preheader == null)
                {
                    Record("LICM rejected: no preheader");
                    continue;
                }

                var writtenRegions = loop.Blocks.SelectMany(block => block.Instructions)
                    .Aggregate(IrMemoryRegion.None, (regions, instruction) =>
                        regions | GetUnknownWrittenRegions(instruction));
                var writtenIdentities = loop.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(GetWrittenIdentities).ToHashSet();
                var clobberedHomes = loop.Blocks.SelectMany(block => block.Instructions)
                    .Select(instruction => (instruction.Payload as IrLoweringOperation)?.ResultHome)
                    .OfType<IVariable>().GroupBy(home => home).Where(group => group.Count() > 1)
                    .Select(group => group.Key).ToHashSet();
                var invariantValues = definitions.Where(pair => !loop.Blocks.Contains(pair.Value.block))
                    .Select(pair => pair.Key).ToHashSet();
                foreach (var value in loop.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(instruction => instruction.Operands).Where(value => !definitions.ContainsKey(value)))
                    invariantValues.Add(value);

                var changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var block in loop.Blocks.OrderBy(block => routine.Blocks.IndexOf(block)).ToArray())
                    {
                        for (var index = 0; index < block.Instructions.Count; index++)
                        {
                            var instruction = block.Instructions[index];
                            if (instruction.Result == null || invariantValues.Contains(instruction.Result) ||
                                instruction.Opcode == IrOpcode.Phi || !IsValueNumberable(instruction) ||
                                instruction.Operands.Any(operand => !invariantValues.Contains(operand)))
                                continue;

                            Record("LICM candidates");
                            var dependencies = stateDependencies.GetValueOrDefault(instruction.Result);
                            var identities = identityDependencies.GetValueOrDefault(instruction.Result);
                            var unknownMemoryChanged = (dependencies & writtenRegions) != 0 || identities != null &&
                                identities.Any(identity => (writtenRegions & identity.Region) != 0);
                            var exactMemoryChanged = identities != null && identities.Any(identity =>
                                writtenIdentities.Any(written => identity.MayAlias(written)));
                            if (unknownMemoryChanged || exactMemoryChanged)
                            {
                                Record("LICM rejected: memory changed");
                                Record(unknownMemoryChanged
                                    ? "LICM rejected: unknown memory changed"
                                    : "LICM rejected: exact memory changed");
                                RecordMemoryRejection("LICM", instruction.Opcode,
                                    unknownMemoryChanged ? writtenRegions : writtenIdentities.Aggregate(
                                        IrMemoryRegion.None, (regions, identity) => regions | identity.Region),
                                    unknownMemoryChanged ? "unknown write" : "exact write");
                                continue;
                            }
                            if (!IsSafeToSpeculate(instruction) &&
                                (!loop.ExitSources.All(exit => cfg.Dominates(block, exit)) ||
                                 !loop.Latches.All(latch => cfg.Dominates(block, latch))))
                            {
                                Record("LICM rejected: conditional execution");
                                continue;
                            }
                            var availablePrior = instruction.IsPure && instruction.Payload is IrLoweringOperation
                                { IsStackResult: true }
                                ? loop.Preheader.Instructions.FirstOrDefault(prior =>
                                    EquivalentLoopInvariantExpression(prior, instruction) &&
                                    prior.Payload is IrLoweringOperation
                                    {
                                        ResultHome: not null,
                                        IsStackResult: false,
                                    })
                                : null;
                            if (availablePrior != null)
                            {
                                CanonicalizeLoopInvariantOperands(availablePrior, instruction);
                                Record("LICM deferred: already available");
                                continue;
                            }
                            if (!TryPrepareLicmHome(instruction, clobberedHomes))
                            {
                                Record("LICM rejected: physical availability");
                                continue;
                            }

                            block.Instructions.RemoveAt(index--);
                            loop.Preheader.Instructions.Add(instruction);
                            invariantValues.Add(instruction.Result);
                            Record("LICM hoisted instructions");
                            changed = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether two value-numberable instructions compute the same expression, honoring
        /// commutativity.
        /// </summary>
        /// <param name="left">The first instruction.</param>
        /// <param name="right">The second instruction.</param>
        /// <returns><see langword="true"/> if the instructions are equivalent; otherwise, <see langword="false"/>.</returns>
        private static bool EquivalentExpression(IrInstruction left, IrInstruction right)
        {
            if (left.Result == null || !IsValueNumberable(left) || left.Opcode != right.Opcode ||
                left.Operands.Count != right.Operands.Count)
                return false;
            if (!IsCommutative(left.Opcode))
                return left.Operands.Zip(right.Operands).All(pair =>
                    StateValuesEquivalent(pair.First, pair.Second));
            var unmatched = right.Operands.ToList();
            foreach (var operand in left.Operands)
            {
                var index = unmatched.FindIndex(candidate => StateValuesEquivalent(operand, candidate));
                if (index < 0)
                    return false;
                unmatched.RemoveAt(index);
            }
            return true;
        }

        /// <summary>
        /// Determines whether two loop-invariant instructions compute the same expression, honoring commutativity
        /// and treating equivalent memory addresses as matching operands.
        /// </summary>
        /// <param name="left">The first instruction.</param>
        /// <param name="right">The second instruction.</param>
        /// <returns><see langword="true"/> if the instructions are equivalent; otherwise, <see langword="false"/>.</returns>
        private static bool EquivalentLoopInvariantExpression(IrInstruction left, IrInstruction right)
        {
            if (left.Result == null || !IsValueNumberable(left) || left.Opcode != right.Opcode ||
                left.Operands.Count != right.Operands.Count)
                return false;
            if (!IsCommutative(left.Opcode))
                return left.Operands.Zip(right.Operands).All(pair =>
                    LoopInvariantValuesEquivalent(pair.First, pair.Second));
            var unmatched = right.Operands.ToList();
            foreach (var operand in left.Operands)
            {
                var index = unmatched.FindIndex(candidate => LoopInvariantValuesEquivalent(operand, candidate));
                if (index < 0)
                    return false;
                unmatched.RemoveAt(index);
            }
            return true;
        }

        /// <summary>
        /// Rewrites a duplicate instruction's operands to match the canonical operand ordering.
        /// </summary>
        /// <param name="canonical">The canonical instruction.</param>
        /// <param name="duplicate">The instruction whose operands should be rewritten.</param>
        private static void CanonicalizeLoopInvariantOperands(IrInstruction canonical, IrInstruction duplicate)
        {
            if (!IsCommutative(canonical.Opcode))
            {
                for (var i = 0; i < canonical.Operands.Count; i++)
                    duplicate.Operands[i] = canonical.Operands[i];
                return;
            }
            var unmatched = duplicate.Operands.ToList();
            for (var i = 0; i < canonical.Operands.Count; i++)
            {
                var index = unmatched.FindIndex(candidate =>
                    LoopInvariantValuesEquivalent(canonical.Operands[i], candidate));
                duplicate.Operands[i] = canonical.Operands[i];
                unmatched.RemoveAt(index);
            }
        }

        private static bool LoopInvariantValuesEquivalent(IrValue left, IrValue right) =>
            StateValuesEquivalent(left, right) || left.MutableExternal && right.MutableExternal &&
            left.MemoryIdentity != null && left.MemoryIdentity.Equals(right.MemoryIdentity);

        /// <summary>
        /// Prepares an instruction to be hoisted by assigning it a non-clobbered local result home, promoting its
        /// stack result to a temporary if necessary.
        /// </summary>
        /// <param name="instruction">The instruction being hoisted.</param>
        /// <param name="clobberedHomes">The variable homes that are written multiple times in the loop.</param>
        /// <returns><see langword="true"/> if a home was prepared; otherwise, <see langword="false"/>.</returns>
        private bool TryPrepareLicmHome(IrInstruction instruction, IReadOnlySet<IVariable> clobberedHomes)
        {
            if (instruction.Payload is not IrLoweringOperation
                {
                    ResultHome: IVariable home,
                    RequiredHome: false,
                } lowering)
                return false;
            if (!lowering.IsStackResult)
                return !clobberedHomes.Contains(home);
            if (lowering.StackEscapes || lowering.EmitTo == null)
                return false;
            var reusable = reusableTemporaries().FirstOrDefault(candidate => !clobberedHomes.Contains(candidate));
            if (!costPolicy.ShouldHoist(instruction, reusable == null))
            {
                Record("LICM rejected: unprofitable rewrite");
                return false;
            }
            var temporary = reusable ?? acquireTemporary();
            if (temporary == null)
                return false;
            lowering.ResultHome = temporary;
            lowering.IsStackResult = false;
            instruction.Result!.PhysicalHome = temporary;
            Record("LICM promoted stack results");
            return true;
        }

        /// <summary>
        /// Eliminates partially redundant expressions by inserting them on the paths where they are missing and
        /// joining the results with a phi.
        /// </summary>
        /// <param name="routine">The routine to transform.</param>
        /// <param name="cfg">The control-flow analysis of the routine.</param>
        private void EliminatePartialRedundancies(RoutineIr routine, CfgAnalysis cfg)
        {
            routine.RebuildPredecessors();
            var definitions = routine.Blocks.SelectMany(block => block.Instructions.Select(instruction =>
                (instruction, block))).Where(pair => pair.instruction.Result != null)
                .ToDictionary(pair => pair.instruction.Result!, pair => pair.block);
            var replacements = new Dictionary<IrValue, IrValue>();
            foreach (var block in routine.Blocks.Where(block => block.Predecessors.Count > 1).ToArray())
            {
                var candidate = block.Instructions.FirstOrDefault(instruction => instruction.Opcode != IrOpcode.Phi);
                if (candidate?.Result == null || !IsSafeToSpeculate(candidate) ||
                    candidate.Payload is not IrLoweringOperation
                    {
                        IsStackResult: true,
                        StackEscapes: false,
                        EmitTo: not null,
                        RequiredHome: false,
                    } candidateLowering ||
                    block.Predecessors.Any(predecessor => predecessor.Terminator is not IrTerminator.Jump jump ||
                        !ReferenceEquals(jump.Target, block)) ||
                    candidate.Operands.Any(operand => definitions.TryGetValue(operand, out var definitionBlock) &&
                        block.Predecessors.Any(predecessor => !cfg.Dominates(definitionBlock, predecessor))))
                    continue;

                var incoming = new Dictionary<IrBlock, IrInstruction?>();
                foreach (var predecessor in block.Predecessors)
                {
                    var prior = predecessor.Instructions.LastOrDefault(instruction =>
                        instruction.Result != null && EquivalentLoopInvariantExpression(instruction, candidate) &&
                        instruction.Payload is IrLoweringOperation
                        {
                            IsStackResult: true,
                            StackEscapes: false,
                            EmitTo: not null,
                            RequiredHome: false,
                        });
                    incoming[predecessor] = prior;
                }
                var insertedEdges = incoming.Count(pair => pair.Value == null);
                if (insertedEdges == 0 || insertedEdges == incoming.Count ||
                    !costPolicy.ShouldPlacePartialRedundancy(candidate, insertedEdges))
                    continue;
                var temporary = acquireTemporary();
                if (temporary == null)
                {
                    Record("PRE rejected: local limit");
                    continue;
                }

                var phi = new IrInstruction(IrOpcode.Phi, routine.CreateValue(), [], payload: new IrPhi(temporary));
                phi.Result!.PhysicalHome = temporary;
                var phiPayload = (IrPhi)phi.Payload!;
                foreach (var pair in incoming)
                {
                    var expression = pair.Value;
                    if (expression == null)
                    {
                        expression = new IrInstruction(candidate.Opcode, routine.CreateValue(), candidate.Operands,
                            candidate.Effect, new IrLoweringOperation(candidateLowering.Emit, temporary,
                                emitTo: candidateLowering.EmitTo), candidate.ReadRegions, candidate.WriteRegions,
                            candidate.CallSummary, candidate.ReadIdentity, candidate.WriteIdentity);
                        expression.Result!.PhysicalHome = temporary;
                        pair.Key.Instructions.Add(expression);
                        Record("PRE inserted expressions");
                    }
                    else
                    {
                        var lowering = (IrLoweringOperation)expression.Payload!;
                        lowering.ResultHome = temporary;
                        lowering.IsStackResult = false;
                        expression.Result!.PhysicalHome = temporary;
                    }
                    phiPayload.Incoming[pair.Key] = expression.Result!;
                }
                block.Instructions.Insert(0, phi);
                block.Instructions.Remove(candidate);
                replacements[candidate.Result] = phi.Result;
                Record("PRE eliminated expressions");
            }
            routine.RebuildPredecessors();
            ReplaceValues(routine, replacements);
        }

        /// <summary>
        /// Determines whether an instruction is safe to speculatively execute, requiring it to be pure and not a
        /// division or variable shift.
        /// </summary>
        /// <param name="instruction">The instruction to check.</param>
        /// <returns><see langword="true"/> if speculation is safe; otherwise, <see langword="false"/>.</returns>
        private static bool IsSafeToSpeculate(IrInstruction instruction) => instruction.IsPure && instruction.Opcode
            is not IrOpcode.Divide and not IrOpcode.Modulo and not IrOpcode.ShiftLeft and not IrOpcode.ShiftRight
            and not IrOpcode.ArithmeticShift and not IrOpcode.LogicalShift;

        /// <summary>
        /// Returns the memory regions an instruction may write.
        /// </summary>
        /// <param name="instruction">The instruction to inspect.</param>
        /// <returns>The written memory regions.</returns>
        private static IrMemoryRegion GetWrittenRegions(IrInstruction instruction) => instruction.Effect switch
        {
            IrEffect.Call => instruction.CallSummary?.GetWrittenRegions() ?? IrMemoryRegion.All,
            IrEffect.Opaque => IrMemoryRegion.All,
            IrEffect.WriteMemory when instruction.WriteRegions == IrMemoryRegion.None => IrMemoryRegion.All,
            IrEffect.WriteMemory => instruction.WriteRegions,
            _ => instruction.WriteRegions,
        };

        /// <summary>
        /// Returns the memory regions an instruction may write to an unknown identity.
        /// </summary>
        /// <param name="instruction">The instruction to inspect.</param>
        /// <returns>The unknown written memory regions.</returns>
        private static IrMemoryRegion GetUnknownWrittenRegions(IrInstruction instruction) => instruction.Effect switch
        {
            IrEffect.Call => instruction.CallSummary?.GetUnknownWrittenRegions() ?? IrMemoryRegion.All,
            IrEffect.Opaque => IrMemoryRegion.All,
            IrEffect.WriteMemory when instruction.WriteIdentity != null => IrMemoryRegion.None,
            IrEffect.WriteMemory when instruction.WriteRegions == IrMemoryRegion.None => IrMemoryRegion.All,
            IrEffect.WriteMemory => instruction.WriteRegions,
            _ => instruction.WriteIdentity != null ? IrMemoryRegion.None : instruction.WriteRegions,
        };

        /// <summary>
        /// Returns the exact memory identities an instruction writes.
        /// </summary>
        /// <param name="instruction">The instruction to inspect.</param>
        /// <returns>The written memory identities.</returns>
        private static IEnumerable<IrMemoryIdentity> GetWrittenIdentities(IrInstruction instruction) =>
            instruction.Effect switch
            {
                IrEffect.Call when instruction.CallSummary != null => instruction.CallSummary.GetWrittenIdentities(),
                IrEffect.WriteMemory when instruction.WriteIdentity != null => [instruction.WriteIdentity],
                _ when instruction.WriteIdentity != null => [instruction.WriteIdentity],
                _ => [],
            };

    }
}
