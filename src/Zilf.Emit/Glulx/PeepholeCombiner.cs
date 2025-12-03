/* Copyright 2010-2025 Tara McGrew
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

namespace Zilf.Emit.Glulx
{
    /// <summary>
    /// Implements Glulx-specific peephole optimizations.
    /// </summary>
    class PeepholeCombiner : IPeepholeCombiner<GlulxCode>, IPeepholeCombinerWithStats
    {
        private readonly CombinerOptimizationDescriptor[] optimizationPipeline;
#if DEBUG
        private readonly OptimizationStats[] optimizationStats;
#endif

        private IEnumerator<CombinableLine<GlulxCode>>? enumerator;
        private List<CombinableLine<GlulxCode>>? matches;

        private readonly record struct CombinerOptimizationDescriptor(string Name, CombinerOptimization Step);

        private delegate bool CombinerOptimization(IEnumerable<CombinableLine<GlulxCode>> lines, out CombinerResult<GlulxCode> result);

#if DEBUG
        private class OptimizationStats
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

        public PeepholeCombiner()
        {
            optimizationPipeline = BuildOptimizationPipeline();
#if DEBUG
            optimizationStats = optimizationPipeline.Select(d => new OptimizationStats(d.Name)).ToArray();
#endif
        }

        void BeginMatch(IEnumerable<CombinableLine<GlulxCode>> lines)
        {
            enumerator = lines.GetEnumerator();
            matches = [];
        }

        bool Match(params Predicate<CombinableLine<GlulxCode>>[] criteria)
        {
            Debug.Assert(matches != null && enumerator != null);
            while (matches.Count < criteria.Length)
            {
                if (!enumerator.MoveNext())
                    return false;

                matches.Add(enumerator.Current);
            }

            return criteria.Zip(matches, (c, m) => c(m)).All(ok => ok);
        }

        void EndMatch()
        {
            enumerator?.Dispose();
            enumerator = null;

            matches = null;
        }

        CombinerResult<GlulxCode> Combine1To1(string newText, string? opcode = null, PeepholeLineType? type = null, ILabel? target = null)
        {
            return new CombinerResult<GlulxCode>(
                1,
                [
                    new CombinableLine<GlulxCode>(
                        matches![0].Label,
                        new GlulxCode { Text = newText, Opcode = opcode },
                        target ?? matches[0].Target,
                        type ?? matches[0].Type)
                ]);
        }

        CombinerResult<GlulxCode> Combine2To1(string newText, string? opcode = null, PeepholeLineType? type = null, ILabel? target = null)
        {
            return new CombinerResult<GlulxCode>(
                2,
                [
                    new CombinableLine<GlulxCode>(
                        matches![0].Label,
                        new GlulxCode { Text = newText, Opcode = opcode },
                        target ?? matches[1].Target,
                        type ?? matches[1].Type)
                ]);
        }

        static CombinerResult<GlulxCode> Consume(int numberOfLines)
        {
            return new CombinerResult<GlulxCode>(numberOfLines, Enumerable.Empty<CombinableLine<GlulxCode>>());
        }

        /// <inheritdoc />
        public CombinerResult<GlulxCode> Apply(IEnumerable<CombinableLine<GlulxCode>> lines)
        {
            for (int i = 0; i < optimizationPipeline.Length; i++)
            {
                if (!optimizationPipeline[i].Step(lines, out var result))
                    continue;

#if DEBUG
                var newLineCount = CountNewLines(result.NewLines);
                var instructionsSaved = result.LinesConsumed - newLineCount;
                optimizationStats[i].Record(instructionsSaved);
#endif
                return result;
            }

            return new CombinerResult<GlulxCode>();
        }

        private static int CountNewLines(IEnumerable<CombinableLine<GlulxCode>> newLines)
        {
            if (newLines is ICollection<CombinableLine<GlulxCode>> collection)
                return collection.Count;

            var count = 0;
            using var enumerator = newLines.GetEnumerator();
            while (enumerator.MoveNext())
                count++;

            return count;
        }

        // Build the optimization pipeline - for now, this is a minimal implementation
        // that can be extended with Glulx-specific optimizations later
        private CombinerOptimizationDescriptor[] BuildOptimizationPipeline() =>
        [
            new("fold push/return pair", TrySimplifyPushReturn),
            new("eliminate copy to same", TryEliminateCopyToSame),
            new("eliminate redundant push/pop", TryEliminatePushPop),
        ];

        /// <summary>
        /// Optimizes push X followed by return pop to return X.
        /// For push 0/1, creates BranchAlways to Label.RFALSE/Label.RTRUE for further optimization.
        /// </summary>
        bool TrySimplifyPushReturn(IEnumerable<CombinableLine<GlulxCode>> lines, out CombinerResult<GlulxCode> result)
        {
            BeginMatch(lines);
            try
            {
                if (Match(
                    a => a.Code.Opcode == "push",
                    b => b.Code.Text == "return pop"))
                {
                    // Extract the value from "push X"
                    var pushText = matches![0].Code.Text;
                    var value = pushText.Substring(5).Trim(); // Skip "push "
                    
                    // For push 0/1, create BranchAlways to Label.RFALSE/Label.RTRUE for further optimization
                    if (value == "0")
                    {
                        result = Combine2To1("return 0", "return", PeepholeLineType.BranchAlways, Label.RFALSE);
                        return true;
                    }
                    if (value == "1")
                    {
                        result = Combine2To1("return 1", "return", PeepholeLineType.BranchAlways, Label.RTRUE);
                        return true;
                    }
                    
                    result = Combine2To1($"return {value}", "return", PeepholeLineType.Terminator);
                    return true;
                }

                result = default;
                return false;
            }
            finally
            {
                EndMatch();
            }
        }

        /// <summary>
        /// Eliminates "copy X -> X" instructions.
        /// </summary>
        bool TryEliminateCopyToSame(IEnumerable<CombinableLine<GlulxCode>> lines, out CombinerResult<GlulxCode> result)
        {
            BeginMatch(lines);
            try
            {
                if (Match(a => a.Code.Opcode == "copy"))
                {
                    var text = matches![0].Code.Text;
                    // Parse "copy SRC -> DEST"
                    var arrowIndex = text.IndexOf(" -> ", StringComparison.Ordinal);
                    if (arrowIndex > 0)
                    {
                        var src = text.Substring(5, arrowIndex - 5).Trim(); // Skip "copy "
                        var dest = text.Substring(arrowIndex + 4).Trim();   // Skip " -> "
                        
                        if (src == dest)
                        {
                            result = Consume(1);
                            return true;
                        }
                    }
                }

                result = default;
                return false;
            }
            finally
            {
                EndMatch();
            }
        }

        /// <summary>
        /// Eliminates redundant push/pop pairs like "copy X -> push" followed by "copy pop -> Y"
        /// becoming "copy X -> Y".
        /// </summary>
        bool TryEliminatePushPop(IEnumerable<CombinableLine<GlulxCode>> lines, out CombinerResult<GlulxCode> result)
        {
            BeginMatch(lines);
            try
            {
                if (Match(
                    a => a.Code.Opcode == "copy" && a.Code.Text.EndsWith(" -> push"),
                    b => b.Code.Opcode == "copy" && b.Code.Text.StartsWith("copy pop -> ")))
                {
                    var pushText = matches![0].Code.Text;
                    var popText = matches[1].Code.Text;
                    
                    // Extract source from "copy SRC -> push"
                    var srcEnd = pushText.IndexOf(" -> ", StringComparison.Ordinal);
                    var src = pushText.Substring(5, srcEnd - 5).Trim();
                    
                    // Extract destination from "copy pop -> DEST"
                    var dest = popText.Substring(12).Trim(); // Skip "copy pop -> "
                    
                    result = Combine2To1($"copy {src} -> {dest}", "copy");
                    return true;
                }

                result = default;
                return false;
            }
            finally
            {
                EndMatch();
            }
        }

        public IEnumerable<PeepholeOptimizationStat> GetOptimizationStats()
        {
#if DEBUG
            return optimizationStats.Select(static s => s.Snapshot());
#else
            return Enumerable.Empty<PeepholeOptimizationStat>();
#endif
        }

        public GlulxCode SynthesizeBranchAlways()
        {
            return new GlulxCode { Text = "jump", Opcode = "jump" };
        }

        public bool AreIdentical(GlulxCode a, GlulxCode b) => a.Text == b.Text;

        public GlulxCode MergeIdentical(GlulxCode a, GlulxCode b)
        {
            // For now, just return the first one since they're identical
            return a;
        }

        public bool CanDuplicate(GlulxCode c)
        {
            // Allow duplication of all instructions for now
            // Can be refined later if certain instructions shouldn't be duplicated
            return true;
        }

        public SameTestResult AreSameTest(GlulxCode a, GlulxCode b)
        {
            // For conditional branches, check if they test the same condition
            // This is a simplified implementation; can be extended for more cases
            
            // If either instruction consumes from the stack (uses 'pop'), they can't be
            // considered the same test because each pop consumes a separate stack value.
            // This prevents incorrect optimization of sequences like:
            //     push value1
            //     push value2  
            //     jnz pop -> target  ; consumes value2
            //     jz pop -> elsewhere ; consumes value1
            // where both pops are needed to balance the stack.
            var aOperands = ExtractOperands(a.Text);
            var bOperands = ExtractOperands(b.Text);
            
            if (aOperands.Contains("pop") || bOperands.Contains("pop"))
                return SameTestResult.Unrelated;
            
            // If the instructions are identical, they test the same condition
            if (a.Text == b.Text && a.Opcode == b.Opcode)
                return SameTestResult.SameTest;

            // Check for opposite polarity tests (jz vs jnz, etc.)
            if (a.Opcode != null && b.Opcode != null)
            {
                var opposites = new Dictionary<string, string>
                {
                    ["jz"] = "jnz",
                    ["jnz"] = "jz",
                    ["jeq"] = "jne",
                    ["jne"] = "jeq",
                    ["jlt"] = "jge",
                    ["jge"] = "jlt",
                    ["jgt"] = "jle",
                    ["jle"] = "jgt",
                };

                if (opposites.TryGetValue(a.Opcode, out var opposite) && opposite == b.Opcode)
                {
                    if (aOperands == bOperands)
                        return SameTestResult.OppositeTest;
                }
            }

            return SameTestResult.Unrelated;
        }

        private static string ExtractOperands(string text)
        {
            // Extract operands from instruction text, removing the branch target
            // The branch target may or may not be embedded in the text (format: "opcode operands -> target")
            var arrowIndex = text.IndexOf(" -> ", StringComparison.Ordinal);
            string operandPart;
            if (arrowIndex > 0)
            {
                // Target is embedded in text, extract everything between opcode and " -> "
                operandPart = text.Substring(0, arrowIndex);
            }
            else
            {
                // Target is not embedded (passed separately), use full text
                operandPart = text;
            }
            
            // Extract operands: everything after the first space (the opcode)
            var spaceIndex = operandPart.IndexOf(' ');
            return spaceIndex > 0 ? operandPart.Substring(spaceIndex + 1) : "";
        }

        public ControlsConditionResult ControlsConditionalBranch(GlulxCode a, GlulxCode b)
        {
            // Check if 'a' pushes a constant that controls 'b's branch
            if (a.Opcode == "copy" && a.Text.EndsWith(" -> push"))
            {
                // Extract the value being pushed
                var arrowIndex = a.Text.IndexOf(" -> ", StringComparison.Ordinal);
                var value = a.Text.Substring(5, arrowIndex - 5).Trim();
                
                // Check if 'b' is testing the stack for zero
                if (b.Opcode == "jz" && b.Text.StartsWith("jz pop "))
                {
                    if (int.TryParse(value, out var numValue))
                    {
                        return numValue == 0
                            ? ControlsConditionResult.CausesBranchIfPositive
                            : ControlsConditionResult.CausesNoOpIfPositive;
                    }
                }
                else if (b.Opcode == "jnz" && b.Text.StartsWith("jnz pop "))
                {
                    if (int.TryParse(value, out var numValue))
                    {
                        return numValue != 0
                            ? ControlsConditionResult.CausesBranchIfPositive
                            : ControlsConditionResult.CausesNoOpIfPositive;
                    }
                }
            }

            return ControlsConditionResult.Unrelated;
        }
    }
}
