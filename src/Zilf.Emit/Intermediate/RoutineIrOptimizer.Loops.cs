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

        private static bool CanRedirectEdge(IrBlock predecessor, IrBlock target) => predecessor.Terminator switch
        {
            IrTerminator.Jump jump when ReferenceEquals(jump.Target, target) =>
                jump.Instruction == null || jump.EmitJump != null,
            IrTerminator.Branch branch when ReferenceEquals(branch.WhenTrue, target) ||
                ReferenceEquals(branch.WhenFalse, target) => branch.Instruction == null ||
                !ReferenceEquals(branch.ExplicitTarget, target),
            _ => false,
        };

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

        private static IrTerminator.Jump RedirectJump(IrTerminator.Jump jump, IrBlock newTarget)
        {
            if (jump.Instruction?.Payload is IrLoweringOperation lowering && jump.EmitJump != null)
                jump.Instruction.Payload = new IrLoweringOperation(_ => jump.EmitJump(newTarget),
                    lowering.ResultHome, lowering.IsStackResult, lowering.EmitTo);
            return jump with { Target = newTarget };
        }

        private void SimplifyInductionVariables(RoutineIr routine)
        {
            var cfg = CfgAnalysis.Create(routine);
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

        private void LoopInvariantCodeMotion(RoutineIr routine)
        {
            var cfg = CfgAnalysis.Create(routine);
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
            var temporary = acquireTemporary();
            if (temporary == null)
                return false;
            lowering.ResultHome = temporary;
            lowering.IsStackResult = false;
            instruction.Result!.PhysicalHome = temporary;
            Record("LICM promoted stack results");
            return true;
        }

        private static bool IsSafeToSpeculate(IrInstruction instruction) => instruction.IsPure && instruction.Opcode
            is not IrOpcode.Divide and not IrOpcode.Modulo and not IrOpcode.ShiftLeft and not IrOpcode.ShiftRight
            and not IrOpcode.ArithmeticShift and not IrOpcode.LogicalShift;

        private static IrMemoryRegion GetWrittenRegions(IrInstruction instruction) => instruction.Effect switch
        {
            IrEffect.Call => instruction.CallSummary?.GetWrittenRegions() ?? IrMemoryRegion.All,
            IrEffect.Opaque => IrMemoryRegion.All,
            IrEffect.WriteMemory when instruction.WriteRegions == IrMemoryRegion.None => IrMemoryRegion.All,
            IrEffect.WriteMemory => instruction.WriteRegions,
            _ => IrMemoryRegion.None,
        };

        private static IrMemoryRegion GetUnknownWrittenRegions(IrInstruction instruction) => instruction.Effect switch
        {
            IrEffect.Call => instruction.CallSummary?.GetUnknownWrittenRegions() ?? IrMemoryRegion.All,
            IrEffect.Opaque => IrMemoryRegion.All,
            IrEffect.WriteMemory when instruction.WriteIdentity != null => IrMemoryRegion.None,
            IrEffect.WriteMemory when instruction.WriteRegions == IrMemoryRegion.None => IrMemoryRegion.All,
            IrEffect.WriteMemory => instruction.WriteRegions,
            _ => IrMemoryRegion.None,
        };

        private static IEnumerable<IrMemoryIdentity> GetWrittenIdentities(IrInstruction instruction) =>
            instruction.Effect switch
            {
                IrEffect.Call when instruction.CallSummary != null => instruction.CallSummary.GetWrittenIdentities(),
                IrEffect.WriteMemory when instruction.WriteIdentity != null => [instruction.WriteIdentity],
                _ => [],
            };

    }
}
