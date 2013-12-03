using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class TfsDownloadVerifier
    {
        public bool IsEnabled { get; private set; }

        public int MaxRetries { get; private set; }

        public TfsDownloadVerifier()
        {
            MaxRetries = 3;
        }


        public void Enable()
        {
            IsEnabled = true;
        }

        public void SetMaxRetries(int count)
        {
            MaxRetries = count;
        }

        public bool IsValid(IWorkspace workspace, IItem item)
        {
            bool result;

            if (item.ItemType == TfsItemType.Folder)
            {
                result = true;
            }
            else
            {
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    result = EntryIsValid(workspace.GetLocalItemForServerItem(item.ServerItem), md5, item.ContentLength, item.HashValue);
                }
            }

            return result;
        }

        public bool EnsureValidChanges(IWorkspace workspace, IEnumerable<IChange> changes, Action<IItem> retry)
        {
            return EnsureValidItems(workspace, changes.Where(c => !IsDelete(c)).Select(c => c.Item), retry);
        }

        public bool EnsureValidItems(IWorkspace workspace, IEnumerable<IItem> changes, Action<IItem> retry)
        {
            var maxRetries = MaxRetries;
            var failures = changes.Where(item => !IsValid(workspace, item)).ToList();
            
            while (failures.Count > 0 && maxRetries-- > 0)
            {
                foreach (var item in failures)
                {
                    retry(item);
                }

                failures = failures.Where(item => !IsValid(workspace, item)).ToList();
            }

            return failures.Count == 0;
        }

        private static bool IsDelete(IChange change)
        {
            const TfsChangeType deleteBits = TfsChangeType.Delete | TfsChangeType.SourceRename;

            return (change.ChangeType & deleteBits) != 0;
        }

        private static bool EntryIsValid(string file, HashAlgorithm algorithm, long targetContentLength, IEnumerable<byte> targetHashValue)
        {
            bool result;
            var fileInfo = new FileInfo(file);

            if (!fileInfo.Exists)
            {
                Trace.WriteLine(new LazyString(() => string.Concat("verify: file not found (", file, ")")));
                result = false;
            }
            else if (fileInfo.Length != targetContentLength)
            {
                Trace.WriteLine(new LazyString(() => string.Format("verify: size mismatch (theirs: {0}, ours: {1}, file: {2})", targetContentLength, fileInfo.Length, file)));
                result = false;
            }
            else
            {
                using (var content = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read))
                {
                    result = algorithm.ComputeHash(content).SequenceEqual(targetHashValue);
                    if (result)
                    {
                        Trace.WriteLine(new LazyString(() => string.Concat("verify: ok (", file, ")")));
                    }
                    else
                    {
                        Trace.WriteLine(new LazyString(() => string.Concat("verify: hash value mismatch (", file, ")")));
                    }
                }
            }

            return result;
        }

        private class LazyString
        {
            private Lazy<string> _value;
            
            public LazyString(Func<string> value)
            {
                _value = new Lazy<string>(value);
            }

            public override string ToString()
            {
                return _value.Value.ToString();
            }
        }
    }
}
