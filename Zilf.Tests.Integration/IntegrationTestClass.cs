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

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass]
    public abstract class IntegrationTestClass
    {
        [NotNull]
        protected static GlobalsAssertionHelper AssertGlobals([ItemNotNull] [NotNull] params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        [NotNull]
        protected static RoutineAssertionHelper AssertRoutine([NotNull] string argSpec, [NotNull] string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [ItemNotNull]
        [NotNull]
        protected static string[] TreeImplications([ItemNotNull] [NotNull] string[] numbering, [ItemNotNull] [NotNull] params string[][] chains)
        {
            Contract.Requires(numbering != null && numbering.Length > 0);
            Contract.Requires(Contract.ForAll(numbering, n => !string.IsNullOrWhiteSpace(n)));
            Contract.Requires(Contract.ForAll(chains, c => c.Length >= 2));

            var result = new List<string>();

            for (int i = 0; i < numbering.Length; i++)
            {
                result.Add($"<=? ,{numbering[i]} {i + 1}>");
            }

            var heads = new HashSet<string>();

            foreach (var chain in chains)
            {
                Contract.Assert(chain.Length >= 2);

                heads.Add(chain[0]);
                result.Add($"<=? <FIRST? ,{chain[0]}> ,{chain[1]}>");

                for (int i = 1; i < chain.Length - 1; i++)
                {
                    result.Add($"<=? <NEXT? ,{chain[i]}> ,{chain[i + 1]}>");
                }

                result.Add($"<NOT <NEXT? ,{chain[chain.Length - 1]}>>");
            }

            foreach (var o in numbering)
            {
                if (!heads.Contains(o))
                {
                    result.Add($"<NOT <FIRST? ,{o}>>");
                }
            }

            return result.ToArray();
        }

        [NotNull]
        protected static EntryPointAssertionHelper AssertEntryPoint([NotNull] string argSpec, [NotNull] string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new EntryPointAssertionHelper(argSpec, body);
        }

        [NotNull]
        protected static RawAssertionHelper AssertRaw([NotNull] string code)
        {
            Contract.Requires(!string.IsNullOrEmpty(code));
            Contract.Ensures(Contract.Result<RawAssertionHelper>() != null);

            return new RawAssertionHelper(code);
        }

        [NotNull]
        protected static ExprAssertionHelper AssertExpr([NotNull] string expression)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(expression));
            Contract.Ensures(Contract.Result<ExprAssertionHelper>() != null);

            return new ExprAssertionHelper(expression);
        }
    }
}