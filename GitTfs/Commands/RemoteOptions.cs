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
                    { "u|username=", "TFS username",
                        v => Username = v },
                    { "p|password=", "TFS password",
                        v => Password = v },
                };
            }
        }

        public string IgnoreRegex { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
