using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glacierizer.UnitTests
{
    using NUnit.Framework;

    [TestFixture]
    public class TransferMetricTest
    {
        private TransferMetric obj;

        [TestFixtureSetUp]
        public void SetUp()
        {
            obj = new TransferMetric();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {            
        }

        [Test]
        public void CreateTransferMetricObject()
        {
            Assert.AreNotEqual(obj, null);
            Assert.AreEqual(obj.partsTransferred(), 0);
            Assert.AreEqual(obj.bytesTransferred(), 0);
        }

        [Test]
        public void ZeroSpeed()
        {
            Assert.AreEqual(obj.speed(), 0);
        }
    }
}
