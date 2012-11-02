using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using System.Diagnostics;

namespace Glacierizer
{
    class UploaderWorker
    {
        private static short NUM_RETRIES = 3;

        private GlacierAPIInterface api;
        private TransferMetric metric;

        private byte[] data;

        private string checksum;
        private long start;
        private long end;
        private string uploadId;
        
        private long lastBytesCount;

        public UploaderWorker(ref GlacierAPIInterface api, ref TransferMetric metric, ref byte[] data, string checksum, long start, long end, string uploadId)
        {
            this.api = api;
            this.metric = metric;
            this.start = start;
            this.end = end;
            this.uploadId = uploadId;
            this.lastBytesCount = 0;
            this.data = data;
            this.checksum = checksum;
        }

        private void OnTransferProgress(Object sender, EventArgs e)
        {
            try
            {
                string tmp = e.ToString();
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(tmp, "Bytes transferred: ([0-9]+)");
                int numBytes = int.Parse(match.Groups[1].Value);

                // Add the delta to the total then save the new byte count
                metric.incrementTransferredBytes(numBytes - lastBytesCount);
                lastBytesCount = numBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        public void Run()
        {
            short retries = 0;

            while (retries++ < NUM_RETRIES)
            {
                try
                {
                    GlacierFilePart part = new GlacierFilePart(ref data, checksum, start, end, uploadId);
                    api.UploadPart(part, OnTransferProgress);
                    metric.incrementTransferredParts();
                    return;
                }
                catch (Exception ex)
                {
                    // Remove what we've uploaded so far from the metrics just to be precise.
                    metric.removeTransferredBytes(lastBytesCount);

                    // Reset the last byte count to zero or we're going to mess things up later.
                    lastBytesCount = 0;

                    Console.WriteLine("Upload exception: " + ex.Message);
                }
            }
        }
    }
}