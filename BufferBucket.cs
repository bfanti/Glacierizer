using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Glacierizer
{
    class BufferBucket
    {
        private byte[] buffer;
        private string checksum;
        private bool ready;
        private int offset;

        public BufferBucket()
        {
            ready = false;
            offset = 0;
        }

        public void Initialize(int size)
        {
            buffer = new byte[size];
        }

        public bool IsReady()
        {
            return ready;
        }

        public void Checksum(string checksum)
        {
            this.checksum = checksum;
        }

        public string Checksum()
        {
            return checksum;
        }

        public byte[] Data()
        {
            return buffer;
        }

        public void Append(byte data)
        {
            buffer[offset++] = data;
        }

        public void IsReady(bool p)
        {
            ready = p;
        }
    }
}