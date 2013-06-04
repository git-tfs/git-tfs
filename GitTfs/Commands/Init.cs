﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("init")]
    [Description("init [options] tfs-url-or-instance-name repository-path [git-repository]")]
    public class Init : GitTfsCommand
    {
        private readonly InitOptions initOptions;
        private readonly RemoteOptions remoteOptions;
        private readonly Globals globals;
        private readonly TextWriter output;
        private readonly IGitHelpers gitHelper;
        private readonly IHelpHelper _help;

        public Init(RemoteOptions remoteOptions, InitOptions initOptions, Globals globals, TextWriter output, IGitHelpers gitHelper, IHelpHelper help)
        {
            this.remoteOptions = remoteOptions;
            this.gitHelper = gitHelper;
            _help = help;
            this.output = output;
            this.globals = globals;
            this.initOptions = initOptions;
        }

        public OptionSet OptionSet
        {
            get { return initOptions.OptionSet.Merge(remoteOptions.OptionSet); }
        }

        public bool IsBare
        {
            get { return initOptions.IsBare; }
        }

        public int Run(string tfsUrl, string tfsRepositoryPath)
        {
            tfsRepositoryPath.AssertValidTfsPath();
            DoGitInitDb();
            GitTfsInit(tfsUrl, tfsRepositoryPath);
            return 0;
        }

        public int Run(string tfsUrl, string tfsRepositoryPath, string gitRepositoryPath)
        {
            tfsRepositoryPath.AssertValidTfsPath();
            if (!initOptions.IsBare)
            {
                InitSubdir(gitRepositoryPath);
            }
            else
            {
                Environment.CurrentDirectory = gitRepositoryPath;
                globals.GitDir = ".";
            }
            return Run(tfsUrl, tfsRepositoryPath);
        }

        private void InitSubdir(string repositoryPath)
        {
            if(!Directory.Exists(repositoryPath))
                Directory.CreateDirectory(repositoryPath);
            Environment.CurrentDirectory = repositoryPath;
            globals.GitDir = ".git";
        }

        private void DoGitInitDb()
        {
            if(!Directory.Exists(globals.GitDir) || initOptions.IsBare)
            {
                gitHelper.CommandNoisy(BuildInitCommand());
            }
            globals.Repository = gitHelper.MakeRepository(globals.GitDir);
        }

        private string[] BuildInitCommand()
        {
            var initCommand = new List<string> {"init"};
            if(initOptions.GitInitTemplate!= null)
                initCommand.Add("--template=" + initOptions.GitInitTemplate);
            if(initOptions.IsBare)
                initCommand.Add("--bare");
            if(initOptions.GitInitShared is string)
                initCommand.Add("--shared=" + initOptions.GitInitShared);
            else if(initOptions.GitInitShared != null)
                initCommand.Add("--shared");
            return initCommand.ToArray();
        }

        private void GitTfsInit(string tfsUrl, string tfsRepositoryPath)
        {
            globals.Repository.CreateTfsRemote(new RemoteInfo
            {
                Id = globals.RemoteId,
                Url = tfsUrl,
                Repository = tfsRepositoryPath,
                RemoteOptions = remoteOptions,
            });
        }
    }

    public static partial class Ext
    {
        static Regex ValidTfsPath = new Regex("^\\$/.+");
        public static void AssertValidTfsPath(this string tfsPath)
        {
            if (!ValidTfsPath.IsMatch(tfsPath))
                throw new GitTfsException("TFS repository can not be root and must start with \"$/\".", SuggestPaths(tfsPath));
        }

        private static IEnumerable<string> SuggestPaths(string tfsPath)
        {
            if (tfsPath == "$" || tfsPath == "$/")
                yield return "Cloning an entire TFS repository is not supported. Try using a subdirectory of the root (e.g. $/MyProject).";
            else if (tfsPath.StartsWith("$"))
                yield return "Try using $/" + tfsPath.Substring(1);
            else
                yield return "Try using $/" + tfsPath;
        }

        public static string ToGitRefName(this string expectedRefName)
        {
            expectedRefName = System.Text.RegularExpressions.Regex.Replace(expectedRefName, @"[!~$?[*^: \\]", string.Empty);
            expectedRefName = expectedRefName.Replace("@{", string.Empty);
            expectedRefName = expectedRefName.Replace("..", string.Empty);
            expectedRefName = expectedRefName.Replace("//", string.Empty);
            expectedRefName = expectedRefName.Replace("/.", "/");
            expectedRefName = expectedRefName.TrimEnd('.', '/');
            return expectedRefName.Trim('/');
        }

    }
}
