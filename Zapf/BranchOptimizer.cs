﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zapf
{
    class BranchOptimizer
    {
        private struct SEntry
        {
            /// <summary>
            /// The address of this SDI, assuming that all previous SDIs are assembled in short form.
            /// </summary>
            public int MinAddress;
            /// <summary>
            /// The number of bytes that would be saved by assembling this SDI in short rather than
            /// long form.
            /// </summary>
            public int LengthDifference;
            /// <summary>
            /// A delegate that indicates whether a given span is legal for the short form of this SDI.
            /// </summary>
            public Predicate<int> AllowShortSpan;
            /// <summary>
            /// The symbol which is the target of this SDI.
            /// </summary>
            public Symbol Operand;
            /// <summary>
            /// The number of padding sections preceding this SDI.
            /// </summary>
            public int PrecedingPaddings;
        }

        private struct PEntry
        {
            /// <summary>
            /// The last starting address at which the padding section was seen.
            /// </summary>
            public int Address;
            /// <summary>
            /// The original starting address of the padding section.
            /// </summary>
            public int MinAddress;
            /// <summary>
            /// A delegate that calculates the size of the padding section if it
            /// starts at a given address.
            /// </summary>
            public Func<int, int> DetermineSize;
            /// <summary>
            /// The number of SDIs that appear before the padding section.
            /// </summary>
            public int PrecedingSDIs;
        }

        private const int INITIAL_SIZE = 1000;

        private readonly List<SEntry> sdis = new List<SEntry>(INITIAL_SIZE);
        private readonly Dictionary<Symbol, int> precedingSdis = new Dictionary<Symbol, int>(INITIAL_SIZE);
        private readonly Dictionary<Symbol, int> precedingPaddings = new Dictionary<Symbol, int>(INITIAL_SIZE);
        private readonly List<PEntry> paddings = new List<PEntry>(INITIAL_SIZE / 10);

        public void RecordSDI(int minAddress, int lengthDifference, Predicate<int> allowShortSpan, Symbol operand)
        {
            SEntry ent;
            ent.MinAddress = minAddress;
            ent.LengthDifference = lengthDifference;
            ent.AllowShortSpan = allowShortSpan;
            ent.Operand = operand;
            ent.PrecedingPaddings = paddings.Count;

            sdis.Add(ent);
        }

        public void RecordPadding(int minAddress, Func<int, int> determineSize)
        {
            PEntry ent;
            ent.Address = ent.MinAddress = minAddress;
            ent.DetermineSize = determineSize;
            ent.PrecedingSDIs = sdis.Count;

            paddings.Add(ent);
        }
        
        public void RecordLabel(Symbol label, int minAddress)
        {
            System.Diagnostics.Debug.Assert(label.Type == SymbolType.GlobalLabel || label.Type == SymbolType.LocalLabel);
            precedingSdis.Add(label, sdis.Count);
            precedingPaddings.Add(label, paddings.Count);
        }

        public IEnumerable<bool> Bake(IEnumerable<Symbol> allLabels)
        {
            // construct graph
            int?[] graph = new int?[sdis.Count];
            for (int i = 0; i < sdis.Count; i++)
                graph[i] = sdis[i].Operand.Value - sdis[i].MinAddress;

            // process graph
            int[] LONG = new int[sdis.Count];

            bool repeat;
            do
            {
                repeat = false;

                for (int i = 0; i < graph.Length; i++)
                    if (graph[i].HasValue)
                    {
                        int v = graph[i].Value;
                        if (!sdis[i].AllowShortSpan(v))
                        {
                            LONG[i] = sdis[i].LengthDifference;
                            foreach (int pi in FindParents(i, graph))
                            {
                                if (sdis[pi].MinAddress < sdis[i].MinAddress)
                                    graph[pi] += LONG[i];
                                else
                                    graph[pi] -= LONG[i];

                                if (pi < i && !sdis[pi].AllowShortSpan(graph[pi].Value))
                                    repeat = true;
                            }
                            graph[i] = null;

                            // try to resize all paddings after this SDI
                            //XXX
                                // adjust span of any SDI that crosses over a changed padding
                                //XXX
                        }
                    }

            } while (repeat);

            int[] INCREMENT = new int[LONG.Length + 1];
            INCREMENT[0] = 0;
            for (int i = 0; i < LONG.Length; i++)
                INCREMENT[i + 1] = INCREMENT[i] + LONG[i];

            int[] PINCREMENT = new int[paddings.Count + 1];
            PINCREMENT[0] = 0;
            for (int i = 0; i < paddings.Count; i++)
            {
                PEntry ent = paddings[i];
                PINCREMENT[i + 1] = PINCREMENT[i] +
                    ent.DetermineSize(ent.Address) - ent.DetermineSize(ent.MinAddress);
            }

            // update symbols
            foreach (Symbol sym in allLabels)
            {
                int precS = precedingSdis[sym];
                int precP = precedingPaddings[sym];
                sym.SetValue(sym.Value + INCREMENT[precS] + PINCREMENT[precP], sym.Pass);
                //System.Diagnostics.Debug.WriteLine(sym.Name + " moves " + INCREMENT[prec] +
                //    " to " + sym.Value);
            }

            // verify graph
            for (int i = 0; i < graph.Length; i++)
            {
                if (graph[i] == null)
                    continue;

                System.Diagnostics.Debug.Assert(sdis[i].AllowShortSpan(graph[i].Value),
                    "illegal graph node");
            }

            // verify short form assignments
            for (int i = 0; i < sdis.Count; i++)
            {
                if (LONG[i] == 0)
                {
                    Symbol sym = sdis[i].Operand;

                    int finalAddr = sdis[i].MinAddress + INCREMENT[i];

                    System.Diagnostics.Debug.Assert(sdis[i].AllowShortSpan(sym.Value - finalAddr),
                        "illegal short form assignment");
                }
            }

            return LONG.Select(l => l != 0);
        }

        private IEnumerable<int> FindParents(int child, int?[] graph)
        {
            int childAddr = sdis[child].MinAddress;

            for (int i = 0; i < graph.Length; i++)
            {
                if (i == child || graph[i] == null)
                    continue;

                int parentAddr = sdis[i].MinAddress;
                int parentOperand = sdis[i].Operand.Value;

                if (parentOperand >= parentAddr)
                {
                    if (childAddr > parentAddr && childAddr <= parentOperand)
                        yield return i;
                }
                else
                {
                    if (childAddr > parentOperand && childAddr < parentAddr)
                        yield return i;
                }
            }
        }

        private IEnumerable<int> FindPadParents(int padding, int?[] graph)
        {
            int padAddr = paddings[padding].MinAddress;

            for (int i = 0; i < graph.Length; i++)
            {
                if (graph[i] == null)
                    continue;

                int parentAddr = sdis[i].MinAddress;
                int parentOperand = sdis[i].Operand.Value;

                if (parentOperand >= parentAddr)
                {
                    if (padAddr > parentAddr && padAddr <= parentOperand)
                        yield return i;
                }
                else
                {
                    if (padAddr > parentOperand && padAddr < parentAddr)
                        yield return i;
                }
            }
        }
    }
}
