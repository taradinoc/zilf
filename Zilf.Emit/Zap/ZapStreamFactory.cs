using System.IO;

namespace Zilf.Emit.Zap
{
    public class ZapStreamFactory : IZapStreamFactory
    {
        private readonly string outFile;

        private const string FrequentWordsSuffix = "_freq";
        private const string DataSuffix = "_data";
        private const string StringSuffix = "_str";

        public ZapStreamFactory(string outFile)
        {
            this.outFile = outFile;
        }

        #region IZapStreamFactory Members

        public Stream CreateMainStream()
        {
            return new FileStream(outFile, FileMode.Create, FileAccess.Write);
        }

        public Stream CreateFrequentWordsStream()
        {
            return new FileStream(
                Path.GetFileNameWithoutExtension(outFile) + FrequentWordsSuffix + Path.GetExtension(outFile),
                FileMode.Create,
                FileAccess.Write);
        }

        public Stream CreateDataStream()
        {
            return new FileStream(
                Path.GetFileNameWithoutExtension(outFile) + DataSuffix + Path.GetExtension(outFile),
                FileMode.Create,
                FileAccess.Write);
        }

        public Stream CreateStringStream()
        {
            return new FileStream(
                Path.GetFileNameWithoutExtension(outFile) + StringSuffix + Path.GetExtension(outFile),
                FileMode.Create,
                FileAccess.Write);
        }

        public string GetMainFileName(bool withExt)
        {
            return withExt ? Path.GetFileName(outFile) : Path.GetFileNameWithoutExtension(outFile);
        }

        public string GetDataFileName(bool withExt)
        {
            var fn = Path.GetFileNameWithoutExtension(outFile) + DataSuffix;
            return withExt ? fn + Path.GetExtension(outFile) : fn;
        }

        public string GetFrequentWordsFileName(bool withExt)
        {
            var fn = Path.GetFileNameWithoutExtension(outFile) + FrequentWordsSuffix;
            return withExt ? fn + Path.GetExtension(outFile) : fn;
        }

        public string GetStringFileName(bool withExt)
        {
            var fn = Path.GetFileNameWithoutExtension(outFile) + StringSuffix;
            return withExt ? fn + Path.GetExtension(outFile) : fn;
        }

        public bool FrequentWordsFileExists
        {
            get
            {
                var fn = GetFrequentWordsFileName(true);
                return File.Exists(fn) || File.Exists(Path.ChangeExtension(fn, ".xzap"));
            }
        }

        #endregion
    }
}