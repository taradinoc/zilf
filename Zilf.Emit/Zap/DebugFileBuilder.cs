using System.Collections.Generic;

namespace Zilf.Emit.Zap
{
    class DebugFileBuilder : IDebugFileBuilder
    {
        private readonly Dictionary<string, int> files = new Dictionary<string, int>();
        private readonly List<string> storedLines = new List<string>();
        
        public int GetFileNumber(string filename)
        {
            if (filename == null)
                return 0;

            int result;
            if (files.TryGetValue(filename, out result) == false)
            {
                result = files.Count + 1;
                files[filename] = result;
            }
            return result;
        }

        public IEnumerable<string> StoredLines
        {
            get { return storedLines; }
        }

        public IDictionary<string, int> Files
        {
            get { return files; }
        }

        public void MarkAction(IOperand action, string name)
        {
            storedLines.Add(string.Format(
                ".DEBUG-ACTION {0},\"{1}\"",
                action,
                name));
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