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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Zilf.Common;

namespace Zilf.Emit
{
    public enum PeepholeLineType
    {
        /// <summary>
        /// The instruction does not branch.
        /// </summary>
        Plain,
        /// <summary>
        /// Execution never resumes after this instruction, like an unconditional branch,
        /// but there is no associated label.
        /// </summary>
        Terminator,
        /// <summary>
        /// Like a terminator, but should not be duplicated when it is the target of
        /// an unconditional branch.
        /// </summary>
        HeavyTerminator,
        /// <summary>
        /// The instruction branches unconditionally and has no side effects.
        /// </summary>
        BranchAlways,
        /// <summary>
        /// The instruction may branch, but the branch polarity cannot be inverted.
        /// </summary>
        BranchNeutral,
        /// <summary>
        /// The instruction branches if a condition is true.
        /// </summary>
        BranchPositive,
        /// <summary>
        /// The instruction branches if a condition is false.
        /// </summary>
        BranchNegative,
    }

    readonly record struct CombinableLine<TCode>(ILabel? Label, TCode Code, ILabel? Target, PeepholeLineType Type);

    readonly record struct CombinerResult<TCode>(int LinesConsumed, IEnumerable<CombinableLine<TCode>> NewLines);

    /// <summary>
    /// Snapshot of a peephole optimization's activity.
    /// </summary>
    public readonly record struct PeepholeOptimizationStat(string Name, int Applications, int InstructionsSaved);

    /// <summary>
    /// Indicates whether two branches test the same condition.
    /// </summary>
    enum SameTestResult
    {
        /// <summary>
        /// The branches do not test the same condition.
        /// </summary>
        Unrelated,
        /// <summary>
        /// The branches test the same condition with the same polarity.
        /// </summary>
        SameTest,
        /// <summary>
        /// The branches test the same condition but with opposite polarity.
        /// </summary>
        OppositeTest,
    }

    /// <summary>
    /// Indicates whether a plain instruction controls a conditional branch.
    /// </summary>
    enum ControlsConditionResult
    {
        /// <summary>
        /// The plain instruction is unrelated to the conditional branch,
        /// or it has additional side effects besides controlling the branch.
        /// </summary>
        Unrelated,
        /// <summary>
        /// The plain instruction causes the condition to branch (if its polarity
        /// is positive), with no other side effects.
        /// </summary>
        CausesBranchIfPositive,
        /// <summary>
        /// The plain instruction causes the condition to fall through (if its
        /// polarity is positive), with no other side effects.
        /// </summary>
        CausesNoOpIfPositive,
    }
    interface IPeepholeCombiner<TCode>
    {
        /// <summary>
        /// Tries to apply one or more optimizations to a sequence of code.
        /// </summary>
        /// <param name="lines">The original instruction sequence.</param>
        /// <returns>A value indicating how many instructions were consumed
        /// and which instructions they should be replaced with.</returns>
        CombinerResult<TCode> Apply(IEnumerable<CombinableLine<TCode>> lines);

        /// <summary>
        /// Generates code for an unconditional branch.
        /// </summary>
        /// <returns>The code.</returns>
        TCode SynthesizeBranchAlways();

        /// <summary>
        /// Determines whether two instructions are functionally identical.
        /// </summary>
        /// <param name="a">The first instruction.</param>
        /// <param name="b">The second instruction.</param>
        /// <returns><see langword="true"/> if the instructions are identical.</returns>
        [System.Diagnostics.Contracts.Pure]
        bool AreIdentical(TCode a, TCode b);

        /// <summary>
        /// Merges any non-functional information from two instructions that
        /// have already been found to be functionally identical.
        /// </summary>
        /// <param name="a">The first instruction.</param>
        /// <param name="b">The second instruction.</param>
        /// <returns>The merged instruction.</returns>
        TCode MergeIdentical(TCode a, TCode b);

        /// <summary>
        /// Determines whether an instruction may be duplicated as part of an
        /// optimization.
        /// </summary>
        /// <param name="c">The instruction.</param>
        /// <returns><see langword="true"/> if the instruction may be duplicated.</returns>
        /// <remarks>
        /// The motivating use case is "optimize branch to terminator". This optimization
        /// is always disabled for some instructions (via <see cref="PeepholeLineType.HeavyTerminator"/>),
        /// but in some cases we need to disable it contextually as well.
        /// </remarks>
        bool CanDuplicate(TCode c);

        /// <summary>
        /// Determines whether one branch instruction tests the same condition
        /// that has already been tested by an earlier branch instruction,
        /// assuming the second test happens immediately after the first.
        /// </summary>
        /// <param name="a">The first instruction.</param>
        /// <param name="b">The second instruction.</param>
        /// <returns>A value indicating whether the branches are related and
        /// whether they have the same polarity.</returns>
        [System.Diagnostics.Contracts.Pure]
        SameTestResult AreSameTest(TCode a, TCode b);

        /// <summary>
        /// Determines whether a plain instruction serves only to create a
        /// condition that will be immediately checked by a conditional branch.
        /// </summary>
        /// <param name="a">The first instruction.</param>
        /// <param name="b">The second instruction.</param>
        /// <returns>A value indicating whether the branch tests a condition
        /// created by the first instruction and, if so, whether it causes
        /// the branch to become unconditional or no-op.</returns>
        [System.Diagnostics.Contracts.Pure]
        ControlsConditionResult ControlsConditionalBranch(TCode a, TCode b);

    }

    interface IPeepholeCombinerWithStats
    {
        IEnumerable<PeepholeOptimizationStat> GetOptimizationStats();
    }

    /// <summary>
    /// Implements target-independent peephole optimizations, such as removing unnecessary branches.
    /// </summary>
    /// <typeparam name="TCode">The type used to represent instructions.</typeparam>
    class PeepholeBuffer<TCode>
    {
        class Line
        {
            public ILabel? Label;
            public TCode Code;
            public ILabel? TargetLabel;
            public PeepholeLineType Type;

            public Line? TargetLine;
            public bool Flag;       // toggled to mark reachability

            public Line(ILabel? label, TCode code, ILabel? target, PeepholeLineType type)
            {
                Label = label;
                Code = code;
                TargetLabel = target;
                Type = type;
            }

            public void CopyFrom(Line other)
            {
                Label = other.Label;
                Code = other.Code;
                TargetLabel = other.TargetLabel;
                Type = other.Type;

                TargetLine = other.TargetLine;
                Flag = other.Flag;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();

                if (Label != null)
                {
                    sb.Append(Label);
                    sb.Append(": ");
                }
                sb.Append(Code);
                if (TargetLabel != null)
                {
                    switch (Type)
                    {
                        case PeepholeLineType.BranchPositive:
                            sb.Append(" /");
                            break;
                        case PeepholeLineType.BranchNegative:
                            sb.Append(" \\");
                            break;
                        case PeepholeLineType.BranchNeutral:
                            sb.Append(" to? ");
                            break;
                        default:
                            sb.Append(" to ");
                            break;
                    }
                    sb.Append(TargetLabel);
                }

                return sb.ToString();
            }
        }

    ILabel? pendingLabel;
    readonly Dictionary<ILabel, ILabel> aliases = new();
    readonly LinkedList<Line> lines = new();
    readonly OptimizationDescriptor[] optimizationPipeline;
#if DEBUG
    readonly OptimizationStats[] optimizationStats;
#endif

        /// <summary>
        /// Gets or sets the delegate that will be used to combine adjacent instructions.
        /// </summary>
        public IPeepholeCombiner<TCode>? Combiner { get; set; }

        /// <summary>
        /// Gets or sets the factory used to allocate new labels for optimizations that need them.
        /// </summary>
        public Func<ILabel>? LabelFactory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracing output should be enabled for this buffer.
        /// </summary>
        public bool TracingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a name for this buffer (e.g., routine name) for tracing purposes.
        /// </summary>
        public string? TracingName { get; set; }

        public PeepholeBuffer()
        {
            optimizationPipeline = BuildOptimizationPipeline();
#if DEBUG
            optimizationStats = optimizationPipeline.Select(d => new OptimizationStats(d.Name)).ToArray();
#endif
        }

        readonly record struct OptimizationDescriptor(string Name, OptimizationStep Step);

        delegate OptimizationStepResult OptimizationStep(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            Action<LinkedListNode<Line>> markReachable);

        enum OptimizationAdvance
        {
            Continue,
            RestartCurrentNode,
            MoveToNextNode,
        }

        readonly record struct OptimizationStepResult(bool Changed, OptimizationAdvance Advance, LinkedListNode<Line>? NextNode, int LinesRemoved, int LinesAdded)
        {
            public int InstructionsSaved => LinesRemoved - LinesAdded;

            public static OptimizationStepResult Continue() => new(false, OptimizationAdvance.Continue, null, 0, 0);

            public static OptimizationStepResult ChangedContinue(int linesRemoved = 0, int linesAdded = 0) =>
                new(true, OptimizationAdvance.Continue, null, linesRemoved, linesAdded);

            public static OptimizationStepResult RestartCurrent(LinkedListNode<Line>? node, int linesRemoved = 0, int linesAdded = 0, bool changed = true) =>
                new(changed, OptimizationAdvance.RestartCurrentNode, node, linesRemoved, linesAdded);

            public static OptimizationStepResult MoveTo(LinkedListNode<Line>? node, bool changed, int linesRemoved = 0, int linesAdded = 0) =>
                new(changed, OptimizationAdvance.MoveToNextNode, node, linesRemoved, linesAdded);
        }

#if DEBUG
        class OptimizationStats
        {
            public OptimizationStats(string name) => Name = name;

            public string Name { get; }
            public int Applications { get; private set; }
            public int InstructionsSaved { get; private set; }

            public void Record(int instructionsSaved)
            {
                Applications++;
                InstructionsSaved += instructionsSaved;
            }

            public PeepholeOptimizationStat Snapshot() => new(Name, Applications, InstructionsSaved);
        }
#endif

        /// <summary>
        /// Adds an instruction to the buffer.
        /// </summary>
        /// <param name="code">The instruction.</param>
        /// <param name="target">The target label of this instruction, or null.</param>
        /// <param name="type">The type of instruction.</param>
        public void AddLine(TCode code, ILabel? target, PeepholeLineType type)
        {
            lines.AddLast(new Line(pendingLabel, code, target, type));
            pendingLabel = null;
        }

        /// <summary>
        /// Inserts the contents of another buffer at the beginning of this buffer.
        /// </summary>
        /// <param name="other">The other buffer.</param>
        /// <exception cref="InvalidOperationException">
        /// One of the labels in the other buffer's <see cref="aliases"/> also exists in this buffer's <see cref="aliases"/>.
        /// </exception>
        public void InsertBufferFirst(PeepholeBuffer<TCode> other)
        {
            // turn pending label into a label on our first line, or copy it if we have no lines
            if (other.pendingLabel != null)
            {
                var firstLine = lines.First;
                if (firstLine == null)
                {
                    pendingLabel = other.pendingLabel;
                }
                else if (firstLine.Value.Label == null)
                {
                    firstLine.Value.Label = other.pendingLabel;
                }
                else
                {
                    aliases.Add(other.pendingLabel, firstLine.Value.Label);
                }
            }

            // copy lines
            if (other.lines.Count > 0)
            {
                var prev = lines.AddFirst(other.lines.First!.Value);
                var src = other.lines.First.Next;

                while (src != null)
                {
                    prev = lines.AddAfter(prev, src.Value);
                    src = src.Next;
                }
            }

            // merge aliases
            foreach (var pair in other.aliases)
            {
                if (aliases.ContainsKey(pair.Key) || aliases.ContainsKey(pair.Value))
                    throw new InvalidOperationException("The same label was emitted in both buffers");

                aliases.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Marks a label at the current position.
        /// </summary>
        /// <param name="label">The label to mark.</param>
        public void MarkLabel(ILabel label)
        {
            if (pendingLabel == null)
                pendingLabel = label;
            else
                aliases.Add(label, pendingLabel);
        }

        /// <summary>
        /// Performs optimizations and returns the optimized instruction sequence
        /// by calling a delegate.
        /// </summary>
        /// <param name="handler">The delegate to call.</param>
        /// <remarks>
        /// <para>The delegate will be called with four parameters for each
        /// instruction in the optimized sequence: the instruction's label (or
        /// null), the code, the target label (or null), and the instruction
        /// type.</para>
        /// <para>The optimized sequence may differ from the original sequence in
        /// several ways:</para>
        /// <list type="bullet">
        ///     <item><description>
        ///     Multiple labels marked at the same position will be merged into a single label.
        ///     </description></item>
        ///     <item><description>
        ///     Instructions may be deleted or reordered.
        ///     </description></item>
        ///     <item><description>
        ///     Instruction labels and target labels may be moved from one instruction to another.
        ///     </description></item>
        ///     <item><description>Instruction types may be toggled between
        ///     <see cref="PeepholeLineType.BranchNegative"/> and
        ///     <see cref="PeepholeLineType.BranchPositive"/>.
        ///     </description></item>
        /// </list>
        /// </remarks>
        public void Finish(Action<ILabel?, TCode, ILabel?, PeepholeLineType> handler)
        {
            Optimize();

            foreach (var line in lines)
                handler(line.Label, line.Code, line.TargetLabel, line.Type);
        }

        void Trace(string? message = null)
        {
            if (!TracingEnabled)
                return;

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            if (TracingName != null)
            {
                Console.WriteLine("=== {0} ===", TracingName);
            }

            if (message != null)
            {
                Console.WriteLine("... {0} ...", message);
                Console.WriteLine();
            }

            foreach (var line in lines)
            {
                if (line.Label != null)
                    Console.Write("{0}:", line.Label);

                Console.Write('\t');

                Console.Write(line.Code == null ? "(null)" : line.Code.ToString());

                Console.Write(' ');

                var targetLabel = line.TargetLabel;
                if (targetLabel != null && aliases.TryGetValue(targetLabel, out ILabel? value))
                    targetLabel = value;

                switch (line.Type)
                {
                    case PeepholeLineType.BranchAlways:
                        Console.Write("to* {0}", targetLabel);
                        break;
                    case PeepholeLineType.BranchPositive:
                        Console.Write("/{0}", targetLabel);
                        break;
                    case PeepholeLineType.BranchNegative:
                        Console.Write("\\{0}", targetLabel);
                        break;
                    case PeepholeLineType.BranchNeutral:
                        Console.Write("to? {0}", targetLabel);
                        break;
                    case PeepholeLineType.Plain:
                        Console.Write("_");
                        break;
                    case PeepholeLineType.Terminator:
                        Console.Write("*");
                        break;
                    case PeepholeLineType.HeavyTerminator:
                        Console.Write("**");
                        break;
                }

                Console.WriteLine();
            }
        }

        void Optimize()
        {
            // apply alias mappings and link lines to each other
            var labelMap = new Dictionary<ILabel, Line>();

            foreach (var line in lines)
            {
                if (line.Label != null)
                    labelMap.Add(line.Label, line);
                if (line.TargetLabel != null && aliases.TryGetValue(line.TargetLabel, out var canonical))
                    line.TargetLabel = canonical;
            }

            aliases.Clear();

            foreach (var line in lines)
            {
                if (line.TargetLabel != null)
                {
                    if (labelMap.TryGetValue(line.TargetLabel, out var labeledLine))
                        line.TargetLine = labeledLine;
                }
            }

            // apply optimizations
            bool changed;
            bool reachableFlag = false;
            var queue = new Queue<LinkedListNode<Line>>();
            var usedLabels = new Dictionary<ILabel, bool>();

            int iterations = 0;
            const int MaxIterations = 10000;

            do
            {
                Trace("begin iteration " + iterations);

                if (lines.Count == 0)
                    break;

                if (++iterations > MaxIterations)
                    throw new InvalidOperationException("Optimizer iteration count exceeded, this is probably a bug");

                changed = false;

                // mark code as reachable and detect label usage
                reachableFlag = !reachableFlag;
                usedLabels.Clear();
                MarkReachable(lines.First!);

                void MarkReachable(LinkedListNode<Line> reachableNode)
                {
                    if (reachableNode.Value.Flag == reachableFlag)
                        return;

                    queue.Enqueue(reachableNode);

                    while (queue.Count > 0)
                    {
                        var node = queue.Dequeue();
                        var line = node.Value;

                        if (line.Flag == reachableFlag)
                            continue;

                        line.Flag = reachableFlag;
                        if (line.TargetLabel != null)
                            usedLabels[line.TargetLabel] = true;

                        if (node.Next != null &&
                            line.Type != PeepholeLineType.Terminator &&
                            line.Type != PeepholeLineType.HeavyTerminator &&
                            line.Type != PeepholeLineType.BranchAlways)
                        {
                            queue.Enqueue(node.Next);
                        }

                        if (line.TargetLine != null)
                        {
                            var targetNode = lines.Find(line.TargetLine);
                            Debug.Assert(targetNode != null);
                            queue.Enqueue(targetNode);
                        }
                    }
                }

                var pipeline = optimizationPipeline;

                for (LinkedListNode<Line>? node = lines.First; node != null;)
                {
                    var restarted = false;

                    for (int i = 0; i < pipeline.Length && node != null; i++)
                    {
                        var descriptor = pipeline[i];
                        var result = descriptor.Step(ref node, labelMap, usedLabels, reachableFlag, MarkReachable);

                        if (result.Changed)
                        {
                            changed = true;
#if DEBUG
                            optimizationStats[i].Record(result.InstructionsSaved);
#endif
                        }

                        if (result.Advance == OptimizationAdvance.Continue)
                            continue;

                        if (result.Advance == OptimizationAdvance.RestartCurrentNode)
                        {
                            node = result.NextNode ?? node;
                            restarted = true;
                        }
                        else if (result.Advance == OptimizationAdvance.MoveToNextNode)
                        {
                            node = result.NextNode;
                            restarted = true;
                        }

                        break;
                    }

                    if (node == null)
                        break;

                    if (!restarted)
                        node = node.Next;
                }
            } while (changed);
        }

        bool TryClearUnusedLabel(Line line, Dictionary<ILabel, bool> usedLabels)
        {
            if (line.Label != null && !usedLabels.ContainsKey(line.Label))
            {
                line.Label = null;
                Trace("clear unused label");
                return true;
            }

            return false;
        }

        bool TryRedirectBranchToUnconditional(Line line)
        {
            if (line.TargetLine != null && line.TargetLine.Type == PeepholeLineType.BranchAlways)
            {
                line.TargetLabel = line.TargetLine.TargetLabel;
                line.TargetLine = line.TargetLine.TargetLine;

                Trace("optimize branch to unconditional");
                return true;
            }

            return false;
        }

        bool TryOptimizeConditionalBranchChain(
            ref LinkedListNode<Line> node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            out int linesAdded)
        {
            linesAdded = 0;
            if (Combiner == null)
                return false;

            var line = node.Value;
            if (line.TargetLine == null || !IsInvertibleBranch(line.TargetLine.Type))
                return false;

            var relation = Combiner.AreSameTest(line.Code, line.TargetLine.Code);
            if (relation == SameTestResult.Unrelated)
                return false;

            var originalTarget = line.TargetLine;
            var targetNode = lines.Find(originalTarget);

            Debug.Assert(targetNode?.Next != null);

            var lineAfterTarget = targetNode.Next.Value;

            /* handle "conditional branch to [next?] related conditional branch":
             *
             * If COND1? and COND2? test the same condition, then:
             *
             *        COND1? /again
             *        ...
             * again: COND2? /elsewhere
             *
             *        becomes:
             *
             *        COND1? /elsewhere
             *        ...
             *        COND2? /elsewhere
             *
             * If they test opposite conditions (or the polarities are opposite), then
             * instead it becomes:
             *
             *        COND1? /skip
             *        ...
             *        COND2? /elsewhere
             * skip:  ...
             *
             * Also, if COND1 falls through to its target (or falls through to an
             * unconditional jump to its target), insert a jump past COND2 (or to
             * COND2's target if they test opposite conditions!), so this:
             *
             *        COND1? /again
             * again: COND2? /elsewhere
             *        ...
             *
             *        becomes:
             *
             *        COND1? /elsewhere
             *        JUMP skip
             *        COND2? /elsewhere         ; may be deleted later
             * skip:  ...
             */

            var sameCondition = (relation == SameTestResult.SameTest) == (line.Type == originalTarget.Type);
            if (sameCondition)
            {
                line.TargetLabel = originalTarget.TargetLabel;
                line.TargetLine = originalTarget.TargetLine;
            }
            else
            {
                var newLabel = EnsureLabel(lineAfterTarget, labelMap);
                line.TargetLabel = newLabel;
                line.TargetLine = lineAfterTarget;
                usedLabels[newLabel] = true;
            }

            if (node.Next != null &&
                (node.Next.Value == originalTarget ||
                 (node.Next.Value.Type == PeepholeLineType.BranchAlways && node.Next.Value.TargetLine == originalTarget)))
            {
                ILabel jumpTargetLabel;
                Line? jumpTargetLine;

                if (sameCondition)
                {
                    jumpTargetLine = lineAfterTarget;
                    jumpTargetLabel = EnsureLabel(lineAfterTarget, labelMap);
                }
                else
                {
                    Debug.Assert(originalTarget.TargetLabel != null);
                    jumpTargetLabel = originalTarget.TargetLabel;
                    jumpTargetLine = originalTarget.TargetLine;
                }

                var newLine = new Line(
                    null,
                    Combiner.SynthesizeBranchAlways(),
                    jumpTargetLabel,
                    PeepholeLineType.BranchAlways)
                {
                    TargetLine = jumpTargetLine,
                    Flag = reachableFlag
                };
                usedLabels[jumpTargetLabel] = true;

                node = lines.AddAfter(node, newLine);
                linesAdded++;
            }

            Trace("optimize conditional branch to related conditional");
            return true;
        }

        bool TryOptimizeBranchOverUnconditional(ref LinkedListNode<Line> node, bool reachableFlag, out int linesAdded)
        {
            linesAdded = 0;
            var line = node.Value;

            if (!IsInvertibleBranch(line.Type) || node.Next?.Next == null)
                return false;

            if (line.TargetLine != node.Next.Next.Value)
                return false;

            if (node.Next.Value.Type != PeepholeLineType.BranchAlways)
                return false;

            if (line.TargetLine == node.Next.Value.TargetLine)
                return false;

            /* handle "conditional branch over unconditional branch":
             *
             *       COND? /skip
             *       JUMP  elsewhere
             * skip: FOO
             *
             *       becomes:
             *
             *       COND? \elsewhere       ; negate branch and use uncond's target
             *       JUMP skip              ; insert uncond branch to cond's target
             *       JUMP elsewhere
             * skip: FOO
             *
             * both unconditional branches will eventually be deleted by other rules.
             *
             * but we have to avoid getting trapped in a loop by something like this:
             *
             *       COND? /skip
             *       JUMP skip
             * skip: FOO
             *
             * which would otherwise become:
             *
             *       COND? \skip
             *       JUMP skip
             *       JUMP skip
             * skip: FOO
             *
             * ... and then the second jump is deleted and we're right back where we
             * started, with the opposite polarity.
             */

            line.Type = InvertBranch(line.Type);

            var newLine = new Line(
                null,
                Combiner == null ? default! : Combiner.SynthesizeBranchAlways(),
                line.TargetLabel,
                PeepholeLineType.BranchAlways)
            {
                TargetLine = line.TargetLine,
                Flag = reachableFlag
            };

            line.TargetLabel = node.Next.Value.TargetLabel;
            line.TargetLine = node.Next.Value.TargetLine;

            node = lines.AddAfter(node, newLine);
            linesAdded = 1;

            Trace("optimize conditional branch over unconditional");
            return true;
        }

        bool TryRemoveBranchToNext(LinkedListNode<Line> node)
        {
            var line = node.Value;

            if (line.Type == PeepholeLineType.BranchAlways &&
                node.Next != null && line.TargetLine == node.Next.Value)
            {
                Trace("doom branch to next");
                return true;
            }

            return false;
        }

        bool TryOptimizeBranchToTerminator(LinkedListNode<Line> node, Dictionary<ILabel, Line> labelMap)
        {
            if (Combiner == null)
                return false;

            var line = node.Value;

            if (line.Type != PeepholeLineType.BranchAlways ||
                line.TargetLine?.Type != PeepholeLineType.Terminator ||
                !Combiner.CanDuplicate(line.TargetLine.Code))
            {
                return false;
            }

            var oldLabel = line.Label;
            line.CopyFrom(line.TargetLine);
            line.Label = oldLabel;
            if (line.Label != null)
                labelMap[line.Label] = line;

            Trace("optimize branch to terminator");
            return true;
        }

        bool TryOptimizePushedConstantFallThrough(
            LinkedListNode<Line> node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (Combiner == null)
                return false;

            var line = node.Value;
            if (line.Type != PeepholeLineType.Plain)
                return false;

            var nextNode = node.Next;
            if (nextNode == null)
                return false;

            var nextLine = nextNode.Value;
            if (!IsInvertibleBranch(nextLine.Type))
                return false;

            var controls = Combiner.ControlsConditionalBranch(line.Code, nextLine.Code);
            if (controls == ControlsConditionResult.Unrelated)
                return false;

            line.Code = Combiner.SynthesizeBranchAlways();
            line.Type = PeepholeLineType.BranchAlways;

            var polarity = nextLine.Type == PeepholeLineType.BranchPositive;
            if ((controls == ControlsConditionResult.CausesBranchIfPositive) == polarity)
            {
                line.TargetLabel = nextLine.TargetLabel;
                line.TargetLine = nextLine.TargetLine;
            }
            else
            {
                Debug.Assert(nextNode.Next != null);
                var afterCondition = nextNode.Next.Value;
                var newLabel = EnsureLabel(afterCondition, labelMap);
                line.TargetLabel = newLabel;
                line.TargetLine = afterCondition;
                usedLabels[newLabel] = true;
            }

            Trace("optimize pushed constant falling through to conditional");
            return true;
        }

        bool TryOptimizePushedConstantJump(
            LinkedListNode<Line> node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels)
        {
            if (Combiner == null)
                return false;

            var line = node.Value;
            if (line.Type != PeepholeLineType.Plain)
                return false;

            var nextNode = node.Next;
            if (nextNode == null)
                return false;

            var jumpLine = nextNode.Value;
            if (jumpLine.Type != PeepholeLineType.BranchAlways || jumpLine.TargetLine == null)
                return false;

            var controls = Combiner.ControlsConditionalBranch(line.Code, jumpLine.TargetLine.Code);
            if (controls == ControlsConditionResult.Unrelated)
                return false;

            line.Code = Combiner.SynthesizeBranchAlways();
            line.Type = PeepholeLineType.BranchAlways;

            var polarity = jumpLine.TargetLine.Type == PeepholeLineType.BranchPositive;
            if ((controls == ControlsConditionResult.CausesBranchIfPositive) == polarity)
            {
                line.TargetLabel = jumpLine.TargetLine.TargetLabel;
                line.TargetLine = jumpLine.TargetLine.TargetLine;
            }
            else
            {
                var targetNode = lines.Find(jumpLine.TargetLine);
                Debug.Assert(targetNode?.Next != null);
                var afterCondition = targetNode.Next.Value;
                var newLabel = EnsureLabel(afterCondition, labelMap);
                line.TargetLabel = newLabel;
                line.TargetLine = afterCondition;
                usedLabels[newLabel] = true;
            }

            Trace("optimize pushed constant jumping to conditional");
            return true;
        }

        bool TryMergeAdjacentTerminators(
            LinkedListNode<Line> node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (Combiner == null)
                return false;

            var line = node.Value;
            var nextNode = node.Next;
            if (nextNode == null)
                return false;

            var nextLine = nextNode.Value;

            if ((line.Type != PeepholeLineType.BranchAlways &&
                 line.Type != PeepholeLineType.Terminator &&
                 line.Type != PeepholeLineType.HeavyTerminator) ||
                line.TargetLabel != nextLine.TargetLabel ||
                !Combiner.AreIdentical(line.Code, nextLine.Code))
            {
                return false;
            }

            markReachable(nextNode);

            nextLine.Code = Combiner.MergeIdentical(line.Code, nextLine.Code);

            if (nextLine.Label == null)
            {
                nextLine.Label = line.Label;
                if (nextLine.Label != null)
                    labelMap[nextLine.Label] = nextLine;
                line.Label = null;
            }

            foreach (var other in lines)
            {
                if (other.TargetLine == line)
                {
                    other.TargetLabel = nextLine.Label;
                    other.TargetLine = nextLine;
                    if (other.TargetLabel != null)
                        usedLabels[other.TargetLabel] = true;
                }
            }

            Trace("merge adjacent identical terminators/unconditionals");
            return true;
        }

        bool TryApplyCombiner(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            bool reachableFlag,
            Dictionary<ILabel, bool> usedLabels,
            out int linesRemoved,
            out int linesAdded)
        {
            linesRemoved = 0;
            linesAdded = 0;
            if (Combiner == null)
                return false;

            if (node == null)
                return false;

            var result = Combiner.Apply(EnumerateCombinableLines(node));
            if (result.LinesConsumed <= 0)
                return false;

            var consumed = result.LinesConsumed;
            var newClines = result.NewLines.ToList();
            var insertAfter = node.Previous;

            linesRemoved = result.LinesConsumed;
            linesAdded = newClines.Count;

            var current = node;
            var next = node.Next;
            while (consumed > 0 && current != null)
            {
                lines.Remove(current);
                current = next;
                next = current?.Next;
                consumed--;
            }

            LinkedListNode<Line>? firstNewNode = null;
            foreach (var newCline in newClines)
            {
                var newLine = new Line(newCline.Label, newCline.Code, newCline.Target, newCline.Type)
                {
                    Flag = reachableFlag
                };

                if (newLine.Label != null)
                    labelMap[newLine.Label] = newLine;

                var newNode = new LinkedListNode<Line>(newLine);

                if (insertAfter != null)
                    lines.AddAfter(insertAfter, newNode);
                else
                    lines.AddFirst(newNode);

                insertAfter = newNode;
                firstNewNode ??= newNode;
            }

            foreach (var line in lines)
            {
                if (line.TargetLabel != null && labelMap.TryGetValue(line.TargetLabel, out var labeledLine))
                    line.TargetLine = labeledLine;
            }

            node = firstNewNode ?? current ?? insertAfter ?? lines.First;

            Trace("apply user combiner");
            return true;
        }

        LinkedListNode<Line>? DeleteLine(
            LinkedListNode<Line> node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            Action<LinkedListNode<Line>> markReachable)
        {
            var line = node.Value;
            var next = node.Next;

            lines.Remove(node);

            if (line.Label != null && next != null)
            {
                markReachable(next);

                if (next.Value.Label == null)
                {
                    // Transfer the label to the next line
                    next.Value.Label = line.Label;
                    labelMap[next.Value.Label] = next.Value;
                }
                else
                {
                    // Next line already has a label - update labelMap to point the deleted label
                    // to the next line, and update any branches that targeted the deleted label
                    labelMap[line.Label] = next.Value;
                }

                // Update any branches that target the deleted line (by TargetLine reference)
                // or the deleted line's label (by TargetLabel, in case TargetLine wasn't resolved)
                foreach (var other in lines)
                {
                    if (other.TargetLine == line || other.TargetLabel == line.Label)
                    {
                        other.TargetLabel = next.Value.Label;
                        other.TargetLine = next.Value;
                        if (other.TargetLabel != null)
                            usedLabels[other.TargetLabel] = true;
                    }
                }
            }

            Trace("delete doomed line");

            return next;
        }

        ILabel EnsureLabel(Line line, Dictionary<ILabel, Line> labelMap)
        {
            if (line.Label == null)
            {
                line.Label = CreateLabel();
                labelMap[line.Label] = line;
            }

            return line.Label;
        }

        ILabel CreateLabel()
        {
            if (LabelFactory == null)
                throw new InvalidOperationException("No label factory was provided for peephole optimizations.");

            return LabelFactory();
        }

        OptimizationDescriptor[] BuildOptimizationPipeline() =>
        [
            new("clear unused labels", RunClearUnusedLabelStep),
            new("remove unreachable line", RunRemoveUnreachableLineStep),
            new("optimize branch targets", RunOptimizeBranchTargetsStep),
            new("optimize pushed constants", RunOptimizePushedConstantsStep),
            new("merge adjacent terminators", RunMergeAdjacentTerminatorsStep),
            new("apply combiner", RunApplyCombinerStep),
        ];

        OptimizationStepResult RunClearUnusedLabelStep(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (node == null)
                return OptimizationStepResult.Continue();

            return TryClearUnusedLabel(node.Value, usedLabels)
                ? OptimizationStepResult.ChangedContinue()
                : OptimizationStepResult.Continue();
        }

        OptimizationStepResult RunRemoveUnreachableLineStep(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (node == null)
                return OptimizationStepResult.Continue();

            if (node.Value.Flag == reachableFlag)
                return OptimizationStepResult.Continue();

            Trace("doom unreachable line");

            var nextNode = DeleteLine(node, labelMap, usedLabels, markReachable);
            node = nextNode;
            return OptimizationStepResult.MoveTo(nextNode, changed: true, linesRemoved: 1);
        }

        OptimizationStepResult RunOptimizeBranchTargetsStep(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (node == null)
                return OptimizationStepResult.Continue();

            var currentNode = node;
            var line = currentNode.Value;

            if (line.TargetLine == null || line.TargetLine == line)
                return OptimizationStepResult.Continue();

            if (TryRedirectBranchToUnconditional(line))
                return OptimizationStepResult.ChangedContinue();

            var refNode = currentNode;

            if (TryOptimizeConditionalBranchChain(ref refNode, labelMap, usedLabels, reachableFlag, out var chainLinesAdded))
            {
                node = refNode;
                return OptimizationStepResult.ChangedContinue(linesAdded: chainLinesAdded);
            }

            if (TryOptimizeBranchOverUnconditional(ref refNode, reachableFlag, out var overLinesAdded))
            {
                node = refNode;
                return OptimizationStepResult.ChangedContinue(linesAdded: overLinesAdded);
            }

            if (TryRemoveBranchToNext(currentNode))
            {
                var nextNode = DeleteLine(currentNode, labelMap, usedLabels, markReachable);
                node = nextNode;
                return OptimizationStepResult.MoveTo(nextNode, changed: true, linesRemoved: 1);
            }

            if (TryOptimizeBranchToTerminator(currentNode, labelMap))
                return OptimizationStepResult.ChangedContinue();

            return OptimizationStepResult.Continue();
        }

        OptimizationStepResult RunOptimizePushedConstantsStep(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (node == null || Combiner == null)
                return OptimizationStepResult.Continue();

            var currentNode = node;

            if (TryOptimizePushedConstantFallThrough(currentNode, labelMap, usedLabels, markReachable))
                return OptimizationStepResult.ChangedContinue();

            if (TryOptimizePushedConstantJump(currentNode, labelMap, usedLabels))
                return OptimizationStepResult.ChangedContinue();

            return OptimizationStepResult.Continue();
        }

        OptimizationStepResult RunMergeAdjacentTerminatorsStep(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (node == null)
                return OptimizationStepResult.Continue();

            var currentNode = node;

            if (!TryMergeAdjacentTerminators(currentNode, labelMap, usedLabels, markReachable))
                return OptimizationStepResult.Continue();

            var nextNode = DeleteLine(currentNode, labelMap, usedLabels, markReachable);
            node = nextNode;
            return OptimizationStepResult.MoveTo(nextNode, changed: true, linesRemoved: 1);
        }

        OptimizationStepResult RunApplyCombinerStep(
            ref LinkedListNode<Line>? node,
            Dictionary<ILabel, Line> labelMap,
            Dictionary<ILabel, bool> usedLabels,
            bool reachableFlag,
            Action<LinkedListNode<Line>> markReachable)
        {
            if (node == null)
                return OptimizationStepResult.Continue();

            if (!TryApplyCombiner(ref node, labelMap, reachableFlag, usedLabels, out var linesRemoved, out var linesAdded))
                return OptimizationStepResult.Continue();

            return OptimizationStepResult.RestartCurrent(node, linesRemoved, linesAdded);
        }

#if DEBUG
        public IEnumerable<PeepholeOptimizationStat> GetOptimizationStats()
        {
            var stats = optimizationStats.Select(static s => s.Snapshot());

            if (Combiner is IPeepholeCombinerWithStats withStats)
                stats = stats.Concat(withStats.GetOptimizationStats());

            return stats;
        }
#endif

        static IEnumerable<CombinableLine<TCode>> EnumerateCombinableLines([DisallowNull] LinkedListNode<Line>? node)
        {
            yield return new CombinableLine<TCode>(node.Value.Label, node.Value.Code, node.Value.TargetLabel, node.Value.Type);

            for (node = node.Next; node != null && node.Value.Label == null; node = node.Next)
            {
                yield return new CombinableLine<TCode>(null, node.Value.Code, node.Value.TargetLabel, node.Value.Type);
            }
        }

        static bool IsInvertibleBranch(PeepholeLineType type) =>
            type == PeepholeLineType.BranchNegative || type == PeepholeLineType.BranchPositive;

        static PeepholeLineType InvertBranch(PeepholeLineType type) =>
            type switch
            {
                PeepholeLineType.BranchPositive => PeepholeLineType.BranchNegative,
                PeepholeLineType.BranchNegative => PeepholeLineType.BranchPositive,
                _ => throw UnhandledCaseException.FromEnum(type)
            };
    }
}
