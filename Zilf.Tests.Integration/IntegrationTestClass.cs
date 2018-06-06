/* Copyright 2010-2018 Jesse McGrew
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass]
    public abstract class IntegrationTestClass
    {
        [NotNull]
        protected static GlobalsAssertionHelper AssertGlobals([ItemNotNull] [NotNull] params string[] globals)
        {
            return new GlobalsAssertionHelper(globals);
        }

        [NotNull]
        protected static RoutineAssertionHelper AssertRoutine([NotNull] string argSpec, [NotNull] string body)
        {
            return new RoutineAssertionHelper(argSpec, body);
        }

        [ItemNotNull]
        [NotNull]
        protected static string[] TreeImplications([ItemNotNull] [NotNull] string[] numbering, [ItemNotNull] [NotNull] params string[][] chains)
        {
            var result = new List<string>();

            for (int i = 0; i < numbering.Length; i++)
            {
                result.Add($"<=? ,{numbering[i]} {i + 1}>");
            }

            var heads = new HashSet<string>();

            foreach (var chain in chains)
            {
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
            return new EntryPointAssertionHelper(argSpec, body);
        }

        [NotNull]
        protected static RawAssertionHelper AssertRaw([NotNull] string code)
        {
            return new RawAssertionHelper(code);
        }

        [NotNull]
        protected static ExprAssertionHelper AssertExpr([NotNull] string expression)
        {
            return new ExprAssertionHelper(expression);
        }
    }
}