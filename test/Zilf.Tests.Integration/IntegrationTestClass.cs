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

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass]
    public abstract class IntegrationTestClass
    {
        protected static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            return new GlobalsAssertionHelper(globals);
        }

        protected static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            return new RoutineAssertionHelper(argSpec, body);
        }

        protected static string[] TreeImplications(string[] numbering, params string[][] chains)
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

                result.Add($"<NOT <NEXT? ,{chain[^1]}>>");
            }

            foreach (var o in numbering)
            {
                if (!heads.Contains(o))
                {
                    result.Add($"<NOT <FIRST? ,{o}>>");
                }
            }

            return [.. result];
        }

        protected static EntryPointAssertionHelper AssertEntryPoint(string argSpec, string body)
        {
            return new EntryPointAssertionHelper(argSpec, body);
        }

        protected static RawAssertionHelper AssertRaw(string code)
        {
            return new RawAssertionHelper(code);
        }

        protected static ExprAssertionHelper AssertExpr(string expression)
        {
            return new ExprAssertionHelper(expression);
        }
    }
}