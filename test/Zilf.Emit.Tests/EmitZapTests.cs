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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Globalization;
using System.IO;
using Zilf.Emit.Zap;

namespace Zilf.Emit.Tests
{
    [TestClass, TestCategory("Compiler")]
    public class EmitZapTests
    {
        MockRepository mockRepository;
        Mock<IZapStreamFactory> mockStreamFactory;

        [TestInitialize]
        public void Initialize()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockStreamFactory = mockRepository.Create<IZapStreamFactory>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            mockRepository.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "zversion 0 should be rejected")]
        public void Ctor_Should_Reject_Low_Zversion()
        {
            _ = new GameBuilder(0, mockStreamFactory.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "zversion 9 should be rejected")]
        public void Ctor_Should_Reject_High_Zversion()
        {
            _ = new GameBuilder(9, mockStreamFactory.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "null streamfactory should be rejected")]
        public void Ctor_Should_Reject_Null_StreamFactory()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _ = new GameBuilder(5, null);
        }

        [TestMethod]
        public void Numeric_Operand_Should_Use_ASCII_Minus_Ignoring_Current_Culture()
        {
            mockStreamFactory.Setup(m => m.CreateMainStream()).Returns(new MemoryStream());
            mockStreamFactory.Setup(m => m.GetFrequentWordsFileName(false)).Returns("freq.zap");
            mockStreamFactory.Setup(m => m.GetDataFileName(false)).Returns("data.zap");

            var customCulture = (CultureInfo)CultureInfo.GetCultureInfo("sv-SE").Clone();
            customCulture.NumberFormat.NegativeSign = "\u2212";

            using var scope = new CultureScope(customCulture);
            using var builder = new GameBuilder(5, mockStreamFactory.Object);
            var text = builder.MakeOperand(-1).ToString();

            Assert.AreEqual("-1", text);
        }

        sealed class CultureScope : IDisposable
        {
            readonly CultureInfo originalCulture;
            readonly CultureInfo originalUiCulture;

            public CultureScope(CultureInfo culture)
            {
                originalCulture = CultureInfo.CurrentCulture;
                originalUiCulture = CultureInfo.CurrentUICulture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }

            public void Dispose()
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }
}
