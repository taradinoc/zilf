using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NMock;
using Zilf.Emit.Zap;

namespace Zilf.Emit.Tests
{
    [TestClass]
    public class EmitZapTests
    {
        private MockFactory mockFactory;
        private Mock<IZapStreamFactory> mockStreamFactory;

        [TestInitialize]
        public void Initialize()
        {
            mockFactory = new MockFactory();

            mockStreamFactory = mockFactory.CreateMock<IZapStreamFactory>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            mockFactory.VerifyAllExpectationsHaveBeenMet();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "zversion 0 should be rejected")]
        public void Ctor_Should_Reject_Low_Zversion()
        {
            var gb = new GameBuilder(0, mockStreamFactory.MockObject, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "zversion 9 should be rejected")]
        public void Ctor_Should_Reject_High_Zversion()
        {
            var gb = new GameBuilder(9, mockStreamFactory.MockObject, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "null streamfactory should be rejected")]
        public void Ctor_Should_Reject_Null_StreamFactory()
        {
            var gb = new GameBuilder(5, (IZapStreamFactory)null, false);
        }
    }
}
