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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter")]
    public class CompilationFlagTests
    {
        [TestMethod]
        public void IN_ZILCH_Flag_Should_Be_Off_By_Default()
        {
            const string CODE1 = @"
<IFFLAG (IN-ZILCH <SETG FOO T>) (T <SETG FOO <>>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE1, ctx.FALSE);

            const string CODE2 = @"
<SETG BAR 123>
<IF-IN-ZILCH <SETG BAR T>>
<IFN-IN-ZILCH <SETG BAR <>>>
,BAR";

            TestHelpers.EvalAndAssert(ctx, CODE2, ctx.FALSE);
        }

        [TestMethod]
        public void IFFLAG_Should_Use_Default_When_Flag_Is_Off()
        {
            const string CODE = @"
<COMPILATION-FLAG MYFLAG <>>
<IFFLAG (MYFLAG <SETG FOO <>>) (T <SETG FOO T>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE, ctx.TRUE);
        }

        [TestMethod]
        public void COMPILATION_FLAG_DEFAULT_Should_Initialize_Value()
        {
            const string CODE = @"
<COMPILATION-FLAG-DEFAULT MYFLAG <>>
<IFFLAG (MYFLAG <SETG FOO <>>) (T <SETG FOO T>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE, ctx.TRUE);
        }

        [TestMethod]
        public void COMPILATION_FLAG_DEFAULT_Should_Not_Reinitialize_Value()
        {
            const string CODE = @"
<COMPILATION-FLAG MYFLAG T>
<COMPILATION-FLAG-DEFAULT MYFLAG <>>
<IFFLAG (MYFLAG <SETG FOO T>) (T <SETG FOO <>>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE, ctx.TRUE);
        }

        [TestMethod]
        public void COMPILATION_FLAG_VALUE_Should_Return_Value()
        {
            const string CODE = @"
<COMPILATION-FLAG MYFLAG 123>
<COMPILATION-FLAG-VALUE MYFLAG>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(123));
        }

        [TestMethod]
        public void COMPILATION_FLAG_VALUE_Should_Return_FALSE_For_Nonexistent_Flag()
        {
            const string CODE = "<COMPILATION-FLAG-VALUE ASDFGHJKL>";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE, ctx.FALSE);
        }

        [TestMethod]
        public void ZIP_OPTIONS_Should_Become_Compilation_Flags()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<IFFLAG (UNDO T) (ELSE <>)>", ctx.FALSE);

            TestHelpers.Evaluate(ctx, "<ZIP-OPTIONS UNDO>");

            TestHelpers.EvalAndAssert(ctx, "<IFFLAG (UNDO T) (ELSE <>)>", ctx.TRUE);
        }

        [TestMethod]
        public void Compilation_Flags_Work_Across_Packages()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, @"
<COMPILATION-FLAG OUTER-FLAG <>>
<PACKAGE ""FOO"">
<COMPILATION-FLAG FOO-FLAG <>>
<ENDPACKAGE>");

            TestHelpers.EvalAndAssert(ctx, "<COMPILATION-FLAG-VALUE FOO-FLAG>", ctx.FALSE);

            TestHelpers.Evaluate(ctx, @"<PACKAGE ""BAR"">");
            TestHelpers.EvalAndAssert(ctx, "<COMPILATION-FLAG-VALUE OUTER-FLAG>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<COMPILATION-FLAG-VALUE FOO-FLAG>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<IFFLAG (OUTER-FLAG 123) (FOO-FLAG 456) (T 789)>", new ZilFix(789));
        }
    }
}
