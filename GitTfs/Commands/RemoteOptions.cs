using System.ComponentModel;
using NDesk.Options;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    using System;

    [StructureMapSingleton]
    public class RemoteOptions
    {
        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "ignore-regex=", "a regex of files to ignore",
                        v => IgnoreRegex = v },
                    { "no-metadata", "leave out the 'git-tfs-id:' tag in commit messages\nUse this when you're exporting from TFS and don't need to put data back into TFS.",
                        v => NoMetaData = v != null },
                    { "u|username=", "TFS username",
                        v => Username = v },
                    { "p|password=", "TFS password",
                        v => Password = v },
                };
            }
        }

        public string IgnoreRegex { get; set; }

        public bool NoMetaData { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
