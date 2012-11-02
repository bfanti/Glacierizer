using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glacierizer.UnitTests
{
    using NUnit.Framework;
    using Glacierizer.UnitTests.Mocks;

    [TestFixture]
    class UploaderWorkerTest
    {
        [Test]
        public void TestUploadWorker()
        {
            GlacierAPIInterface api = new TestGlacierAPI();
            TransferMetric metric = new TransferMetric();
            byte[] data = new byte[3 * 1024];
            string checksum = "";
            long start = 0;
            long end = 3 * 1024;
            string uploadId = "";

            UploaderWorker worker = new UploaderWorker(ref api, ref metric, ref data, checksum, start, end, uploadId);
            Assert.NotNull(worker);

            worker.Run();

            Assert.AreEqual(metric.bytesTransferred(), 3 * 1024);
            Assert.AreEqual(metric.partsTransferred(), 1);
        }
    }
}
