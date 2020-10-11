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
        [JetBrains.Annotations.NotNull]
        protected static GlobalsAssertionHelper AssertGlobals([ItemNotNull] [JetBrains.Annotations.NotNull] params string[] globals)
        {
            return new GlobalsAssertionHelper(globals);
        }

        [JetBrains.Annotations.NotNull]
        protected static RoutineAssertionHelper AssertRoutine([JetBrains.Annotations.NotNull] string argSpec, [JetBrains.Annotations.NotNull] string body)
        {
            return new RoutineAssertionHelper(argSpec, body);
        }

        [ItemNotNull]
        [JetBrains.Annotations.NotNull]
        protected static string[] TreeImplications([ItemNotNull] [JetBrains.Annotations.NotNull] string[] numbering, [ItemNotNull] [JetBrains.Annotations.NotNull] params string[][] chains)
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

        [JetBrains.Annotations.NotNull]
        protected static EntryPointAssertionHelper AssertEntryPoint([JetBrains.Annotations.NotNull] string argSpec, [JetBrains.Annotations.NotNull] string body)
        {
            return new EntryPointAssertionHelper(argSpec, body);
        }

        [JetBrains.Annotations.NotNull]
        protected static RawAssertionHelper AssertRaw([JetBrains.Annotations.NotNull] string code)
        {
            return new RawAssertionHelper(code);
        }

        [JetBrains.Annotations.NotNull]
        protected static ExprAssertionHelper AssertExpr([JetBrains.Annotations.NotNull] string expression)
        {
            return new ExprAssertionHelper(expression);
        }
    }
}