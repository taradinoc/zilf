using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class FlowControlTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        #region DO

        [TestMethod]
        public void TestDO_Up_Fixes()
        {
            AssertRoutine("", "<DO (I 1 5) <PRINTN .I> <CRLF>>")
                .Outputs("1\n2\n3\n4\n5\n");
        }

        [TestMethod]
        public void TestDO_Down_Fixes()
        {
            AssertRoutine("", "<DO (I 5 1) <PRINTN .I> <CRLF>>")
                .Outputs("5\n4\n3\n2\n1\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_By2()
        {
            AssertRoutine("", "<DO (I 1 5 2) <PRINTN .I> <CRLF>>")
                .Outputs("1\n3\n5\n");
        }

        [TestMethod]
        public void TestDO_Down_Fixes_By2()
        {
            AssertRoutine("", "<DO (I 5 1 -2) <PRINTN .I> <CRLF>>")
                .Outputs("5\n3\n1\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_ByN()
        {
            AssertRoutine("\"AUX\" (N 2)", "<DO (I 1 5 .N) <PRINTN .I> <CRLF>>")
                .Outputs("1\n3\n5\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_CalculateInc()
        {
            AssertRoutine("", "<DO (I 1 16 <* 2 .I>) <PRINTN .I> <CRLF>>")
                .Outputs("1\n2\n4\n8\n16\n");
        }

        [TestMethod]
        public void TestDO_Up_Forms()
        {
            AssertRoutine("", "<DO (I <FOO> <BAR .I>) <PRINTN .I> <CRLF>>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"FOO\"> <CRLF> 7>")
                .WithGlobal("<ROUTINE BAR (I) <PRINTI \"BAR\"> <CRLF> <G? .I 9>>")
                .Outputs("FOO\nBAR\n7\nBAR\n8\nBAR\n9\nBAR\n");
        }

        [TestMethod]
        public void TestDO_Result()
        {
            AssertRoutine("", "<DO (I 1 10) <>>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void TestDO_Result_RETURN()
        {
            AssertRoutine("", "<DO (I 1 10) <COND (<==? .I 5> <RETURN <* .I 3>>)>>")
                .GivesNumber("15");
        }

        [TestMethod]
        public void TestDO_EndClause()
        {
            AssertRoutine("",
                "<DO (I 1 4) " +
                "  (<TELL \"rock!\">) " +
                "  <TELL N .I> " +
                "  <COND (<G=? .I 3> <TELL \" o'clock\">)> " +
                "  <TELL \", \">>")
                .Outputs("1, 2, 3 o'clock, 4 o'clock, rock!");
        }

        #endregion

        #region MAP-CONTENTS

        [TestMethod]
        public void TestMAP_CONTENTS_Basic()
        {
            AssertRoutine("", "<MAP-CONTENTS (F ,TABLE) <PRINTD .F> <CRLF>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (DESC \"apple\")>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (DESC \"cherry\")>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (DESC \"banana\")>")
                .Outputs("apple\nbanana\ncherry\n");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithNext()
        {
            AssertRoutine("", "<MAP-CONTENTS (F N ,TABLE) <REMOVE .F> <PRINTD .F> <PRINTI \", \"> <PRINTD? .N> <CRLF>>")
               .WithGlobal("<ROUTINE PRINTD? (OBJ) <COND (.OBJ <PRINTD .OBJ>) (ELSE <PRINTI \"nothing\">)>>")
               .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
               .WithGlobal("<OBJECT APPLE (IN TABLE) (DESC \"apple\")>")
               .WithGlobal("<OBJECT CHERRY (IN TABLE) (DESC \"cherry\")>")
               .WithGlobal("<OBJECT BANANA (IN TABLE) (DESC \"banana\")>")
               .Outputs("apple, banana\nbanana, cherry\ncherry, nothing\n");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithEnd()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F ,TABLE) (END <RETURN .SUM>) <SET SUM <+ .SUM <GETP .F ,P?PRICE>>>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (PRICE 1)>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (PRICE 2)>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (PRICE 3)>")
                .GivesNumber("6");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithEnd_Empty()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F ,TABLE) (END <RETURN 42>) <RFALSE>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .GivesNumber("42");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithNextAndEnd()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F N ,TABLE) (END <RETURN .SUM>) <REMOVE .F> <SET SUM <+ .SUM <GETP .F ,P?PRICE>>>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (PRICE 1)>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (PRICE 2)>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (PRICE 3)>")
                .GivesNumber("6");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithNextAndEnd_Empty()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F N ,TABLE) (END <RETURN 42>) <RFALSE>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .GivesNumber("42");
        }

        #endregion

        #region MAP-DIRECTIONS

        [TestMethod]
        public void TestMAP_DIRECTIONS()
        {
            AssertRoutine("", "<MAP-DIRECTIONS (D P ,CENTER) <TELL N .D \" \" D <GETB .P ,REXIT> CR>>")
                .WithGlobal("<DIRECTIONS NORTH SOUTH EAST WEST>")
                .WithGlobal("<OBJECT CENTER (NORTH TO N-ROOM) (WEST TO W-ROOM)>")
                .WithGlobal("<OBJECT N-ROOM (DESC \"north room\")>")
                .WithGlobal("<OBJECT W-ROOM (DESC \"west room\")>")
                .InV3()
                .Outputs("31 north room\n28 west room\n");
        }

        #endregion
    }
}
