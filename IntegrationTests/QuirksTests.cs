/* Copyright 2010-2016 Jesse McGrew
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class QuirksTests
    {
        static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void TestGVALWithLocal()
        {
            AssertRoutine("\"AUX\" (X 5)", "<FOO ,X>")
                .WithGlobal("<ROUTINE FOO (A) .A>")
                .WithWarnings()
                .GivesNumber("5");
        }

        [TestMethod]
        public void TestLVALWithGlobal()
        {
            AssertRoutine("", "<FOO .X>")
                .WithGlobal("<GLOBAL X 5>")
                .WithGlobal("<ROUTINE FOO (A) .A>")
                .WithWarnings()
                .GivesNumber("5");
        }
    }
}
