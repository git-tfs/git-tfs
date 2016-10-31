using NDesk.Options;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class RemoteOptions
    {
        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "ignore-regex=", "A regex of files to ignore",
                        v => IgnoreRegex = v },
                    { "except-regex=", "A regex of exceptions to '--ignore-regex'",
                        v => ExceptRegex = v},
                    { "u|username=", "TFS username",
                        v => Username = v },
                    { "p|password=", "TFS password",
                        v => Password = v },
                };
            }
        }

        public string IgnoreRegex { get; set; }
        public string ExceptRegex { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
