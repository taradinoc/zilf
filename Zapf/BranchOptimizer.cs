using System;
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
            /// This must point to the 
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
        }

        private const int INITIAL_SIZE = 1000;

        private readonly List<SEntry> sdis = new List<SEntry>(INITIAL_SIZE);
        private readonly Dictionary<Symbol, int> precedingSdis = new Dictionary<Symbol, int>(INITIAL_SIZE);

        public void RecordSDI(int minAddress, int lengthDifference, Predicate<int> allowShortSpan, Symbol operand)
        {
            SEntry ent;
            ent.MinAddress = minAddress;
            ent.LengthDifference = lengthDifference;
            ent.AllowShortSpan = allowShortSpan;
            ent.Operand = operand;

            sdis.Add(ent);
        }

        public void RecordLabel(Symbol label)
        {
            System.Diagnostics.Debug.Assert(label.Type == SymbolType.GlobalLabel || label.Type == SymbolType.LocalLabel);
            precedingSdis.Add(label, sdis.Count);
        }

        public IEnumerable<bool> Bake()
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
                        }
                    }

            } while (repeat);

            int[] INCREMENT = new int[LONG.Length + 1];
            INCREMENT[0] = 0;
            for (int i = 0; i < LONG.Length; i++)
                INCREMENT[i + 1] = INCREMENT[i] + LONG[i];

            // update symbols
            for (int i = 0; i < sdis.Count; i++)
            {
                Symbol sym = sdis[i].Operand;
                int prec = precedingSdis[sym];
                sym.SetValue(sym.Value + INCREMENT[prec], sym.Pass);
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
                    if (childAddr >= parentAddr && childAddr <= parentOperand)
                        yield return i;
                }
                else
                {
                    if (childAddr >= parentOperand && childAddr <= parentAddr)
                        yield return i;
                }
            }
        }
    }
}
