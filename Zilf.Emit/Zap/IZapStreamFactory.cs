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