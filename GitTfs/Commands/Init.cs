using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("init")]
    [Description("init [options] tfs-url repository-path [git-repository]")]
    public class Init : GitTfsCommand
    {
        [OptDef(OptValType.ValueReq)]
        [Description("The --template option to pass to git-init.")]
        public string template { get; set; }

        [OptDef(OptValType.ValueOpt)]
        [Description("The --shared option to pass to git-init.")]
        public object shared { get; set; }
//
//        [OptDef(OptValType.ValueReq)]
//        [ShortOptionName('u')]
//        [Description("The URL or alias of the TFS server to use.")]
//        public string tfs { get; set; }
//
//        [OptDef(OptValType.ValueReq)]
//        [ShortOptionName('r')]
//        [LongOptionName("repo-path")]
//        [UseNameAsLongOption(false)]
//        [Description("The repository path in TFS that this git repository will be a mirror of.")]
//        public string RepositoryPath { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("no-metadata")]
        [UseNameAsLongOption(false)]
        [Description("If specified, git-tfs will leave out the git-tfs-id: lines at the end of every commit.")]
        public bool NoMetaData { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("ignore-regex")]
        [UseNameAsLongOption(false)]
        [Description("If specified, git-tfs will not sync any paths that match this regular expression.")]
        public bool IgnoreRegex { get; set; }

        [OptDef(OptValType.ValueReq)]
        [Description("Your TFS username, including domain.")]
        public string username { get; set; }

        [OptDef(OptValType.ValueReq)]
        [Description("An optional remote ID, useful if this repository will track multiple TFS repositories.")]
        public string id { get; set; }

        //private GitTfs gitTfs;

        public Init()
        {
            id = "tfs"; // The default TFS remote id
        }

        public int Run(IEnumerable<string> args)
        {
            InitSubdir(args);
            GitTfsInit(args);
            return 0;
        }

        private void InitSubdir(IList<string> args)
        {
            var repositoryPath = args.Count == 3 ? args[2] : ".";
            if(!Directory.Exists(repositoryPath))
                Directory.CreateDirectory(repositoryPath);
            Environment.ChangeDirectory(repositoryPath);
            GIT_DIR = ".git";
            repository = git.Repository(GIT_DIR);
        }

        private void GitTfsInit(IList<string> args)
        {
            if(!Directory.Exists(GIT_DIR))
            {
                var initArgs = new List<string>();
                initArgs.Add("init");
                if(template != null)
                {
                    initArgs.Add("--template=" + template);
                }
                if(shared != null)
                {
                    if(shared is String)
                    {
                        initArgs.Add("--shared=" + shared);
                    }
                    else
                    {
                        initArgs.Add("--shared");
                    }
                }
                CommandNoisy(initArgs);
                repository = git.Repository(".git");
            }
            // git-svn does this, but I don't know if I want to or not.
            //CommandNoisy("config", "core.autocrlf", "false");
            var prefix = "tfs-remote." + id;
            if(NoMetaData)  CommandNoisy("config", prefix + ".no-meta-data", 1);
            if(username)    CommandNoisy("config", prefix + ".username", username);
            if(IgnoreRegex) CommandNoisy("config", prefix + ".ignore-paths", IgnoreRegex);
            CommandNoisy("config", prefix + ".url" + args[0]);
            CommandNoisy("config", prefix + ".repository" + args[1]);
        }
    }
}
