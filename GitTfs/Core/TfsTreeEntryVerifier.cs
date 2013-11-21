using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Sep.Git.Tfs.Core
{
    public class TfsTreeEntryVerifier
    {
        public bool IsEnabled { get; private set; }

        
        public TfsTreeEntryVerifier()
        { }


        public void Enable()
        {
            IsEnabled = true;
        }

        public bool IsValid(TfsTreeEntry entry)
        {
            bool result;

            using (var md5 = new MD5CryptoServiceProvider())
            {
                result = FileIsValid(entry.FullName, md5, entry.Item.ContentLength, entry.Item.HashValue);
            }

            return result;
        }

        public IEnumerable<TfsTreeEntry> GetInvalidEntries(IEnumerable<TfsTreeEntry> entries)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return entries.Where(e => !FileIsValid(e.FullName, md5, e.Item.ContentLength, e.Item.HashValue));
            }
        }

        private static bool FileIsValid(string file, HashAlgorithm algorithm, long targetContentLength, IEnumerable<byte> targetHashValue)
        {
            bool result;
            var fileInfo = new FileInfo(file);

            if (!fileInfo.Exists || fileInfo.Length != targetContentLength)
            {
                result = false;
            }
            else
            {
                using (var content = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    result = algorithm.ComputeHash(content).SequenceEqual(targetHashValue);
                }
            }

            return result;
        }
    }
}
