using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;

namespace Glacierizer
{
    class GlacierizerUploader
    {
        private GlacierAPIInterface glacierAPI = null;
        private Stream inputStream = null;
        private List<ThreadInfo> threads = null;
        private List<string> hashList = null;
        public string GetHashList()
        {
            string output = "";
            foreach (string item in hashList)
            {
                output += item + "\n";
            }
            return output;
        }

        private int partSize;
        private short numRequestedThreads;
        private int bufferFill;
        private long totalBytesProcessed;

        public long TotalBytesProcessed
        {
            get { return totalBytesProcessed; }
        }

        private TransferMetric transferMetric;
        public long TotalBytesUploaded
        {
            get { return transferMetric.bytesTransferred(); }
        }

        private string _archiveId;
        public string ArchiveId
        {
            get { return _archiveId; }
        }

        public GlacierizerUploader(GlacierAPIInterface api, Stream input, int partSize, short numThreads)
        {
            glacierAPI = api;
            inputStream = input;
            this.partSize = partSize;
            numRequestedThreads = numThreads;

            threads = new List<ThreadInfo>();
            for (int i = 0; i < numRequestedThreads; ++i)
            {
                ThreadInfo info = new ThreadInfo(partSize);
                threads.Add(info);
            }

            hashList = new List<string>();

            transferMetric = new TransferMetric();
        }

        public bool Upload()
        {
            string uploadId = glacierAPI.InitiateMultiPartUpload(partSize);
            Console.WriteLine("Upload started.");
            Console.WriteLine("Upload ID: " + uploadId);

            totalBytesProcessed = 0;

            bufferFill = 0;
            int pipeReadableLength = (int)Math.Pow(2, 10);

            byte[] buffer = new byte[partSize];

            int bytesRead = 1;
            long currentStart = 0;
            long currentEnd = 0;

            Timer timer = new Timer(DisplayMetrics, null, 5000, 5000);
            
            while (bytesRead > 0)
            {
                bytesRead = inputStream.Read(buffer, bufferFill, pipeReadableLength);
                bufferFill += bytesRead;

                // Decide whether we are ready to upload this part:
                // 1. If we read ZERO bytes, but the bufferFill isn't zero, it means the stream ended and we have to upload the last few bytes.
                // 2. If our total current bufferFill is equal to partSize it means we have a full part ready to upload.
                if (bytesRead == 0 && bufferFill != 0
                 || bufferFill == partSize)
                {
                    currentEnd = currentStart + bufferFill - 1;

                    PrepareAndStartThreadForUpload(uploadId, ref buffer, currentStart, currentEnd);

                    totalBytesProcessed += bufferFill;
                    bufferFill = 0;
                 
                    currentStart = currentEnd + 1;
                }
            }

            while (threads.Count != 0)
            {
                for (int i = 0; i < threads.Count; ++i)
                {
                    if (!threads[i].IsAlive())
                        threads.RemoveAt(i);
                }
                Thread.Sleep(1000);
            }

            return FinalizeUpload(uploadId);
        }

        private bool FinalizeUpload(string uploadId)
        {
            string fullChecksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(hashList);

            int numRetries = 0;
            while (numRetries++ < 3)
            {
                try
                {
                    _archiveId = glacierAPI.EndMultiPartUpload(totalBytesProcessed, fullChecksum, uploadId);

                    if (_archiveId.Length == 0)
                        return false;
                    else
                        return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine("Upload failed in final stage.");
            Console.WriteLine("Upload Id: " + uploadId);
            return false;
        }

        private void PrepareAndStartThreadForUpload(string uploadId, ref byte[] buffer, long currentStart, long currentEnd)
        {
            int availableThreadIndex = WaitForThreadsInPool(ref threads);

            threads[availableThreadIndex].CopyData(ref buffer, bufferFill);

            string checksum = "";
            using (MemoryStream ms = new MemoryStream(threads[availableThreadIndex].data))
            {
                checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(ms);
                hashList.Add(checksum);
            }

            threads[availableThreadIndex].SetWorker(new UploaderWorker(ref glacierAPI, ref transferMetric, ref threads[availableThreadIndex].data, checksum, currentStart, currentEnd, uploadId));
            threads[availableThreadIndex].Start();
        }

        private int WaitForThreadsInPool(ref List<ThreadInfo> threads)
        {
            while (true)
            {
                for (int i = 0; i < threads.Count; ++i)
                {
                    if (!threads[i].IsAlive())
                        return i;
                }
                long speed = transferMetric.speed();
                long partDuration = speed != 0 ? (1000 * (partSize / speed)) : 0;
                long sleep = Math.Max(Math.Min(2 * partDuration / threads.Count, 60000), 500);
                Console.WriteLine("Main Thread Waiting for Uploader Threads to finish. Sleeping for " + sleep + "ms");
                Thread.Sleep((int)sleep);
            }
        }

        private void DisplayMetrics(object state)
        {
            Process proc = Process.GetCurrentProcess();
            Console.WriteLine("Read: " + Utilities.BytesToHuman(bufferFill) +
                              ", Processed: " + Utilities.BytesToHuman(totalBytesProcessed) +
                              ", Uploaded: " + Utilities.BytesToHuman(transferMetric.bytesTransferred()) +
                              ", Parts: " + transferMetric.partsTransferred() +
                              ", Speed: " + Utilities.BytesToHuman(transferMetric.speed()) + "/s" +
                              ", Threads: " + threads.Count +
                              ", RAM: " + Utilities.BytesToHuman(proc.PrivateMemorySize64));
        }
    }
}