using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Glacierizer
{
    class DownloadWorker
    {
        private PWGlacierAPI api;
        private string jobId;
        private long start;
        private long end;
        private BufferBucket bucket;
        private TransferMetric metric;

        public DownloadWorker(ref PWGlacierAPI api, string jobId, long start, long end, ref BufferBucket bucket, ref TransferMetric metric)
        {
            this.api = api;
            this.jobId = jobId;
            this.start = start;
            this.end = end;
            this.bucket = bucket;
            this.metric = metric;
        }

        public void Download()
        {
            int retries = 0;
            while (retries++ < 3)
            {
                try
                {
                    ArchivePartInfo info = api.DownloadArchivePart(jobId, start, end);

                    Stream stream = info.Stream();
                    List<byte> dynamicData = new List<byte>();
                    int data;
                    long bytesRead = 0;
                    while ((data = stream.ReadByte()) != -1)
                    {
                        bytesRead = metric.incrementTransferredBytes(1);
                        dynamicData.Add((byte)data);
                    }

                    bucket.Initialize(dynamicData.Count);
                    dynamicData.CopyTo(bucket.Data());

                    using (Stream hashStream = new MemoryStream(bucket.Data(), 0, dynamicData.Count))
                    {
                        string checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(hashStream);

                        if (checksum != info.Checksum())
                        {
                            metric.removeTransferredBytes(bytesRead);
                            throw new Exception("Checksum mismatch");
                        }

                        bucket.Checksum(checksum);
                        bucket.IsReady(true);

                        metric.incrementTransferredParts();
                    }

                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Downloader Exception: " + e.Message);
                }
            }

            Console.WriteLine("Download failed");
        }
    }
}
