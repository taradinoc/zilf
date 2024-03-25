/* Copyright 2010-2024 Tara McGrew
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

namespace ZilfSourceGenerators.Test
{
    [TestClass]
    public class DistinguisherTests
    {
        private static void AssertNames<TProp, TItem>(
            Distinguisher<TProp, TItem> distinguisher, (TItem, TProp)[] expectedNames)
            where TProp : IEquatable<TProp>
            where TItem : notnull
        {
            var actualNames = distinguisher.GetUniqueNames().ToArray();
            CollectionAssert.AreEquivalent(expectedNames, actualNames);
        }

        [TestMethod]
        public void TestDistinguisher()
        {
            /// <item>Start with an empty distinguisher.</item>
            
            var distinguisher = new Distinguisher<string, string>();

            /// <item>Add Item1 with properties A, B, C. Its unique name is "A".</item>
            
            distinguisher.Add(["A", "B", "C"], "Item1");

            AssertNames(
                distinguisher,
                [("A", "Item1")]);

            /// <item>Add Item2 with properties A, B, D. This changes the name of Item1 to "A_C",
            /// and assigns Item2 the name "A_D", because the second properties are identical.</item>

            distinguisher.Add(["A", "B", "D"], "Item2");

            CollectionAssert.AreEquivalent(
                distinguisher.GetUniqueNames().ToArray(),
                ((string, string)[])[
                    ("A_C", "Item1"),
                    ("A_D", "Item2")
                ]);

            /// <item>Add Item3 with properties A, B. Item1 retains the name "A_C", Item2 retains
            /// the name "A_D", and Item3 is assigned the name "A".</item>

            distinguisher.Add(["A", "B"], "Item3");

            CollectionAssert.AreEquivalent(
                distinguisher.GetUniqueNames().ToArray(),
                ((string, string)[])[
                    ("A_C", "Item1"),
                    ("A_D", "Item2"),
                    ("A", "Item3")
                ]);

            /// <item>Add Item4 with properties B, A, C. All existing items retain their names, and
            /// Item4 is assigned the name "B", because its first property is enough to
            /// distinguish it from the other items.</item>

            distinguisher.Add(["B", "A", "C"], "Item4");

            CollectionAssert.AreEquivalent(
                distinguisher.GetUniqueNames().ToArray(),
                ((string, string)[])[
                    ("A_C", "Item1"),
                    ("A_D", "Item2"),
                    ("A", "Item3"),
                    ("B", "Item4")
                ]);

            /// <item>Add Item5 with properties A. Item5 is assigned the name "A". Since the second
            /// property is now needed to distinguish it from some of the existing items, this
            /// changes the name of Item1 to "A_B_C", Item2 to "A_B_D", and Item3 to "A_B".</item>

            distinguisher.Add(["A"], "Item5");

            CollectionAssert.AreEquivalent(
                distinguisher.GetUniqueNames().ToArray(),
                ((string, string)[])[
                    ("A_B_C", "Item1"),
                    ("A_B_D", "Item2"),
                    ("A_B", "Item3"),
                    ("B", "Item4"),
                    ("A", "Item5")
                ]);
        }
    }
}
