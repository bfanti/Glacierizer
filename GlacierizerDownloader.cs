using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Glacierizer
{
    class GlacierizerDownloader
    {
        private PWGlacierAPI glacierAPI = null;

        Queue<BufferBucket> outputQueue = new Queue<BufferBucket>();
        List<string> treeHashes = new List<string>();

        private Stream outputStream = null;
        private short maxNumThreads;
        private List<Thread> threads = new List<Thread>();

        private TransferMetric transferMetrics = new TransferMetric();

        private string jobId;
        private string archiveId;
        private int partSize;

        public long TotalBytesDownloaded { get; set; }

        public GlacierizerDownloader(Properties props)
        {
            glacierAPI = new PWGlacierAPI(props.vault, props.archive);

            if (props.jobId.Length != 0)
                jobId = props.jobId;

            archiveId = props.archive;
            partSize = props.size;
            maxNumThreads = props.threads;

            if (props.filename == null)
                outputStream = Console.OpenStandardOutput();
            else
                outputStream = File.Open(props.filename, FileMode.OpenOrCreate);
        }

        private void WriteToStream()
        {
            while (true)
            {
                try
                {
                    BufferBucket bucket;
                    byte[] data;
                    string checksum;

                    if (outputQueue.Count > 0)
                    {
                        while (true)
                        {
                            lock (outputQueue)
                            {
                                if (outputQueue.Peek().IsReady())
                                {
                                    bucket = outputQueue.Dequeue();
                                    data = new byte[bucket.Data().Length];
                                    checksum = bucket.Checksum();
                                    bucket.Data().CopyTo(data, 0);
                                    break;
                                }
                            }
                            Thread.Sleep(1000);
                        }

                        treeHashes.Add(checksum);
                        outputStream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public bool Download()
        {
            string jobId = this.jobId != null ? this.jobId : glacierAPI.InitiateDownloadRequest(archiveId);

            while (!glacierAPI.JobCompleted(jobId))
            {
                Console.WriteLine("Waiting for AWS Glacier Archive retrieval...");
                if (this.jobId != null)
                {
                    Console.WriteLine("Write JobId to file & quit?");
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.KeyChar == 'y' || key.KeyChar== 'Y')
                    {
                        System.IO.File.WriteAllText("jobId.out", jobId);
                        return false;
                    }
                }
                Thread.Sleep(60 * 1000);
            }

            GlacierArchiveInfo info = glacierAPI.ArchiveInfo(jobId);

            long currentStart = 0 ;
            long currentEnd = 0;

            // Start the writer thread
            Thread writer = new Thread(new ThreadStart(WriteToStream));
            writer.Start();

            // Start the metrics reporting thread
            Timer timer = new Timer(DisplayMetrics, null, 1000, 1000);

            while (currentEnd < info.SizeInBytes)
            {
                currentEnd = currentStart + partSize - 1;

                WaitForNextAvailableThread();

                BufferBucket bucket = new BufferBucket();
                lock (outputQueue)
                {
                    outputQueue.Enqueue(bucket);
                }
                DownloadWorker worker = new DownloadWorker(ref glacierAPI, jobId, currentStart, currentEnd, ref bucket, ref transferMetrics);
                Thread t = new Thread(new ThreadStart(worker.Download));
                t.Start();
                threads.Add(t);

                currentStart = currentEnd + 1;
            }

            // Wait for all download thread workers to finish
            while (threads.Count > 0)
            {
                for (int i = 0; i < threads.Count; ++i)
                {
                    if (!threads[i].IsAlive)
                        threads.RemoveAt(i);
                }
                Thread.Sleep(1000);
            }

            // Wait for the output writer thread worker to finish, then kill it.
            while (outputQueue.Count > 0)
                Thread.Sleep(500);

            writer.Abort();

            outputStream.Close();

            string checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(treeHashes);

            if (checksum == info.Checksum)
                return true;
            else
                return false;
        }

        public void WaitForNextAvailableThread()
        {
            if (threads.Count < maxNumThreads)
                return;

            while (true)
            {
                for (int i = 0; i < threads.Count; ++i)
                {
                    if (!threads[i].IsAlive)
                    {
                        threads.RemoveAt(i);
                        return;
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void DisplayMetrics(object state)
        {
            Process proc = Process.GetCurrentProcess();
            Console.WriteLine("Downloaded: " + Utilities.BytesToHuman(transferMetrics.bytesTransferred()) +
                              ", Parts: " + transferMetrics.partsTransferred() +
                              ", Speed: " + Utilities.BytesToHuman(transferMetrics.speed()) + "/s" +
                              ", Threads: " + threads.Count +
                              ", RAM: " + Utilities.BytesToHuman(proc.PrivateMemorySize64));
        }
    }
}
