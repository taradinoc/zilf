using System;

namespace Zilf.Language
{
    abstract class ZilError : Exception
    {
        private ISourceLine src;

        public ZilError(string message)
            : base(message)
        {
        }

        public ZilError(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ZilError(ISourceLine src, string message)
            : base(message)
        {
            this.src = src;
        }

        public ZilError(ISourceLine src, string func, int minArgs, int maxArgs)
            : this(src, ArgCountMsg(func, minArgs, maxArgs))
        {
        }

        public ISourceLine SourceLine
        {
            get { return src; }
            set { src = value; }
        }

        public string SourcePrefix
        {
            get
            {
                if (src == null || src.SourceInfo == null)
                    return "";
                else
                    return src.SourceInfo + ": ";
            }
        }

        public static string ArgCountMsg(string func, int min, int max)
        {
            if (min == max)
                return string.Format("{0}: expected {1} arg{2}", func, min, min == 1 ? "" : "s");
            else if (min == 0)
                return string.Format("{0}: expected at most {1} arg{2}", func, max, max == 1 ? "" : "s");
            else if (max == 0)
                return string.Format("{0}: expected at least {1} arg{2}", func, min, min == 1 ? "" : "s");
            else
                return string.Format("{0}: expected {1} to {2} args", func, min, max);
        }
    }
}