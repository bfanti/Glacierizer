using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Glacierizer
{
    class ThreadInfo
    {
        private Thread thread = null;
        private UploaderWorker worker = null;
        public byte[] data = null;

        public ThreadInfo(int partSize)
        {
            data = new byte[partSize];
        }

        public void SetWorker(UploaderWorker w)
        {
            worker = w;
            thread = null;
            thread = new Thread(new ThreadStart(w.Run));
        }

        public void CopyData(ref byte[] newData, int size)
        {
            if (size > data.Length)
                throw new Exception("Seriously?");

            if (size < data.Length)
                data = new byte[size];

            for (int i = 0; i < size; ++i)
                data[i] = newData[i];
        }

        public bool IsAlive()
        {
            if (thread != null)
                return thread.IsAlive;
            else
                return false;
        }

        public void Start()
        {
            if (thread != null)
                thread.Start();
        }
    }
}
