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

extern alias JBA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using JBA::JetBrains.Annotations;

namespace IntegrationTests
{
    [TestClass, TestCategory("Compiler"), TestCategory("Macros")]
    public class MacroTests
    {
        [NotNull]
        static RoutineAssertionHelper AssertRoutine([NotNull] string argSpec, [NotNull] string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void SPLICEs_Should_Work_Inside_Routines()
        {
            // void context
            AssertRoutine("", "<VARIOUS-THINGS> T")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(<TELL \"hello\"> <TELL CR> <TELL \"world\">) SPLICE>>")
                .Outputs("hello\nworld");

            // value context
            AssertRoutine("", "<VARIOUS-THINGS>")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(123 456) SPLICE>>")
                .GivesNumber("456");
        }

        [TestMethod]
        public void Macro_Call_With_Wrong_Argument_Count_Should_Raise_An_Error()
        {
            AssertRoutine("\"AUX\" S", "<SET S <FOO A>>")
                .WithGlobal("<DEFMAC FOO ('X 'Y 'Z) <FORM TELL \"hello world\" CR>>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void Macros_Can_Define_Globals_Inside_Routines()
        {
            AssertRoutine("", "<PRINTN <MAKE-GLOBAL 123>>")
                .WithGlobal("<DEFMAC MAKE-GLOBAL (N) <EVAL <FORM GLOBAL NEW-GLOBAL .N>> ',NEW-GLOBAL>")
                .Outputs("123");
        }

        [TestMethod]
        public void Macros_Can_Be_Used_In_Local_Initializers()
        {
            AssertRoutine("\"AUX\" (X <MY-VALUE>)", ".X")
                .WithGlobal("<DEFMAC MY-VALUE () 123>")
                .GivesNumber("123");
        }

    }
}
