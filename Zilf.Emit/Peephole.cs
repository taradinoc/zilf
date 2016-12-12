/* Copyright 2010, 2015 Jesse McGrew
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

#undef TRACE_PEEPHOLE

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

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

    struct CombinableLine<TCode>
    {
        public ILabel Label { get; private set; }
        public TCode Code { get; private set; }
        public ILabel Target { get; private set; }
        public PeepholeLineType Type { get; private set; }

        public CombinableLine(ILabel label, TCode code, ILabel target, PeepholeLineType type)
            : this()
        {
            this.Label = label;
            this.Code = code;
            this.Target = target;
            this.Type = type;
        }
    }

    struct CombinerResult<TCode>
    {
        public int LinesConsumed;
        public IEnumerable<CombinableLine<TCode>> NewLines;

        public CombinerResult(int linesConsumed, IEnumerable<CombinableLine<TCode>> newLines)
        {
            this.LinesConsumed = linesConsumed;
            this.NewLines = newLines;
        }
    }

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
        /// <returns><b>true</b> if the instructions are identical.</returns>
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
        /// Determines whether one branch instruction tests the same condition
        /// that has already been tested by an earlier branch instruction,
        /// assuming the second test happens immediately after the first.
        /// </summary>
        /// <param name="a">The first instruction.</param>
        /// <param name="b">The second instruction.</param>
        /// <returns>A value indicating whether the branches are related and
        /// whether they have the same polarity.</returns>
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
        ControlsConditionResult ControlsConditionalBranch(TCode a, TCode b);

        /// <summary>
        /// Allocates a new label.
        /// </summary>
        /// <returns>The new label.</returns>
        ILabel NewLabel();
    }

    /// <summary>
    /// Implements target-independent peephole optimizations, such as removing unnecessary branches.
    /// </summary>
    /// <typeparam name="TCode">The type used to represent instructions.</typeparam>
    class PeepholeBuffer<TCode>
    {
        class Line
        {
            public ILabel Label;
            public TCode Code;
            public ILabel TargetLabel;
            public PeepholeLineType Type;

            public Line TargetLine;
            public bool Flag;       // toggled to mark reachability

            public Line(ILabel label, TCode code, ILabel target, PeepholeLineType type)
            {
                this.Label = label;
                this.Code = code;
                this.TargetLabel = target;
                this.Type = type;
            }

            public void CopyFrom(Line other)
            {
                this.Label = other.Label;
                this.Code = other.Code;
                this.TargetLabel = other.TargetLabel;
                this.Type = other.Type;

                this.TargetLine = other.TargetLine;
                this.Flag = other.Flag;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

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

        ILabel pendingLabel;
        IPeepholeCombiner<TCode> combiner;
        Dictionary<ILabel, ILabel> aliases = new Dictionary<ILabel, ILabel>();
        LinkedList<Line> lines = new LinkedList<Line>();

        public PeepholeBuffer()
        {
        }

        /// <summary>
        /// Gets or sets the delegate that will be used to combine adjacent instructions.
        /// </summary>
        public IPeepholeCombiner<TCode> Combiner
        {
            get { return combiner; }
            set { combiner = value; }
        }

        /// <summary>
        /// Adds an instruction to the buffer.
        /// </summary>
        /// <param name="code">The instruction.</param>
        /// <param name="target">The target label of this instruction, or null.</param>
        /// <param name="type">The type of instruction.</param>
        public void AddLine(TCode code, ILabel target, PeepholeLineType type)
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
                var firstLine = this.lines.First;
                if (firstLine == null)
                {
                    this.pendingLabel = other.pendingLabel;
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
                var prev = lines.AddFirst(other.lines.First.Value);
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
        ///     <see cref="PeepholeLineType.BranchPositive"/>.)
        ///     </description></item>
        /// </list>
        /// </remarks>
        public void Finish(Action<ILabel, TCode, ILabel, PeepholeLineType> handler)
        {
            Optimize();

            foreach (Line line in lines)
                handler(line.Label, line.Code, line.TargetLabel, line.Type);
        }

        [System.Diagnostics.Conditional("TRACE_PEEPHOLE")]
        void Trace()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            foreach (Line line in lines)
            {
                if (line.Label != null)
                    Console.Write("{0}:", line.Label);

                Console.Write('\t');

                Console.Write(line.Code == null ? "(null)" : line.Code.ToString());

                Console.Write(' ');

                ILabel targetLabel = line.TargetLabel;
                if (targetLabel != null && aliases.ContainsKey(targetLabel))
                    targetLabel = aliases[targetLabel];

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
            Dictionary<ILabel, Line> labelMap = new Dictionary<ILabel, PeepholeBuffer<TCode>.Line>();

            foreach (Line line in lines)
            {
                ILabel canonical;
                if (line.Label != null)
                    labelMap.Add(line.Label, line);
                if (line.TargetLabel != null && aliases.TryGetValue(line.TargetLabel, out canonical))
                    line.TargetLabel = canonical;
            }

            aliases.Clear();

            foreach (Line line in lines)
            {
                if (line.TargetLabel != null)
                {
                    Line labeledLine;
                    if (labelMap.TryGetValue(line.TargetLabel, out labeledLine))
                        line.TargetLine = labeledLine;
                }
            }

            // apply optimizations
            bool changed;
            bool reachableFlag = false;
            Queue<LinkedListNode<Line>> queue = new Queue<LinkedListNode<Line>>();
            Dictionary<ILabel, bool> usedLabels = new Dictionary<ILabel, bool>();

            int iterations = 0;
            const int MaxIterations = 10000;

            do
            {
                Trace();

                if (lines.Count == 0)
                    break;

                if (++iterations > MaxIterations)
                    throw new NotImplementedException("Optimizer iteration count exceeded, this is probably a bug");

                changed = false;

                // mark code as reachable and detect label usage
                reachableFlag = !reachableFlag;
                usedLabels.Clear();
                queue.Enqueue(lines.First);

                while (queue.Count > 0)
                {
                    LinkedListNode<Line> node = queue.Dequeue();
                    Line line = node.Value;

                    if (line.Flag != reachableFlag)
                    {
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
                            LinkedListNode<Line> targetNode = lines.Find(line.TargetLine);
                            Contract.Assume(targetNode != null);
                            queue.Enqueue(targetNode);
                        }
                    }
                }

                // apply optimizations to each line
                for (LinkedListNode<Line> node = lines.First; node != null; node = node.Next)
                {
                    Line line = node.Value;
                    bool delete = false;

                    // clear unused labels
                    if (line.Label != null && !usedLabels.ContainsKey(line.Label))
                    {
                        line.Label = null;
                        changed = true;
                    }

                    if (line.Flag != reachableFlag)
                    {
                        // delete unreachable code
                        delete = true;
                    }
                    else if (line.TargetLine != null && line.TargetLine != line)
                    {
                        SameTestResult sameTestResult;
                        if (line.TargetLine.Type == PeepholeLineType.BranchAlways)
                        {
                            // if the target is an unconditional branch, use its target instead
                            line.TargetLabel = line.TargetLine.TargetLabel;
                            line.TargetLine = line.TargetLine.TargetLine;

                            changed = true;
                        }
                        else if (combiner != null && IsInvertibleBranch(line.TargetLine.Type) &&
                                 (sameTestResult = combiner.AreSameTest(line.Code, line.TargetLine.Code)) != SameTestResult.Unrelated)
                        {
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

                            var originalTarget = line.TargetLine;
                            var targetNode = lines.Find(originalTarget);
                            Contract.Assume(targetNode != null && targetNode.Next != null);

                            var lineAfterTarget = targetNode.Next.Value;

                            var sameCondition = (sameTestResult == SameTestResult.SameTest) == (line.Type == line.TargetLine.Type);
                            if (sameCondition)
                            {
                                line.TargetLabel = line.TargetLine.TargetLabel;
                                line.TargetLine = line.TargetLine.TargetLine;
                            }
                            else
                            {
                                if (lineAfterTarget.Label == null)
                                {
                                    lineAfterTarget.Label = combiner.NewLabel();
                                    labelMap[lineAfterTarget.Label] = lineAfterTarget;
                                }

                                line.TargetLabel = lineAfterTarget.Label;
                                line.TargetLine = lineAfterTarget;
                                usedLabels[line.TargetLabel] = true;
                            }

                            if (node.Next != null &&
                                (node.Next.Value == originalTarget ||
                                 (node.Next.Value.Type == PeepholeLineType.BranchAlways && node.Next.Value.TargetLine == originalTarget)))
                            {
                                ILabel jumpTargetLabel;
                                Line jumpTargetLine;
                                if (sameCondition)
                                {
                                    if (lineAfterTarget.Label == null)
                                    {
                                        lineAfterTarget.Label = combiner.NewLabel();
                                        labelMap[lineAfterTarget.Label] = lineAfterTarget;
                                    }

                                    jumpTargetLabel = lineAfterTarget.Label;
                                    jumpTargetLine = lineAfterTarget;
                                }
                                else
                                {
                                    jumpTargetLabel = originalTarget.TargetLabel;
                                    jumpTargetLine = originalTarget.TargetLine;
                                }

                                var newLine = new Line(
                                    null,
                                    combiner.SynthesizeBranchAlways(),
                                    jumpTargetLabel,
                                    PeepholeLineType.BranchAlways);
                                newLine.TargetLine = jumpTargetLine;
                                newLine.Flag = reachableFlag;
                                usedLabels[newLine.TargetLabel] = true;

                                node = lines.AddAfter(node, newLine);
                            }

                            changed = true;
                        }
                        else if (IsInvertibleBranch(line.Type) &&
                                 node.Next != null && node.Next.Next != null &&
                                 line.TargetLine == node.Next.Next.Value &&
                                 node.Next.Value.Type == PeepholeLineType.BranchAlways &&
                                 line.TargetLine != node.Next.Value.TargetLine)
                        {
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
                             * 
                             */

                            line.Type = InvertBranch(line.Type);

                            Line newLine = new Line(
                                null,
                                combiner == null ? default(TCode) : combiner.SynthesizeBranchAlways(),
                                line.TargetLabel,
                                PeepholeLineType.BranchAlways);
                            newLine.TargetLine = line.TargetLine;
                            newLine.Flag = reachableFlag;

                            line.TargetLabel = node.Next.Value.TargetLabel;
                            line.TargetLine = node.Next.Value.TargetLine;

                            node = lines.AddAfter(node, newLine);
                            changed = true;
                        }
                        else if (line.Type == PeepholeLineType.BranchAlways &&
                            node.Next != null && line.TargetLine == node.Next.Value)
                        {
                            // delete "branch to next"
                            delete = true;
                        }
                        else if (line.Type == PeepholeLineType.BranchAlways &&
                            line.TargetLine.Type == PeepholeLineType.Terminator)
                        {
                            // handle "branch to terminator" by replacing the branch with a copy of the terminator
                            ILabel oldLabel = line.Label;
                            line.CopyFrom(line.TargetLine);
                            line.Label = oldLabel;
                            if (line.Label != null)
                                labelMap[line.Label] = line;
                            changed = true;
                        }
                    }

                    if (!delete && combiner != null && line.Type == PeepholeLineType.Plain && node.Next != null)
                    {
                        ControlsConditionResult controlsCondResult;

                        if (IsInvertibleBranch(node.Next.Value.Type) &&
                            (controlsCondResult = combiner.ControlsConditionalBranch(line.Code, node.Next.Value.Code)) != ControlsConditionResult.Unrelated)
                        {
                            // handle "push constant then fall through to a conditional branch that tests it"
                            line.Code = combiner.SynthesizeBranchAlways();
                            line.Type = PeepholeLineType.BranchAlways;

                            var polarity = node.Next.Value.Type == PeepholeLineType.BranchPositive;
                            if ((controlsCondResult == ControlsConditionResult.CausesBranchIfPositive) == polarity)
                            {
                                // branch to condition's target
                                line.TargetLabel = node.Next.Value.TargetLabel;
                                line.TargetLine = node.Next.Value.TargetLine;
                            }
                            else
                            {
                                // branch to instruction after condition
                                var lineAfterCondition = node.Next.Next.Value;
                                if (lineAfterCondition.Label == null)
                                {
                                    lineAfterCondition.Label = combiner.NewLabel();
                                    labelMap[lineAfterCondition.Label] = lineAfterCondition;
                                }

                                line.TargetLabel = lineAfterCondition.Label;
                                line.TargetLine = lineAfterCondition;
                                usedLabels[line.TargetLabel] = true;
                            }

                            changed = true;
                        }
                        else if (node.Next.Value.Type == PeepholeLineType.BranchAlways && node.Next.Value.TargetLine != null &&
                            (controlsCondResult = combiner.ControlsConditionalBranch(line.Code, node.Next.Value.TargetLine.Code)) != ControlsConditionResult.Unrelated)
                        {
                            // handle "push constant then jump to a conditional branch that tests it"
                            line.Code = combiner.SynthesizeBranchAlways();
                            line.Type = PeepholeLineType.BranchAlways;

                            var polarity = node.Next.Value.TargetLine.Type == PeepholeLineType.BranchPositive;
                            if ((controlsCondResult == ControlsConditionResult.CausesBranchIfPositive) == polarity)
                            {
                                // branch to condition's target
                                line.TargetLabel = node.Next.Value.TargetLine.TargetLabel;
                                line.TargetLine = node.Next.Value.TargetLine.TargetLine;
                            }
                            else
                            {
                                // branch to instruction after condition
                                var lineAfterCondition = lines.Find(node.Next.Value.TargetLine).Next.Value;
                                if (lineAfterCondition.Label == null)
                                {
                                    lineAfterCondition.Label = combiner.NewLabel();
                                    labelMap[lineAfterCondition.Label] = lineAfterCondition;
                                }

                                line.TargetLabel = lineAfterCondition.Label;
                                line.TargetLine = lineAfterCondition;
                                usedLabels[line.TargetLabel] = true;
                            }

                            changed = true;
                        }
                    }

                    if (!delete && combiner != null && node.Next != null)
                    {
                        var nextLine = node.Next.Value;

                        // merge adjacent identical terminators and unconditional branches
                        if ((line.Type == PeepholeLineType.BranchAlways ||
                             line.Type == PeepholeLineType.Terminator ||
                             line.Type == PeepholeLineType.HeavyTerminator) &&
                            line.TargetLabel == nextLine.TargetLabel &&
                            combiner.AreIdentical(line.Code, nextLine.Code))
                        {
                            delete = true;

                            nextLine.Flag = reachableFlag;

                            // merge code
                            nextLine.Code = combiner.MergeIdentical(line.Code, nextLine.Code);

                            // merge labels
                            if (nextLine.Label == null)
                            {
                                nextLine.Label = line.Label;
                                if (nextLine.Label != null)
                                    labelMap[nextLine.Label] = nextLine;
                                line.Label = null;
                            }

                            foreach (var l in lines)
                            {
                                if (l.TargetLine == line)
                                {
                                    l.TargetLabel = nextLine.Label;
                                    l.TargetLine = nextLine;
                                }
                            }
                        }
                    }

                    if (!delete && combiner != null)
                    {
                        // give the user a chance to combine lines
                        var result = combiner.Apply(EnumerateCombinableLines(node));
                        if (result.LinesConsumed > 0)
                        {
                            // the lines have been combined
                            var count = result.LinesConsumed;
                            var newClines = result.NewLines.ToList();
                            var addAfter = node.Previous;

                            // remove old lines
                            var next = node.Next;
                            while (node != null && count > 0)
                            {
                                lines.Remove(node);
                                node = next;
                                if (node != null)
                                    next = node.Next;
                                count--;
                            }

                            // we'll leave node pointing at the first new line, but clear it for now
                            node = null;

                            // add new lines
                            foreach (var newCline in newClines)
                            {
                                var newLine = new Line(newCline.Label, newCline.Code, newCline.Target, newCline.Type);
                                newLine.Flag = reachableFlag;

                                if (newLine.Label != null)
                                    labelMap[newLine.Label] = newLine;

                                var newNode = new LinkedListNode<Line>(newLine);

                                if (addAfter != null)
                                    lines.AddAfter(addAfter, newNode);
                                else
                                    lines.AddFirst(newNode);

                                addAfter = newNode;

                                if (node == null)
                                    node = newNode;
                            }

                            // fix targets for old and new lines
                            foreach (var l in lines)
                            {
                                if (l.TargetLabel != null)
                                {
                                    Line labeledLine;
                                    if (labelMap.TryGetValue(l.TargetLabel, out labeledLine))
                                        l.TargetLine = labeledLine;
                                }
                            }

                            changed = true;
                        }
                    }

                    // delete code that has been doomed
                    if (delete)
                    {
                        LinkedListNode<Line> next = node.Next;

                        lines.Remove(node);
                        changed = true;

                        /* if the line is labeled, update references to it. we assume the
                         * optimization rules will never delete the labeled last line of
                         * the function unless it's unreachable. */
                        if (line.Label != null /*&& next != null*/)
                        {
                            Contract.Assert(next != null);

                            // update references to this label
                            if (next.Value.Label == null)
                            {
                                next.Value.Label = line.Label;
                                labelMap[next.Value.Label] = next.Value;
                            }

                            foreach (Line l2 in lines)
                            {
                                if (l2.TargetLine == line)
                                {
                                    l2.TargetLabel = next.Value.Label;
                                    l2.TargetLine = next.Value;
                                }
                            }
                        }

                        if (next != null && next.Previous != null)
                        {
                            node = next.Previous;
                            continue;
                        }
                        else
                            break;
                    }
                }
            } while (changed);
        }

        static IEnumerable<CombinableLine<TCode>> EnumerateCombinableLines(LinkedListNode<Line> node)
        {
            yield return new CombinableLine<TCode>(node.Value.Label, node.Value.Code, node.Value.TargetLabel, node.Value.Type);

            for (node = node.Next; node != null && node.Value.Label == null; node = node.Next)
            {
                yield return new CombinableLine<TCode>(null, node.Value.Code, node.Value.TargetLabel, node.Value.Type);
            }
        }

        static bool IsInvertibleBranch(PeepholeLineType type)
        {
            return type == PeepholeLineType.BranchNegative ||
                   type == PeepholeLineType.BranchPositive;
        }

        static PeepholeLineType InvertBranch(PeepholeLineType type)
        {
            switch (type)
            {
                case PeepholeLineType.BranchPositive:
                    return PeepholeLineType.BranchNegative;

                case PeepholeLineType.BranchNegative:
                    return PeepholeLineType.BranchPositive;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
