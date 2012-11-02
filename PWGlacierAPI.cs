using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;

namespace Glacierizer
{
    class GlacierFilePart
    {
        public byte[] Data { get; set; }
        public string Checksum { get; set; }
        public long Start {get;set;}
        public long End { get; set; }
        public string UploadId { get; set; }

        public string Range
        {
            get
            {
                return "bytes " + Start + "-" + End + "/*";
            }
        }

        public GlacierFilePart(ref byte[] data, string checksum, long start, long end, string uploadId)
        {
            Data = data;
            Checksum = checksum;
            Start = start;
            End = end;
            UploadId = uploadId;
        }
    }

    class PWGlacierAPI : GlacierAPIInterface
    {
        private AmazonGlacierClient _amazonGlacierClient;
        private string _vault;
        private string _archive;
        
        public PWGlacierAPI(string vaultName, string archiveName)
        {
            _amazonGlacierClient = new AmazonGlacierClient(Amazon.RegionEndpoint.USEast1);
            _vault = vaultName;
            _archive = archiveName;
        }

        public string InitiateMultiPartUpload(int packetSize)
        {
            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest()
            {
                ArchiveDescription = _archive,
                PartSize = packetSize,
                VaultName = _vault
            };

            InitiateMultipartUploadResponse response = _amazonGlacierClient.InitiateMultipartUpload(initiateRequest);

            return response.InitiateMultipartUploadResult.UploadId;
        }

        public bool UploadPart(GlacierFilePart part, System.EventHandler<Amazon.Runtime.StreamTransferProgressArgs> progressCallback)
        {
            UploadMultipartPartRequest uploadRequest = new UploadMultipartPartRequest()
            {
                Body = new MemoryStream(part.Data),
                Checksum = part.Checksum,
                Range = part.Range,
                StreamTransferProgress = progressCallback,
                UploadId = part.UploadId,
                VaultName = _vault
            };
            
            UploadMultipartPartResponse response = _amazonGlacierClient.UploadMultipartPart(uploadRequest);
            
            if (part.Checksum == response.UploadMultipartPartResult.Checksum)
                return true;
            else
                return false;
        }

        public string EndMultiPartUpload(long archiveSize, string checksum, string uploadId)
        {
            CompleteMultipartUploadRequest request = new CompleteMultipartUploadRequest()   
            {
                ArchiveSize = archiveSize.ToString(),
                Checksum = checksum,
                UploadId = uploadId,
                VaultName = _vault
            };

            CompleteMultipartUploadResponse response = _amazonGlacierClient.CompleteMultipartUpload(request);
            return response.CompleteMultipartUploadResult.ArchiveId;
        }

        public string InitiateDownloadRequest(string archiveId, string snsTopic = "")
        {
            InitiateJobRequest initDownloadRequest = new InitiateJobRequest()
            {
                VaultName = _vault,
                JobParameters = new JobParameters()
                {
                    Type = "archive-retrieval",
                    ArchiveId = archiveId
                }
            };

            InitiateJobResponse response = _amazonGlacierClient.InitiateJob(initDownloadRequest);
            return response.InitiateJobResult.JobId;
        }

        public bool JobCompleted(string jobId)
        {
            DescribeJobRequest describeJobRequest = new DescribeJobRequest()
            {
                AccountId = "-",
                JobId = jobId,
                VaultName = _vault
            };

            DescribeJobResponse response = _amazonGlacierClient.DescribeJob(describeJobRequest);
            return response.DescribeJobResult.Completed;
        }

        public GlacierArchiveInfo ArchiveInfo(string jobId)
        {
            DescribeJobRequest describeJobRequest = new DescribeJobRequest()
            {
                AccountId = "-",
                JobId = jobId,
                VaultName = _vault
            };

            DescribeJobResponse response = _amazonGlacierClient.DescribeJob(describeJobRequest);
            return new GlacierArchiveInfo(response.DescribeJobResult);
        }

        public ArchivePartInfo DownloadArchivePart(string jobId, long start, long end)
        {
            GetJobOutputRequest downloadRequest = new GetJobOutputRequest()
            {
                JobId = jobId,
                VaultName = _vault
            };

            downloadRequest.SetRange(start, end);

            GetJobOutputResponse response = _amazonGlacierClient.GetJobOutput(downloadRequest);
            GetJobOutputResult result = response.GetJobOutputResult;

            ArchivePartInfo info = new ArchivePartInfo(result.Body, result.Checksum);

            return info;
        }

        public string InitiateVaultInventoryRequest()
        {
            InitiateJobRequest inventoryRequest = new InitiateJobRequest()
            {
                VaultName = _vault,
                JobParameters = new JobParameters()
                {
                    Type = "inventory-retrieval"
                }
            };

            InitiateJobResponse response = _amazonGlacierClient.InitiateJob(inventoryRequest);
            return response.InitiateJobResult.JobId;
        }

        public byte[] GetVaultInventory(string jobId)
        {
            GetJobOutputRequest inventoryRequest = new GetJobOutputRequest()
            {
                JobId = jobId,
                VaultName = _vault
            };

            GetJobOutputResponse response = _amazonGlacierClient.GetJobOutput(inventoryRequest);
            using (Stream webStream = response.GetJobOutputResult.Body)
            {
                List<byte> data = new List<byte>();
                int newByte;

                while ((newByte = webStream.ReadByte()) != -1)
                    data.Add((byte)newByte);

                return data.ToArray();
            }
        }
    }
}