using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glacierizer.UnitTests
{
    using NUnit.Framework;
    using Glacierizer.UnitTests.Mocks;
    using System.IO;

    [TestFixture]
    class GlacierizerUploaderTest
    {
        GlacierAPIInterface api;

        [SetUp]
        public void SetUp()
        {
            api = new TestGlacierAPI();
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void TestUploadFromFileWithSingleThread()
        {
            Stream input = File.Open("../..//UnitTests/data/test.bin", FileMode.Open);
            int partSize = 1024 * 1024;
            short numThreads = 1;
            
            GlacierizerUploader uploader = new GlacierizerUploader(api, input, partSize, numThreads);

            Assert.AreNotEqual(uploader, null);

            bool uploadSucceeded = uploader.Upload();

            Assert.IsTrue(uploadSucceeded);
            Assert.AreEqual(uploader.TotalBytesUploaded, 20971520);
        }

        [Test]
        public void TestUploadFromFileWithMultipleThreads()
        {
            Stream input = File.Open("../..//UnitTests/data/test.bin", FileMode.Open);
            int partSize = 1024 * 1024;
            short numThreads = 4;

            GlacierizerUploader uploader = new GlacierizerUploader(api, input, partSize, numThreads);

            Assert.AreNotEqual(uploader, null);

            bool uploadSucceeded = uploader.Upload();

            Assert.IsTrue(uploadSucceeded);
            Assert.AreEqual(uploader.TotalBytesUploaded, 20971520);
        }

        [Test]
        public void TestUploadFromVeryLargeFileWithMassiveThreads()
        {
            Stream input = File.Open("../..//UnitTests/data/large.bin", FileMode.Open);
            int partSize = 8 * 1024 * 1024;
            short numThreads = 50;

            GlacierizerUploader uploader = new GlacierizerUploader(api, input, partSize, numThreads);

            Assert.AreNotEqual(uploader, null);

            bool uploadSucceeded = uploader.Upload();

            Assert.IsTrue(uploadSucceeded);
            Assert.AreEqual(uploader.TotalBytesUploaded, 0);
        }
    }
}
