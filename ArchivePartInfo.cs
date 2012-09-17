using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Glacierizer
{
    class ArchivePartInfo
    {
        private Stream stream;
        private string checksum;

        public ArchivePartInfo(System.IO.Stream stream, string p)
        {
            this.stream = stream;
            checksum = p;
        }

        public Stream Stream()
        {
            return stream;
        }

        public string Checksum()
        {
            return checksum;
        }
    }
}
