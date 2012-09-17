using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glacierizer
{
    class GlacierArchiveInfo
    {
        public string Id { get; set; }
        public long SizeInBytes { get; set; }
        public DateTime CreationDate { get; set; }
        public string Checksum { get; set; }

        public GlacierArchiveInfo(Amazon.Glacier.Model.DescribeJobResult result)
        {
            Id = result.ArchiveId;
            SizeInBytes = result.ArchiveSizeInBytes;
            CreationDate = result.CreationDate;
            Checksum = result.SHA256TreeHash;
        }
    }
}
