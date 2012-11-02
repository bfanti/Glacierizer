using System;
namespace Glacierizer
{
    interface GlacierAPIInterface
    {
        GlacierArchiveInfo ArchiveInfo(string jobId);
        ArchivePartInfo DownloadArchivePart(string jobId, long start, long end);
        string EndMultiPartUpload(long archiveSize, string checksum, string uploadId);
        byte[] GetVaultInventory(string jobId);
        string InitiateDownloadRequest(string archiveId, string snsTopic = "");
        string InitiateMultiPartUpload(int packetSize);
        string InitiateVaultInventoryRequest();
        bool JobCompleted(string jobId);
        bool UploadPart(GlacierFilePart part, EventHandler<Amazon.Runtime.StreamTransferProgressArgs> progressCallback);
    }
}
