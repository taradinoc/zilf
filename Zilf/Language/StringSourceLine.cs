namespace Zilf.Language
{
    sealed class StringSourceLine : ISourceLine
    {
        private readonly string info;

        public StringSourceLine(string info)
        {
            this.info = info;
        }

        public string SourceInfo
        {
            get { return info; }
        }
    }
}