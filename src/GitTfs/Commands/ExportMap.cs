using System.ComponentModel;

using GitTfs.Core;

using NDesk.Options;

using StructureMap;

namespace GitTfs.Commands
{
    [Pluggable("exportmap")]
    [Description("exportmap -f <file>")]
    [RequiresValidGitRepository]
    public class ExportMap : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly Help _helper;

        public ExportMap(Globals globals, Help helper)
        {
            _globals = globals;
            _helper = helper;
        }

        public string FilePath { get; set; }

        public OptionSet OptionSet => new OptionSet
                {
                     { "f|file=", "The output file path",
                        f => FilePath = f }
                };

        public int Run()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                return _helper.Run(this);
            }

            var commits = _globals.Repository.GetCommitChangeSetPairs();
            File.WriteAllLines(FilePath, commits.Select(map => $"{map.Key}-{map.Value}"));

            return 0;
        }
    }
}
