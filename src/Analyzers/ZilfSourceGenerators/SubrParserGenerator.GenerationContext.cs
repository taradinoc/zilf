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

namespace ZilfSourceGenerators;

partial class SubrParserGenerator
{
    /// <summary>
    /// Generation context for parameter tree code generation
    /// </summary>
    public class GenerationContext
    {
        public string ArgIndexVar { get; set; } = "argIndex";
        public string ArgsVar { get; set; } = "args";
        public string SiteVar { get; set; } = "site";
        // Name of the variable to use for the ErrorRanker in generated code.
        // Defaults to "ranker" for top-level parsers. Helper methods can override this
        // if they introduce a differently named ranker variable.
        public string RankerVar { get; set; } = "ranker";
        public string MethodName { get; set; } = "";
        public int Depth { get; set; } = 0;
        public int ParameterCount { get; set; } = 0;
        // CallerArgIndexVar is set when the generator is creating code inside a
        // helper method (i.e. one of the emitted TryParse_* locals). In that
        // scenario nested helper invocations need a way to report their
        // failure progress back up to the enclosing helper. When non-null,
        // the generator emits code that assigns the caller's arg-index
        // variable (usually named `callerArgIndex`) before returning false
        // from a nested helper. This lets the outer helper preserve the
        // consumed-argument count and allow the shared ErrorRanker to be
        // consulted by a top-level parser.
        public string? CallerArgIndexVar { get; set; } = null;
        public ParameterNode[] PriorOptionalNodes { get; set; } = [];
        public bool TrackOptionalMismatch { get; set; }

        public string RankerParameter => $"ref ErrorRanker {RankerVar}";
        public string RankerArgument => $"ref {RankerVar}";

        public GenerationContext WithDepth(int newDepth) => new()
        {
            ArgIndexVar = ArgIndexVar,
            ArgsVar = ArgsVar,
            SiteVar = SiteVar,
            RankerVar = RankerVar,
            MethodName = MethodName,
            Depth = newDepth,
            ParameterCount = ParameterCount,
            CallerArgIndexVar = CallerArgIndexVar,
            PriorOptionalNodes = PriorOptionalNodes,
            TrackOptionalMismatch = TrackOptionalMismatch
        };

        public int GetRemainingRequiredParameterCount()
        {
            // This would be calculated by the tree builder based on following parameters
            // For now, return 0 as a placeholder
            return 0;
        }
    }
}
