namespace Zilf.Emit
{
    public struct DebugLineRef
    {
        public string File;
        public int Line;
        public int Column;

        public DebugLineRef(string file, int line, int column)
        {
            this.File = file;
            this.Line = line;
            this.Column = column;
        }
    }
}