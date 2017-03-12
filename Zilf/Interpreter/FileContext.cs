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
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    class FileContext : IDisposable
    {
        public Context Context { get; }
        public FileContext Parent { get; }
        public string Path { get; }

        public FileFlags Flags { get; set; }
        public ZilList DefStructDefaults { get; set; }

        public FileContext(Context ctx, string path)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(path));

            Context = ctx;
            Parent = ctx.CurrentFile;
            Path = path;
        }

        public void Dispose()
        {
            if (this == Context.CurrentFile)
            {
                Context.PopFileContext();
            }
            else
            {
                throw new InvalidOperationException($"{nameof(FileContext)} being disposed must be at the top of the stack");
            }
        }
    }
}
