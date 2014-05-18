using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace IntegrationTests
{
    [TestClass]
    public class TreeTests
    {
        private static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

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

        [TestMethod]
        public void TestContents()
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
        public void TestHouse()
        {
            AssertGlobals(
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT FLOOR>",
                "<OBJECT CEILING>")
                .Implies(TreeImplications(
                    new[] { "BED", "BEDROOM", "CEILING", "FLOOR", "ROOMS", "MICROWAVE", "SINK", "KITCHEN", "FRIDGE" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" }));
        }

        // TODO: tests for <ORDER-OBJECTS? ...>
    }
}
