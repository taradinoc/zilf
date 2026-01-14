/* Copyright 2010-2025 Tara McGrew
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
using System.Threading.Tasks;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Code Generation")]
    public class PruningTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task Unused_Routines_Should_Warn_And_Be_Pruned()
        {
            // Define an extra routine FOO that is never referenced; it should be warned and not emitted.
            await AssertRoutine("", "<PRINTI \"hi\">")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"never\">>")
                .WithWarnings("ZIL0213") // routine unused
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");
        }

        [TestMethod]
        public async Task Unused_Routines_Should_Not_Warn_When_Flagged()
        {
            // ...but they should still be pruned
            await AssertRoutine("", "<PRINTI \"hi\">")
                .WithGlobal("<FILE-FLAGS UNUSED-ROUTINES?>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"never\">>")
                .WithoutWarnings()
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");

            await AssertRoutine("", "<PRINTI \"hi\">")
                .WithGlobal("<ROUTINE-FLAGS UNUSED?>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"never\">>")
                .WithoutWarnings()
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");
        }

        [TestMethod]
        public async Task Unused_Routines_Should_Not_Be_Pruned_When_Flagged()
        {
            // ...but they should still warn
            await AssertRoutine("", "<PRINTI \"hi\">")
                .WithGlobal("<FILE-FLAGS KEEP-ROUTINES?>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"never\">>")
                .WithoutWarnings("ZIL0213")
                .GeneratesCodeMatchingAsync(@"\.FUNCT\s+FOO\b");

            await AssertRoutine("", "<PRINTI \"hi\">")
                .WithGlobal("<ROUTINE-FLAGS KEEP?>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"never\">>")
                .WithoutWarnings("ZIL0213")
                .GeneratesCodeMatchingAsync(@"\.FUNCT\s+FOO\b");
        }

        [TestMethod]
        public async Task Unused_Routines_Should_Not_Warn_Or_Be_Pruned_When_Flagged()
        {
            await AssertRoutine("", "<PRINTI \"hi\">")
                .WithGlobal("<FILE-FLAGS KEEP-ROUTINES? UNUSED-ROUTINES?>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"never\">>")
                .WithoutWarnings("ZIL0213")
                .GeneratesCodeMatchingAsync(@"\.FUNCT\s+FOO\b");

            await AssertRoutine("", "<PRINTI \"hi\">")
                .WithGlobal("<ROUTINE-FLAGS KEEP? UNUSED?>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"never\">>")
                .WithoutWarnings("ZIL0213")
                .GeneratesCodeMatchingAsync(@"\.FUNCT\s+FOO\b");
        }

        [TestMethod]
        public async Task Macro_Referenced_Routines_Should_Be_Counted_As_Used()
        {
            // FOO is only referenced via macro expansion inside BAR; it should be kept and no warning produced.
            await AssertRoutine("", "<BAR>")
                .WithGlobal("<DEFMAC CALL-FOO () <FORM FOO>>")
                .WithGlobal("<ROUTINE FOO () <RTRUE>>")
                .WithGlobal("<ROUTINE BAR () <CALL-FOO>>")
                .WithoutWarnings()
                .GeneratesCodeMatchingAsync(@"\.FUNCT\s+FOO\b");
        }

        [TestMethod]
        public async Task Routine_References_Inside_Failed_Version_Checks_Should_Not_Count_As_Uses()
        {
            await AssertRoutine("", "<VERSION? (ZIP <FOO>) (ELSE <>)>")
                .InV5()
                .WithGlobal("<ROUTINE FOO () <RTRUE>>")
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");

            await AssertRoutine("", "<MAYBE <FOO>>")
                .WithGlobal("<DEFMAC MAYBE ('A) <FORM VERSION? <LIST ZIP .A>>>")
                .WithGlobal("<ROUTINE FOO () <RTRUE>>")
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT \s+FOO\b");
        }

        [TestMethod]
        public async Task Routine_References_Inside_Failed_Compilation_Flag_Checks_Should_Not_Count_As_Uses()
        {
            await AssertRoutine("", "<IFFLAG (WANT-FOO <FOO>) (ELSE <>)>")
                .WithGlobal("<COMPILATION-FLAG WANT-FOO <>>")
                .WithGlobal("<ROUTINE FOO () <RTRUE>>")
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");

            await AssertRoutine("", "<IF-WANT-FOO <FOO>>")
                .WithGlobal("<COMPILATION-FLAG WANT-FOO <>>")
                .WithGlobal("<ROUTINE FOO () <RTRUE>>")
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");

            await AssertRoutine("", "<MAYBE <FOO>>")
                .WithGlobal("<DEFMAC MAYBE ('A) <FORM IFFLAG <LIST WANT-FOO .A>>>")
                .WithGlobal("<ROUTINE FOO () <RTRUE>>")
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT \s+FOO\b");
        }

        [TestMethod]
        public async Task Routine_References_Ignored_By_Macros_Should_Not_Count_As_Uses()
        {
            // a macro that takes code at the call site but does nothing with it
            await AssertRoutine("", "<PROB 25 <FOO>>")
                .WithGlobal("<DEFMAC PROB ('A 'B) <FORM L? '<RANDOM 100> .A>>")
                .WithGlobal("<ROUTINE FOO () 0>")
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");

            // a macro that mentions a routine in its definition but doesn't compile it
            await AssertRoutine("", "<PROB 25>")
                .WithGlobal("<DEFMAC PROB ('A) <COND (,LUCKY T) (ELSE <FORM L? '<FOO> .A>)>>")
                .WithGlobal("<ROUTINE FOO () <RANDOM 100>>")
                .WithGlobal("<SETG LUCKY T>")
                .GeneratesCodeNotMatchingAsync(@"\.FUNCT\s+FOO\b");
        }
    }
}
