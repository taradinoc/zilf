/* Copyright 2010, 2012 Jesse McGrew
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

    /// <summary>
    /// Implements target-independent peephole optimizations, such as removing unnecessary branches.
    /// </summary>
    /// <typeparam name="TCode">The type used to represent instructions.</typeparam>
    class PeepholeBuffer<TCode>
    {
        private class Line
        {
            public ILabel Label;
            public TCode Code;
            public ILabel TargetLabel;
            public PeepholeLineType Type;

            public Line TargetLine;
            public bool Flag;
            public int ReachableCount;

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
                this.ReachableCount = other.ReachableCount;
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

        /// <summary>
        /// Wraps a method that can be used by the optimizer to combine a pair of
        /// adjacent instructions.
        /// </summary>
        /// <param name="label">Input: the label of the first instruction. Output: the label
        /// of the combined instruction.</param>
        /// <param name="code1">Input: the code of the first instruction. Output: the code
        /// of the combined instruction.</param>
        /// <param name="target1">Input: the target label of the first instruction.
        /// Output: the target label of the combined instruction.</param>
        /// <param name="type1">Input: the type of the first instruction. Output: the
        /// type of the combined instruction.</param>
        /// <param name="code2">The code of the second instruction.</param>
        /// <param name="target2">The target label of the second instruction.</param>
        /// <param name="type2">The type of the second instruction.</param>
        /// <returns>true if the two instructions were combined; otherwise, false.</returns>
        /// <remarks>
        /// <para>The optimizer will call the method (via its <see cref="Combiner"/> property)
        /// for every pair of adjacent instructions, unless the second instruction is the
        /// target of a branch. Note that the method may even be called for instructions
        /// synthesized by the optimizer, in which case <paramref name="code1"/> and
        /// <paramref name="code2"/> will be <c>default(<typeparamref name="TCode"/>)</c>.</para>
        /// <para>If the method returns true, the pair of instructions will be replaced
        /// with a single instruction specified by the <paramref name="label"/>,
        /// <paramref name="code1"/>, <paramref name="target1"/>, and <paramref name="type1"/>
        /// parameters. If it returns false, the second instruction will be left as-is; the
        /// method should avoid changing any of the ref parameters in this case.</para>
        /// </remarks>
        public delegate bool CombinerDelegate(
            ref ILabel label, ref TCode code1, ref ILabel target1, ref PeepholeLineType type1,
            TCode code2, ILabel target2, PeepholeLineType type2);

        private ILabel pendingLabel;
        private CombinerDelegate combiner;
        private Dictionary<ILabel, ILabel> aliases = new Dictionary<ILabel, ILabel>();
        private LinkedList<Line> lines = new LinkedList<Line>();

        public PeepholeBuffer()
        {
        }

        /// <summary>
        /// Gets or sets the delegate that will be used to combine adjacent instructions.
        /// </summary>
        public CombinerDelegate Combiner
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
        private void Trace()
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

        private void Optimize()
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
                if (line.TargetLabel != null)
                {
                    labelMap.TryGetValue(line.TargetLabel, out line.TargetLine);
                    if (line.TargetLine != null)
                        line.TargetLine.ReachableCount++;
                }

            labelMap = null;

            // apply optimizations
            bool changed;
            bool reachableFlag = false;
            Queue<LinkedListNode<Line>> queue = new Queue<LinkedListNode<Line>>();
            Dictionary<ILabel, bool> usedLabels = new Dictionary<ILabel, bool>();

            do
            {
                Trace();

                if (lines.Count == 0)
                    break;

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
                        line.ReachableCount = 1;
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
                            System.Diagnostics.Debug.Assert(targetNode != null);
                            queue.Enqueue(targetNode);
                        }
                    }
                    else
                        line.ReachableCount++;
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
                        if (line.TargetLine.Type == PeepholeLineType.BranchAlways)
                        {
                            // if the target is an unconditional branch, use its target instead
                            line.TargetLine.ReachableCount--;

                            line.TargetLabel = line.TargetLine.TargetLabel;
                            line.TargetLine = line.TargetLine.TargetLine;

                            if (line.TargetLine != null)
                                line.TargetLine.ReachableCount++;
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
                                null, default(TCode), line.TargetLabel, PeepholeLineType.BranchAlways);
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
                            changed = true;
                        }
                    }

                    if (!delete && node.Next != null && node.Next.Value.Label == null)
                    {
                        // give the user a chance to combine lines
                        if (combiner != null && combiner(
                            ref line.Label, ref line.Code, ref line.TargetLabel, ref line.Type,
                            node.Next.Value.Code, node.Next.Value.TargetLabel, node.Next.Value.Type))
                        {
                            // the lines have been combined
                            if (line.TargetLabel == null)
                                line.TargetLine = null;
                            else
                                line.TargetLine = lines.FirstOrDefault(l => l.Label == line.TargetLabel);

                            lines.Remove(node.Next);
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
                        if (line.Label != null && next != null)
                        {
                            // update references to this label
                            if (next.Value.Label == null)
                                next.Value.Label = line.Label;

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

        private static bool IsInvertibleBranch(PeepholeLineType type)
        {
            return type == PeepholeLineType.BranchNegative ||
                   type == PeepholeLineType.BranchPositive;
        }

        private static PeepholeLineType InvertBranch(PeepholeLineType type)
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
