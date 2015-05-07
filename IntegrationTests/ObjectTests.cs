using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace IntegrationTests
{
    [TestClass]
    public class ObjectTests
    {
        private static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        #region Object Numbering & Tree Ordering

        private string[] TreeImplications(string[] numbering, params string[][] chains)
        {
            Contract.Requires(numbering != null && numbering.Length > 0);
            Contract.Requires(Contract.ForAll(numbering, n => !string.IsNullOrWhiteSpace(n)));
            Contract.Requires(Contract.ForAll(chains, c => c.Length >= 2));

            var result = new List<string>();

            for (int i = 0; i < numbering.Length; i++)
            {
                result.Add(string.Format("<=? ,{0} {1}>", numbering[i], i + 1));
            }

            var heads = new HashSet<string>();

            if (chains != null)
            {
                foreach (var chain in chains)
                {
                    Contract.Assert(chain.Length >= 2);

                    heads.Add(chain[0]);
                    result.Add(string.Format("<=? <FIRST? ,{0}> ,{1}>", chain[0], chain[1]));

                    for (int i = 1; i < chain.Length - 1; i++)
                    {
                        result.Add(string.Format("<=? <NEXT? ,{0}> ,{1}>", chain[i], chain[i + 1]));
                    }

                    result.Add(string.Format("<NOT <NEXT? ,{0}>>", chain[chain.Length - 1]));
                }
            }

            foreach (var o in numbering)
            {
                if (!heads.Contains(o))
                {
                    result.Add(string.Format("<NOT <FIRST? ,{0}>>", o));
                }
            }

            return result.ToArray();
        }

        /* Default ordering:
         * 
         * Objects are numbered in reverse definition-or-mention order.
         * 
         * The object tree is traversed in reverse definition order (ignoring mere mentions),
         * except that the first child defined is the first child traversed (not the last).
         */

        [TestMethod]
        public void TestContents_DefaultOrder()
        {
            AssertGlobals(
                "<OBJECT RAINBOW>",
                "<OBJECT RED (IN RAINBOW)>",
                "<OBJECT YELLOW (IN RAINBOW)>",
                "<OBJECT GREEN (IN RAINBOW)>",
                "<OBJECT BLUE (IN RAINBOW)>")
                .Implies(TreeImplications(
                    new[] { "BLUE", "GREEN", "YELLOW", "RED", "RAINBOW" },
                    new[] { "RAINBOW", "RED", "BLUE", "GREEN", "YELLOW" }));
        }

        [TestMethod]
        public void TestHouse_DefaultOrder()
        {
            AssertGlobals(
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "BED", "BEDROOM", "LOCAL-GLOBALS", "CEILING", "FLOOR", "ROOMS", "MICROWAVE", "SINK", "KITCHEN", "FRIDGE" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_RoomsFirst()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-FIRST>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "KITCHEN", "BEDROOM", "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "FLOOR", "CEILING", "LOCAL-GLOBALS", "BED" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_RoomsAndLgsFirst()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-AND-LGS-FIRST>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "KITCHEN", "FLOOR", "CEILING", "BEDROOM", "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "LOCAL-GLOBALS", "BED" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_RoomsLast()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-LAST>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "FLOOR", "CEILING", "LOCAL-GLOBALS", "BED", "KITCHEN", "BEDROOM" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_Defined()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? DEFINED>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "FRIDGE", "SINK", "MICROWAVE", "KITCHEN", "FLOOR", "BEDROOM", "BED", "ROOMS", "LOCAL-GLOBALS", "CEILING" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        // TODO: tests for other <ORDER-OBJECTS? ...>


        /* <ORDER-TREE? REVERSE-DEFINED>:
         * 
         * The object tree is traversed in reverse definition order (with no exception for
         * the first defined child).
         */

        [TestMethod]
        public void TestContents_Tree_ReverseDefined()
        {
            AssertGlobals(
                "<ORDER-TREE? REVERSE-DEFINED>",
                "<OBJECT RAINBOW>",
                "<OBJECT RED (IN RAINBOW)>",
                "<OBJECT YELLOW (IN RAINBOW)>",
                "<OBJECT GREEN (IN RAINBOW)>",
                "<OBJECT BLUE (IN RAINBOW)>")
                .Implies(TreeImplications(
                    new[] { "BLUE", "GREEN", "YELLOW", "RED", "RAINBOW" },
                    new[] { "RAINBOW", "BLUE", "GREEN", "YELLOW", "RED" }));
        }

        [TestMethod]
        public void TestHouse_Tree_ReverseDefined()
        {
            AssertGlobals(
                "<ORDER-TREE? REVERSE-DEFINED>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "LOCAL-GLOBALS", "BED", "BEDROOM", "CEILING", "FLOOR", "ROOMS", "MICROWAVE", "SINK", "KITCHEN", "FRIDGE" },
                    new[] { "KITCHEN", "MICROWAVE", "SINK", "FRIDGE" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "BEDROOM", "KITCHEN" },
                    new[] { "LOCAL-GLOBALS", "CEILING", "FLOOR" }));
        }

        #endregion

        #region Attribute Numbering

        [TestMethod]
        public void TestFindbitsMustBeNonZero()
        {
            AssertGlobals(
                "<OBJECT FOO (FLAGS F1BIT F2BIT F3BIT F4BIT F5BIT F6BIT F7BIT F8BIT " +
                                   "F9BIT F10BIT F11BIT F12BIT F13BIT F14BIT F15BIT F16BIT " +
                                   "F17BIT F18BIT F19BIT F20BIT F21BIT F22BIT F23BIT F24BIT " +
                                   "F25BIT F26BIT F27BIT F28BIT F29BIT F30BIT F31BIT F32BIT)>",
                "<SYNTAX BAR OBJECT (FIND F1BIT) WITH OBJECT (FIND F2BIT) = V-BAR>",
                "<SYNTAX BAZ OBJECT (FIND F31BIT) WITH OBJECT (FIND F32BIT) = V-BAZ>",
                "<ROUTINE V-BAR () <>>",
                "<ROUTINE V-BAZ () <>>")
                .Implies(
                    "<NOT <0? ,F1BIT>>",
                    "<NOT <0? ,F2BIT>>",
                    "<NOT <0? ,F31BIT>>",
                    "<NOT <0? ,F32BIT>>");
        }

        [TestMethod]
        public void TestBitSynonym()
        {
            AssertRoutine("", "<AND <==? ,MAINBIT ,ALIASBIT> <FSET? ,FOO ,MAINBIT> <FSET? ,BAR ,ALIASBIT>>")
                .WithGlobal("<BIT-SYNONYM MAINBIT ALIASBIT>")
                .WithGlobal("<OBJECT FOO (FLAGS MAINBIT)>")
                .WithGlobal("<OBJECT BAR (FLAGS ALIASBIT)>")
                .GivesNumber("1");
        }

        #endregion

        #region PROPDEF/PROPSPEC

        [TestMethod]
        public void PROPDEF_Basic_Pattern_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FOOT INCHES:FIX = 2 <WORD .FEET> <BYTE .INCHES>)" +
                " (HEIGHT FEET:FIX FT INCHES:FIX = 2 <WORD .FEET> <BYTE .INCHES>)>",
                "<OBJECT GIANT (HEIGHT 10 FT 8)>")
                .Implies(
                    "<=? <GET <GETPT ,GIANT ,P?HEIGHT> 0> 10>",
                    "<=? <GETB <GETPT ,GIANT ,P?HEIGHT> 2> 8>");
        }

        [TestMethod]
        public void PROPDEF_OPT_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FT \"OPT\" INCHES:FIX = <WORD .FEET> <BYTE .INCHES>)>",
                "<OBJECT GIANT1 (HEIGHT 100 FT)>",
                "<OBJECT GIANT2 (HEIGHT 50 FT 11)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,GIANT1 ,P?HEIGHT>> 3>",
                    "<=? <GET <GETPT ,GIANT1 ,P?HEIGHT> 0> 100>",
                    "<=? <GETB <GETPT ,GIANT1 ,P?HEIGHT> 2> 0>",
                    "<=? <PTSIZE <GETPT ,GIANT2 ,P?HEIGHT>> 3>",
                    "<=? <GET <GETPT ,GIANT2 ,P?HEIGHT> 0> 50>",
                    "<=? <GETB <GETPT ,GIANT2 ,P?HEIGHT> 2> 11>");
        }

        [TestMethod]
        public void PROPDEF_MANY_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF TRANSLATE <> " +
                " (TRANSLATE \"MANY\" A:ATOM N:FIX = \"MANY\" <VOC .A BUZZ> <WORD .N>)>",
                "<OBJECT NUMBERS (TRANSLATE ONE 1 TWO 2)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,NUMBERS ,P?TRANSLATE>> 8>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 0> ,W?ONE>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 1> 1>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 2> ,W?TWO>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 3> 2>");
        }

        [TestMethod]
        public void PROPDEF_Constants_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FT INCHES:FIX = (HEIGHTSIZE 3) (H-FEET <WORD .FEET>) (H-INCHES <BYTE .INCHES>))>")
                .Implies(
                    "<=? ,HEIGHTSIZE 3>",
                    "<=? ,H-FEET 0>",
                    "<=? ,H-INCHES 2>");
        }

        [TestMethod]
        public void PROPDEF_For_DIRECTIONS_Should_Be_Used_For_All_Directions()
        {
            AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH GOES TO WOODS)>",
                "<OBJECT WOODS (NORTH GOES TO HOUSE)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,HOUSE ,P?SOUTH>> ,MY-UEXIT>",
                    "<=? <GETB <GETPT ,HOUSE ,P?SOUTH> ,MY-REXIT> ,WOODS>");
        }

        [TestMethod]
        public void Clearing_PROPSPEC_For_DIRECTIONS_Should_Override_Default_Patterns()
        {
            AssertGlobals(
                "<PUTPROP DIRECTIONS PROPSPEC>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH TO WOODS)>",
                "<OBJECT WOODS (NORTH TO HOUSE)>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void PROPDEF_For_DIRECTIONS_Can_Be_Used_For_Implicit_Directions()
        {
            AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (EAST GOES TO WOODS)>",
                "<OBJECT WOODS (WEST GOES TO HOUSE)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,HOUSE ,P?EAST>> ,MY-UEXIT>",
                    "<=? <GETB <GETPT ,HOUSE ,P?EAST> ,MY-REXIT> ,WOODS>",
                    "<BAND <GETB ,W?EAST 4> ,PS?DIRECTION>");
        }

        [TestMethod]
        public void Vocab_Created_By_PROPDEF_Should_Work_Correctly()
        {
            AssertGlobals(
                "<PROPDEF FOO <> (FOO A:ATOM = <VOC .A PREP>)>",
                "<DEFINE FOO-PROP (L) (<> <VOC \"FOO\" PREP>)>",
                "<OBJECT BAR (FOO FOO)>")
                .Implies(
                    "<=? <GETP ,BAR ,P?FOO> ,W?FOO>");
        }

        [TestMethod]
        public void Vocab_Created_By_PROPSPEC_Should_Work_Correctly()
        {
            AssertGlobals(
                "<PUTPROP FOO PROPSPEC FOO-PROP>",
                "<DEFINE FOO-PROP (L) (<> <VOC \"FOO\" PREP>)>",
                "<OBJECT BAR (FOO FOO)>")
                .Implies(
                    "<=? <GETP ,BAR ,P?FOO> ,W?FOO>");
        }

        #endregion
    }
}
