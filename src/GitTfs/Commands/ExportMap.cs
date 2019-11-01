using System.ComponentModel;
using NDesk.Options;
using Newtonsoft.Json;
using StructureMap;

namespace GitTfs.Commands
{
    [Pluggable("exportmap")]
    [Description("exportmap -f <file> -r <repo>")]
    public class ExportMap : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly Init _init;
        private readonly Help _helper;

        public ExportMap(Globals globals, Init init, Help helper)
        {
            _globals = globals;
            _init = init;
            _helper = helper;
        }

        public string FilePath { get; set; }
        public string RepositoryPath { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                     { "f|file=", "The output file path",
                        f => FilePath = f },
                     { "r|repository=", "The path to the repository",
                        r => RepositoryPath = r }
                };
            }
        }

        public int Run()
        {
            if (string.IsNullOrWhiteSpace(RepositoryPath)
                || string.IsNullOrWhiteSpace(FilePath))
            {
                return _helper.Run(this);
            }

            _globals.Repository = _init.GitHelper.MakeRepository(RepositoryPath);
            var commits = _globals.Repository.GetCommitChangeSetPairs();
            var dic = JsonConvert.SerializeObject(commits);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(FilePath))
            {
                file.WriteLine(dic);
                file.Flush();
            }

            return 0;
        }
    }
}
