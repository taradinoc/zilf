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

using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace Zilf.Emit.Zap
{
    [ContractClass(typeof(IZapStreamFactoryContracts))]
    public interface IZapStreamFactory
    {
        Stream CreateMainStream();
        Stream CreateFrequentWordsStream();
        Stream CreateDataStream();
        Stream CreateStringStream();

        string GetMainFileName(bool withExt);
        string GetDataFileName(bool withExt);
        string GetFrequentWordsFileName(bool withExt);
        string GetStringFileName(bool withExt);

        bool FrequentWordsFileExists { get; }
    }

    [ContractClassFor(typeof(IZapStreamFactory))]
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
            throw new NotImplementedException();
        }

        public string GetDataFileName(bool withExt)
        {
            throw new NotImplementedException();
        }

        public string GetFrequentWordsFileName(bool withExt)
        {
            throw new NotImplementedException();
        }

        public string GetStringFileName(bool withExt)
        {
            throw new NotImplementedException();
        }

        public bool FrequentWordsFileExists
        {
            get { throw new NotImplementedException(); }
        }
    }
}