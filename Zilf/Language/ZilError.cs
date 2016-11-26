/* Copyright 2010, 2016 Jesse McGrew
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

namespace Zilf.Language
{
    abstract class ZilError : Exception
    {
        public ISourceLine SourceLine { get; set; }

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
            this.SourceLine = src;
        }

        public ZilError(ISourceLine src, string func, int minArgs, int maxArgs)
            : this(src, ArgCountMsg(func, minArgs, maxArgs))
        {
        }

        public string SourcePrefix
        {
            get
            {
                if (SourceLine == null || SourceLine.SourceInfo == null)
                    return "";
                else
                    return SourceLine.SourceInfo + ": ";
            }
        }

        public static string ArgCountMsg(string func, int min, int max, string argName = "arg")
        {
            if (min == max)
                return string.Format("{0}: expected {1} {2}{3}", func, min, argName, min == 1 ? "" : "s");
            else if (min == 0)
                return string.Format("{0}: expected at most {1} {2}{3}", func, max, argName, max == 1 ? "" : "s");
            else if (max == 0)
                return string.Format("{0}: expected at least {1} {2}{3}", func, min, argName, min == 1 ? "" : "s");
            else
                return string.Format("{0}: expected {1} to {2} {3}s", func, min, max, argName);
        }
    }
}