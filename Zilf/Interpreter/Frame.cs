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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    class Frame : IDisposable
    {
        public Context Context { get; }
        public Frame Parent { get; }
        public ZilForm CallingForm { get; }
        public ISourceLine SourceLine { get; }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Context != null);
            Contract.Invariant(SourceLine != null);
        }

        public Frame(Context ctx, ZilForm callingForm)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(callingForm != null);
            Contract.Ensures(CallingForm != null);

            Context = ctx;
            Parent = ctx.TopFrame;
            CallingForm = callingForm;
            SourceLine = callingForm.SourceLine;
        }

        public Frame(Context ctx, ISourceLine sourceLine)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(sourceLine != null);
            Contract.Ensures(CallingForm == null);

            Context = ctx;
            Parent = ctx.TopFrame;
            SourceLine = sourceLine;
        }

        public void Dispose()
        {
            if (this == Context.TopFrame)
            {
                Context.PopFrame();
            }
            else
            {
                throw new InvalidOperationException($"{nameof(Frame)} being disposed must be at the top of the stack");
            }
        }
    }
}
