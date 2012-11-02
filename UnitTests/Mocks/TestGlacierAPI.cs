using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Glacierizer.UnitTests.Mocks
{
    class TestGlacierAPI : GlacierAPIInterface
    {
        private int partSize = 0;
        private int incrementTransferred = 0;
        private int transferred = 0;

        #region GlacierAPIInterface Members

        public GlacierArchiveInfo ArchiveInfo(string jobId)
        {
            throw new NotImplementedException();
        }

        public ArchivePartInfo DownloadArchivePart(string jobId, long start, long end)
        {
            throw new NotImplementedException();
        }

        public string EndMultiPartUpload(long archiveSize, string checksum, string uploadId)
        {
            // Simulate waiting for initiate response from server, then return fake archive id
            Thread.Sleep(500);
            return "1234567890";
        }

        public byte[] GetVaultInventory(string jobId)
        {
            throw new NotImplementedException();
        }

        public string InitiateDownloadRequest(string archiveId, string snsTopic = "")
        {
            // Simulate waiting for initiate response from server, then return fake archive id
            Thread.Sleep(500);
            return "1234567890";
        }

        public string InitiateMultiPartUpload(int packetSize)
        {
            // Simulate waiting for initiate response from server, then return fake archive id
            partSize = packetSize;
            Thread.Sleep(500);
            return "1234567890";
        }

        public string InitiateVaultInventoryRequest()
        {
            // Simulate waiting for initiate response from server, then return fake archive id
            Thread.Sleep(500);
            return "1234567890";
        }

        public bool JobCompleted(string jobId)
        {
            throw new NotImplementedException();
        }

        public bool UploadPart(GlacierFilePart part, EventHandler<Amazon.Runtime.StreamTransferProgressArgs> progressCallback)
        {
            long total = partSize;

            for (int i = 0; i < 16; ++i)
            {
                incrementTransferred = partSize / 16;
                transferred += incrementTransferred;
                Amazon.Runtime.StreamTransferProgressArgs e = new Amazon.Runtime.StreamTransferProgressArgs(incrementTransferred, transferred, total);
                progressCallback(null, e);
                Thread.Sleep(2);
            }
            transferred = 0;

            return true;
        }

        #endregion
    }
}
