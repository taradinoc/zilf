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
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler.Builtins
{
    struct PredCall
    {
        public CompileCtx cc { get; private set; }
        public IRoutineBuilder rb { get; private set; }
        public ZilForm form { get; private set; }

        public ILabel label { get; private set; }
        public bool polarity { get; private set; }

        public PredCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
            : this()
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(label != null);

            this.cc = cc;
            this.rb = rb;
            this.form = form;
            this.label = label;
            this.polarity = polarity;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(cc != null);
            Contract.Invariant(rb != null);
            Contract.Invariant(form != null);
            Contract.Invariant(label != null);
        }
    }
}