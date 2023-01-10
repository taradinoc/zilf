/* Copyright 2010-2023 Tara McGrew
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
using Zilf.Emit;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler.Builtins
{
#pragma warning disable IDE1006 // Naming Styles
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct PredCall
    {
        public Compilation cc { get; }
        public IRoutineBuilder rb { get; }
        public ZilForm form { get; }

        public ILabel label { get; }
        public bool polarity { get; }

        public PredCall(Compilation cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
            : this()
        {
            this.cc = cc;
            this.rb = rb;
            this.form = form;
            this.label = label;
            this.polarity = polarity;
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}