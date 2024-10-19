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
        public bool WithChangesetsOnMultipleBranches { get; set; } = false;

        public OptionSet OptionSet => new OptionSet
                {
                     { "f|file=", "The output file path",
                        f => FilePath = f },
                     { "m|include-multiple-branches", "Outputs the commitid for each branch the changeset touches",
                        v => WithChangesetsOnMultipleBranches = (v != null) }
                };

        public int Run()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                return _helper.Run(this);
            }

            var commits = _globals.Repository.GetCommitChangeSetPairs(WithChangesetsOnMultipleBranches);
            File.WriteAllLines(FilePath, commits.Select(map => $"{map.Key}-{string.Join(" ", map.Value)}"));

            return 0;
        }
    }
}
