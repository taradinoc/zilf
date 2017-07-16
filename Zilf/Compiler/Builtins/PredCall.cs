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
using Zilf.Emit;
using Zilf.Interpreter.Values;
using JetBrains.Annotations;
using System.Diagnostics;

namespace Zilf.Compiler.Builtins
{
#pragma warning disable IDE1006 // Naming Styles
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct PredCall
    {
        [NotNull]
        public Compilation cc { get; }
        [NotNull]
        public IRoutineBuilder rb { get; }
        [NotNull]
        public ZilForm form { get; }

        [NotNull]
        public ILabel label { get; }
        public bool polarity { get; }

        public PredCall([NotNull] Compilation cc, [NotNull] IRoutineBuilder rb, [NotNull] ZilForm form, [NotNull] ILabel label, bool polarity)
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

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ContractInvariantMethod]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant(cc != null);
            Contract.Invariant(rb != null);
            Contract.Invariant(form != null);
            Contract.Invariant(label != null);
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}