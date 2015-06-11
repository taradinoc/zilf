/* Copyright 2010, 2015 Jesse McGrew
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