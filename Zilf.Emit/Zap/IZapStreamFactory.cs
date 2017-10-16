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

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using JetBrains.Annotations;

namespace Zilf.Emit.Zap
{
    [ContractClass(typeof(IZapStreamFactoryContracts))]
    public interface IZapStreamFactory
    {
        [NotNull] Stream CreateMainStream();
        [NotNull] Stream CreateFrequentWordsStream();
        [NotNull] Stream CreateDataStream();
        [NotNull] Stream CreateStringStream();

        [NotNull] string GetMainFileName(bool withExt);
        [NotNull] string GetDataFileName(bool withExt);
        [NotNull] string GetFrequentWordsFileName(bool withExt);
        [NotNull] string GetStringFileName(bool withExt);

        bool FrequentWordsFileExists { get; }
    }

    [ContractClassFor(typeof(IZapStreamFactory))]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    abstract class IZapStreamFactoryContracts : IZapStreamFactory
    {
        public Stream CreateMainStream()
        {
            Contract.Ensures(Contract.Result<Stream>() != null);
            return default(Stream);
        }

        public Stream CreateFrequentWordsStream()
        {
            Contract.Ensures(Contract.Result<Stream>() != null);
            return default(Stream);
        }

        public Stream CreateDataStream()
        {
            Contract.Ensures(Contract.Result<Stream>() != null);
            return default(Stream);
        }

        public Stream CreateStringStream()
        {
            Contract.Ensures(Contract.Result<Stream>() != null);
            return default(Stream);
        }

        public string GetMainFileName(bool withExt)
        {
            return default(string);
        }

        public string GetDataFileName(bool withExt)
        {
            return default(string);
        }

        public string GetFrequentWordsFileName(bool withExt)
        {
            return default(string);
        }

        public string GetStringFileName(bool withExt)
        {
            return default(string);
        }

        public bool FrequentWordsFileExists => default(bool);
    }
}