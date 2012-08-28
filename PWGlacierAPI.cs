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

        public GlacierFilePart(byte[] data, string checksum, long start, long end, string uploadId)
        {
            Data = data;
            Checksum = checksum;
            Start = start;
            End = end;
            UploadId = uploadId;
        }
    }

    class PWGlacierAPI
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

        public string InitiateMultiPartUpload(long packetSize)
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

        public bool UploadPart(GlacierFilePart part)
        {
            UploadMultipartPartRequest uploadRequest = new UploadMultipartPartRequest()
            {
                Body = new MemoryStream(part.Data),
                Checksum = part.Checksum,
                Range = part.Range,
                StreamTransferProgress = OnTransferProgress,
                UploadId = part.UploadId,
                VaultName = _vault
            };

            Console.WriteLine("PWGlacierAPI: Uploading bytes " + part.Range);

            UploadMultipartPartResponse response = _amazonGlacierClient.UploadMultipartPart(uploadRequest);

            Console.WriteLine("PWGlacierAPI: Done uploading part.");

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

            try
            {
                CompleteMultipartUploadResponse response = _amazonGlacierClient.CompleteMultipartUpload(request);
                return response.CompleteMultipartUploadResult.ArchiveId;
            }
            catch (InvalidParameterValueException ex)
            {
                return "";
            }
        }

        private void OnTransferProgress(Object sender, EventArgs e)
        {
            string percentageCompleted = e.ToString();
            
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(percentageCompleted, "Percentage completed: ([0-9]+)");
            percentageCompleted = match.Groups[1].Value;
            
            if(int.Parse(percentageCompleted) % 20 == 0)
                Console.WriteLine("GlacierUploader: Part complete percentage " + percentageCompleted + "%");
        }
    }
}