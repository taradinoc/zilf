using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zapf;

namespace ZapfTests
{
    [TestClass]
    public class LabelConvergenceTests
    {
        private static string PaddingWords(int count)
        {
            if (count < 1)
                return "";

            var sb = new StringBuilder(".WORD 0");
            for (int i = 1; i < count; i++)
                sb.Append(",0");

            return sb.ToString();
        }

        private static string FormatAsRanges(IEnumerable<int> numbers)
        {
            int? last = null, rangeStart = null;
            var ranges = new List<string>();

            foreach (int i in numbers)
            {
                if (last == null)
                {
                    // first number starts a range
                    rangeStart = i;
                }
                else if (i == last + 1)
                {
                    // next number in sequence continues a range
                }
                else
                {
                    // terminate range and start a new one
                    if (rangeStart == last)
                        ranges.Add(rangeStart.ToString());
                    else
                        ranges.Add(string.Format("{0}-{1}", rangeStart, last));

                    rangeStart = i;
                }

                last = i;
            }

            if (last != null)
            {
                if (rangeStart == last)
                    ranges.Add(rangeStart.ToString());
                else
                    ranges.Add(string.Format("{0}-{1}", rangeStart, last));
            }

            return string.Join(", ", ranges);
        }

        [TestMethod]
        public void TestConstantTable1()
        {
            const string CodeTemplate = @"
	.NEW 5

	TABLE2=MYTABLE

{0}

MYTABLE::
	.WORD 0

	.FUNCT TEST-ROUTINE
	COPYT TABLE2,TABLE2,6
	RTRUE

	.FUNCT GO
START::
	CALL1 TEST-ROUTINE >STACK
	PRINTN STACK
	QUIT

	.END";

            var failures = new List<int>();

            for (int i = 0; i <= 256; i++)
            {
                var code = string.Format(CodeTemplate, PaddingWords(i));
                if (!TestHelper.Assemble(code))
                    failures.Add(i);
            }

            if (failures.Count > 0)
            {
                Assert.Fail("Could not assemble with padding word counts: {0}",
                    FormatAsRanges(failures));
            }
        }

        [TestMethod]
        public void TestConstantTable2()
        {
            const string SCode = @"
	.FSTR FSTR?DUMMY,""""
WORDS::
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY
	FSTR?DUMMY

	TBL=T?0

GLOBAL:: .TABLE
	.ENDT

OBJECT:: .TABLE
	.ENDT

T?0:: .TABLE 6
	.WORD 12345,123,45
	.ENDT

IMPURE::

VOCAB:: .TABLE
	.ENDT

ENDLOD::

	.FUNCT TEST-IMPLIES,FAILS
	GET TBL,0 >STACK
	EQUAL? STACK,12345 /?L3
	INC 'FAILS
?L3:	GETB TBL,2 >STACK
	EQUAL? STACK,123 /?L6
	INC 'FAILS
?L6:	GETB TBL,3 >STACK
	EQUAL? STACK,45 /?L9
	INC 'FAILS
?L9:	RETURN FAILS

	.FUNCT GO
START::
	CALL TEST-IMPLIES >STACK
	ZERO? STACK \?L1
?L1:	QUIT

	.END";

            Assert.IsTrue(TestHelper.Assemble(SCode), "Failed to assemble");
        }

        [TestMethod]
        public void TestConstantTable3()
        {
            const string SCode = @"
    .NEW 5
    .WORD EXTAB

    EXTAB=T?EXTAB

T?EXTAB:: .TABLE
    .WORD 0
    .ENDT

    .END";

            Assert.IsTrue(TestHelper.Assemble(SCode), "Failed to assemble");
        }
    }
}
