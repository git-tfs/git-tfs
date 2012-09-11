using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sep.Git.Tfs.Core
{
    public class RemoteInfo
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Repository { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string IgnoreRegex { get; set; }
        public bool NoMetaData { get; set; }
        public IEnumerable<string> Aliases { get; set; }
        public bool Autotag { get; set; }
    }
}
