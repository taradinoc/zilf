/* Copyright 2010-2018 Jesse McGrew
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

using System.Collections.Generic;
using JetBrains.Annotations;

namespace Zilf.Emit.Zap
{
    class DebugFileBuilder : IDebugFileBuilder
    {
        readonly Dictionary<string, int> files = new Dictionary<string, int>();
        readonly List<string> storedLines = new List<string>();

        public int GetFileNumber([CanBeNull] string filename)
        {
            if (filename == null)
                return 0;

            if (files.TryGetValue(filename, out int result))
                return result;

            return files[filename] = files.Count + 1;
        }

        public IEnumerable<string> StoredLines => storedLines;
        public IDictionary<string, int> Files => files;

        public void MarkAction(IOperand action, string name)
        {
            storedLines.Add($".DEBUG-ACTION {action},\"{name}\"");
        }

        public void MarkObject(IObjectBuilder obj, DebugLineRef start, DebugLineRef end)
        {
            storedLines.Add(string.Format(
                ".DEBUG-OBJECT {0},\"{0}\",{1},{2},{3},{4},{5},{6}",
                obj,
                GetFileNumber(start.File),
                start.Line,
                start.Column,
                GetFileNumber(end.File),
                end.Line,
                end.Column));
        }

        public void MarkRoutine(IRoutineBuilder routine, DebugLineRef start, DebugLineRef end)
        {
            ((RoutineBuilder)routine).defnStart = start;
            ((RoutineBuilder)routine).defnEnd = end;
        }

        public void MarkSequencePoint(IRoutineBuilder routine, DebugLineRef point)
        {
            ((RoutineBuilder)routine).MarkSequencePoint(point);
        }
    }
}