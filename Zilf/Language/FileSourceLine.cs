using System.Diagnostics.Contracts;

namespace Zilf.Language
{
    sealed class FileSourceLine : ISourceLine
    {
        private readonly string filename;
        private readonly int line;

        public FileSourceLine(string filename, int line)
        {
            Contract.Requires(filename != null);

            this.filename = filename;
            this.line = line;
        }

        public string SourceInfo
        {
            get { return string.Format("{0}:{1}", filename, line); }
        }

        public string FileName
        {
            get { return filename; }
        }

        public int Line
        {
            get { return line; }
        }
    }
}