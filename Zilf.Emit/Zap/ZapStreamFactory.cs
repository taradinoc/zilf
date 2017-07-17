/* Copyright 2010-2017 Jesse McGrew
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

using System.IO;

namespace Zilf.Emit.Zap
{
    public class ZapStreamFactory : IZapStreamFactory
    {
        readonly string outFile;

        const string FrequentWordsSuffix = "_freq";
        const string DataSuffix = "_data";
        const string StringSuffix = "_str";

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