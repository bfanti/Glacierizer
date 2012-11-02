using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Glacierizer
{
    class TransferMetric
    {
        private Stopwatch stopWatch;
        private long totalBytesTransferred;
        private int totalPartsTransferred;

        private object bytesMutex;
        private object partsMutex;

        public TransferMetric()
        {
            totalPartsTransferred = 0;
            stopWatch = new Stopwatch();
            stopWatch.Start();
            bytesMutex = new object();
            partsMutex = new object();
        }

        public void incrementTransferredBytes()
        {
            lock (bytesMutex)
            {
                ++totalBytesTransferred;
            }
        }

        public long incrementTransferredBytes(long more)
        {
            lock (bytesMutex)
            {
                totalBytesTransferred += more;
            }
            return more;
        }

        public void removeTransferredBytes(long count)
        {
            lock (bytesMutex)
            {
                totalBytesTransferred -= count;
            }
        }

        public void incrementTransferredParts()
        {
            lock (partsMutex)
            {
                ++totalPartsTransferred;
            }
        }

        public long bytesTransferred()
        {
            return totalBytesTransferred;
        }

        public int partsTransferred()
        {
            return totalPartsTransferred;
        }

        public long speed()
        {
            lock (bytesMutex)
            {
                long elapsedInSeconds = stopWatch.ElapsedMilliseconds / 1000;
                if (elapsedInSeconds != 0)
                    return totalBytesTransferred / elapsedInSeconds;
                else
                    return 0;
            }
        }
    }
}
