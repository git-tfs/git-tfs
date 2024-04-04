using GitTfs.Util;
using NDesk.Options;

namespace GitTfs.Commands
{
    [StructureMapSingleton]
    public class RemoteOptions
    {
        public OptionSet OptionSet => new OptionSet
                {
                    { "ignore-regex=", "A regex of files to ignore",
                        v => IgnoreRegex = v },
                    { "except-regex=", "A regex of exceptions to '--ignore-regex'",
                        v => ExceptRegex = v},
                    { "gitignore:", "Use .gitignore to ignore files on download from TFS. Alternatively, provide path toward the .gitignore file which will be used to ignore files",
                        v => { GitIgnorePath = v; UseGitIgnore = true; } },
                    { "no-gitignore", "Do not use .gitignore to ignore files on download from TFS",
                        v => NoGitIgnore = v != null },
                    { "u|username=", "TFS username",
                        v => Username = v },
                    { "p|password=", "TFS password",
                        v => Password = v },
                    { "no-parallel", "Do not do parallel requests to TFS",
                        v => NoParallel = (v != null) },
                };

        public string IgnoreRegex { get; set; }
        public string ExceptRegex { get; set; }
        public string GitIgnorePath { get; set; }
        public bool UseGitIgnore { get; set; }
        public bool NoGitIgnore { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool NoParallel { get; set; }
    }
}
