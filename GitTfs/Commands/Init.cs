using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

        public IGitHelpers GitHelper
        {
            get { return gitHelper; }
        }

        public int Run(string tfsUrl, string tfsRepositoryPath)
        {
            tfsRepositoryPath.AssertValidTfsPathOrRoot();
            DoGitInitDb();
            GitTfsInit(tfsUrl, tfsRepositoryPath);
            return 0;
        }

        public int Run(string tfsUrl, string tfsRepositoryPath, string gitRepositoryPath)
        {
            tfsRepositoryPath.AssertValidTfsPathOrRoot();
            if (!initOptions.IsBare)
            {
                InitSubdir(gitRepositoryPath);
            }
            else
            {
                Environment.CurrentDirectory = gitRepositoryPath;
                globals.GitDir = ".";
            }
            var runResult = Run(tfsUrl, tfsRepositoryPath);
            try
            {
                File.WriteAllText(@".git\description", tfsRepositoryPath + "\n" + HideUserCredentials(globals.CommandLineRun));
            }
            catch (Exception)
            {
                Trace.WriteLine("warning: Unable to update de repository description!");
            }
            return runResult;
        }

        public static string HideUserCredentials(string commandLineRun)
        {
            Regex rgx = new Regex("(--username|-u)[= ][^ ]+");
            commandLineRun = rgx.Replace(commandLineRun, "--username=xxx");
            rgx = new Regex("(--password|-p)[= ][^ ]+");
            return rgx.Replace(commandLineRun, "--password=xxx");
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

            if (!string.IsNullOrWhiteSpace(initOptions.WorkspacePath))
            {
                Trace.WriteLine("workspace path:" + initOptions.WorkspacePath);

                try
                {
                    var dir = Directory.CreateDirectory(initOptions.WorkspacePath);
                    globals.Repository.SetConfig(GitTfsConstants.WorkspaceConfigKey, initOptions.WorkspacePath);

                }
                catch (Exception)
                {
                    throw new GitTfsException("error: workspace path is invalid!");
                }
            }

            globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, false.ToString());

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
            },initOptions.GitInitAutoCrlf,initOptions.GitInitIgnoreCase);
        }
    }

    public static class Ext
    {
        private static Regex ValidTfsPath = new Regex("^\\$/.+");
        public static bool IsValidTfsPath(this string tfsPath)
        {
            return ValidTfsPath.IsMatch(tfsPath);
        }

        public static void AssertValidTfsPathOrRoot(this string tfsPath)
        {
            if (tfsPath == GitTfsConstants.TfsRoot)
                return;
            AssertValidTfsPath(tfsPath);
        }

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

        public static string ToGitBranchNameFromTfsRepositoryPath(this string tfsRepositoryPath, bool includeTeamProjectName = false)
        {
            if (includeTeamProjectName)
            {
                return tfsRepositoryPath
                    .Replace("$/", String.Empty)
                    .ToGitRefName();
            }

            string gitBranchNameExpected = tfsRepositoryPath.IndexOf("$/") == 0
                ? tfsRepositoryPath.Remove(0, tfsRepositoryPath.IndexOf('/', 2) + 1)
                : tfsRepositoryPath;

            return gitBranchNameExpected.ToGitRefName();
        }

        public static string ToTfsTeamProjectRepositoryPath(this string tfsRepositoryPath)
        {
            if (!tfsRepositoryPath.StartsWith("$/"))
            {
                return tfsRepositoryPath;
            }

            var index = tfsRepositoryPath.IndexOf('/', 2);
            if (index == -1)
            {
                return tfsRepositoryPath;
            }

            return tfsRepositoryPath.Remove(index, tfsRepositoryPath.Length - index);
        }

        public static string ToLocalGitRef(this string refName)
        {
            return "refs/heads/" + refName;
        }

    }
}
