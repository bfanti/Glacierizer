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

namespace Glacierizer
{
    class GlacierizerUploader
    {
        private PWGlacierAPI _glacierAPI = null;
        private Stream _inputStream = null;
        private long _partSize;
        private short _threads;

        private HashAlgorithm _hasher;

        private long _totalBytesUploaded;
        public long TotalBytesUploaded
        {
            get { return _totalBytesUploaded; }
        }

        private string _archiveId;
        public string ArchiveId
        {
            get { return _archiveId; }
        }

        public GlacierizerUploader(Properties props)
        {
            _glacierAPI = new PWGlacierAPI(props.vault, props.name);
            _hasher = SHA256.Create();

            if (props.filename.Length > 0)
            {
                _inputStream = File.Open(props.filename, FileMode.Open);
            }
            else
                _inputStream = Console.OpenStandardInput();

            _partSize = props.size;
            _threads = props.threads;
        }
        
        public bool Upload()
        {
            string uploadId = _glacierAPI.InitiateMultiPartUpload(_partSize);

            Queue<byte[]> hashQueue = new Queue<byte[]>();
            _totalBytesUploaded = 0;

            Queue<byte> currentQueue = new Queue<byte>();
            byte[] shortBuffer = new byte[1024];

            int bytes;
            long currentStart = 0;
            long currentEnd = 0;

            while (true)
            {
                bytes = _inputStream.Read(shortBuffer, 0, shortBuffer.Length);

                if (bytes == 0)
                {
                    if (currentQueue.Count != 0)
                    {
                        long size = currentQueue.Count;
                        currentEnd = currentStart + currentQueue.Count - 1;

                        UploadPart(ref currentQueue, ref hashQueue, size, currentStart, currentEnd, uploadId);

                        _totalBytesUploaded += size;
                    }

                    Console.WriteLine("StdInReader: Data stream finished...");
                    break;
                }

                for (int i = 0; i < bytes; ++i)
                    currentQueue.Enqueue(shortBuffer[i]);

                if (currentQueue.Count >= _partSize)
                {
                    currentEnd = currentStart + _partSize - 1;

                    UploadPart(ref currentQueue, ref hashQueue, _partSize, currentStart, currentEnd, uploadId);

                    _totalBytesUploaded += _partSize;
                    currentStart = currentEnd + 1;
                }
            }

            // Compute the full tree SHA256 checksum
            string fullChecksum = "";

            Queue<byte[]> treeHash = processLevel(hashQueue);

            // If the hash queue count is not 1, something went wrong. Throw?
            if (treeHash.Count != 1)
                throw new Exception();

            byte[] hash = treeHash.Dequeue();
            foreach (char c in hash)
            {
                int tmp = c;
                fullChecksum += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
            }

            _archiveId = _glacierAPI.EndMultiPartUpload(_totalBytesUploaded, fullChecksum, uploadId);
            if (_archiveId.Length == 0)
                return false;
            else
                return true;
        }

        private Queue<byte[]> processLevel(Queue<byte[]> hashQueue)
        {
            if (hashQueue.Count == 1)
                return hashQueue;

            Queue<byte[]> newLevelQueue = new Queue<byte[]>();

            while (hashQueue.Count >= 2)
            {
                byte[] first = hashQueue.Dequeue();
                byte[] second = hashQueue.Dequeue();

                byte[] hashBytes = _hasher.ComputeHash(first.Concat(second).ToArray());

                newLevelQueue.Enqueue(hashBytes);

                if (hashQueue.Count == 1)
                    newLevelQueue.Enqueue(hashQueue.Dequeue());
            }

            return processLevel(newLevelQueue);
        }

        private void UploadPart(ref Queue<byte> currentQueue, ref Queue<byte[]> hashQueue, long size, long start, long end, string uploadId)
        {
            byte[] uploadablePart = new byte[size];

            for (int i = 0; i < size; ++i)
                uploadablePart[i] = currentQueue.Dequeue();

            HashAlgorithm hasher = SHA256.Create();
            byte[] hashBytes = hasher.ComputeHash(uploadablePart);
            hashQueue.Enqueue(hashBytes);
            string checksum = "";
            foreach (char c in hashBytes)
            {
                int tmp = c;
                checksum += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
            }

            GlacierFilePart part = new GlacierFilePart(uploadablePart, checksum, start, end, uploadId);
            _glacierAPI.UploadPart(part);
        }
    }
}