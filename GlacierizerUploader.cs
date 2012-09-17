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
        private PWGlacierAPI _glacierAPI = null;
        private Stream inputStream = null;
        private List<ThreadInfo> threads = null;

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

        public GlacierizerUploader(Properties props)
        {
            _glacierAPI = new PWGlacierAPI(props.vault, props.archive);

            if (props.filename.Length > 0)
            {
                inputStream = File.Open(props.filename, FileMode.Open);
            }
            else
                inputStream = Console.OpenStandardInput();

            partSize = props.size;
            numRequestedThreads = props.threads;

            threads = new List<ThreadInfo>();
            for (int i = 0; i < numRequestedThreads; ++i)
            {
                ThreadInfo info = new ThreadInfo(partSize);
                threads.Add(info);
            }

            transferMetric = new TransferMetric();
        }

        public bool Upload()
        {
            string uploadId = _glacierAPI.InitiateMultiPartUpload(partSize);
            Console.WriteLine("Upload started.");
            Console.WriteLine("Upload ID: " + uploadId);

            List<string> hashList = new List<string>();
            totalBytesProcessed = 0;

            bufferFill = 0;
            int pipeReadableLength = 64 * (int)Math.Pow(2, 10);

            byte[] buffer = new byte[partSize];

            int bytesRead = 1;
            long currentStart = 0;
            long currentEnd = 0;

            Timer timer = new Timer(DisplayMetrics, null, 1000, 5000);
            
            while (bytesRead > 0)
            {
                bytesRead = inputStream.Read(buffer, bufferFill, pipeReadableLength);
                bufferFill += bytesRead;

                // Is the buffer full? If so, we're ready to ship this package, OR
                // did we read 0 bytes and still have something left in the buffer?
                // Ship it.
                if (bytesRead == 0 && bufferFill != 0
                 || bufferFill == partSize)
                {
                    currentEnd = currentStart + bufferFill - 1;

                    int availableThreadIndex = WaitForThreadsInPool(ref threads);

                    threads[availableThreadIndex].CopyData(ref buffer, bufferFill);
                    threads[availableThreadIndex].SetWorker(new UploaderWorker(ref _glacierAPI, ref transferMetric, ref threads[availableThreadIndex].data, ref hashList, currentStart, currentEnd, uploadId));
                    threads[availableThreadIndex].Start();

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

            string fullChecksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(hashList);

            _archiveId = _glacierAPI.EndMultiPartUpload(totalBytesProcessed, fullChecksum, uploadId);
            if (_archiveId.Length == 0)
                return false;
            else
                return true;
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
                long partDuration = 1000 * (partSize / speed);
                long sleep = Math.Max(Math.Min(partDuration / threads.Count, 60000), 500);
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