namespace Zilf.Emit
{
    public struct DebugLineRef
    {
        public readonly string File;
        public readonly int Line;
        public readonly int Column;

        public DebugLineRef(string file, int line, int column)
        {
            File = file;
            Line = line;
            Column = column;
        }
    }
}