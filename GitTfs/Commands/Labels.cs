using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("labels")]
    [Description("labels [options] [tfsRemoteId]\n ex : git tfs labels\n      git tfs labels -i myRemoteBranche\n      git tfs labels --all")]
    [RequiresValidGitRepository]
    public class Labels : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly Globals _globals;
        private readonly AuthorsFile _authors;

        private RemoteOptions _remoteOptions;
        public string TfsUsername { get; set; }
        public string TfsPassword { get; set; }
        public string ParentBranch { get; set; }
        public bool LabelAllBranches { get; set; }
        string AuthorsFilePath { get; set; }

        public Labels(TextWriter stdout, Globals globals, AuthorsFile authors)
        {
            this._stdout = stdout;
            this._globals = globals;
            this._authors = authors;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "all|fetch-all", "Fetch all the labels on all the TFS remotes (For TFS 2010 and later)", v => LabelAllBranches = v != null },
                    { "u|username=", "TFS username", v => TfsUsername = v },
                    { "p|password=", "TFS password", v => TfsPassword = v },
                    { "a|authors=", "Path to an Authors file to map TFS users to Git users", v => AuthorsFilePath = v },
                };
            }
        }

        public int Run(IGitTfsRemote remote)
        {
            return CreateLabelsForTfsBranch(remote);
        }

        private int Run(string remoteId)
        {
            var tfsRemote = _globals.Repository.ReadTfsRemote(remoteId);
            if (tfsRemote == null)
                throw new GitTfsException("error: No git-tfs repository found. Please try to clone first...\n");

            _authors.Parse(AuthorsFilePath, _globals.GitDir);

            return CreateLabelsForTfsBranch(tfsRemote);
        }

        public int Run()
        {
            if (!LabelAllBranches)
            {
                return Run(_globals.RemoteId);
            }

            var allRemotes = _globals.Repository.ReadAllTfsRemotes();

            _authors.Parse(AuthorsFilePath, _globals.GitDir);

            foreach (var tfsRemote in allRemotes)
            {
                CreateLabelsForTfsBranch(tfsRemote);
            }
            return GitTfsExitCodes.OK;
        }

        private int CreateLabelsForTfsBranch(IGitTfsRemote tfsRemote)
        {
            UpdateRemote(tfsRemote);
            Trace.WriteLine("Looking for label on " + tfsRemote.TfsRepositoryPath);
            var labels = tfsRemote.Tfs.GetLabels(tfsRemote.TfsRepositoryPath);
            foreach (var label in labels)
            {
                Trace.WriteLine("LabelId:" + label.Id + "/ChangesetId:" + label.ChangesetId + "/LabelName:" + label.Name + "/Owner:" + label.Owner);
                Trace.WriteLine("Try to find changeset in git repository...");
                string sha1TagCommit = _globals.Repository.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + label.ChangesetId + "[^0-9]");
                if (string.IsNullOrWhiteSpace(sha1TagCommit))
                {
                    Trace.WriteLine("This label does not match an existing commit...");
                    continue;
                }
                Trace.WriteLine("Commit found! sha1 : " + sha1TagCommit);

                string ownerName;
                string ownerEmail;
                if(_authors.Authors.ContainsKey(label.Owner))
                {
                    var author = _authors.Authors[label.Owner];
                    ownerName = author.Name;
                    ownerEmail = author.Email;
                }
                else
                {
                    ownerName = label.Owner;
                    ownerEmail = label.Owner;
                }
                var labelName = label.IsTransBranch ? label.Name + "(" + tfsRemote.Id + ")" : label.Name;
                _stdout.WriteLine("Writing label '" + labelName + "'...");
                _globals.Repository.CreateTag(labelName, sha1TagCommit, label.Comment, ownerName, ownerEmail ,label.Date);
            }
            return GitTfsExitCodes.OK;
        }

        private void UpdateRemote(IGitTfsRemote tfsRemote)
        {
            if (!string.IsNullOrWhiteSpace(TfsUsername))
            {
                tfsRemote.TfsUsername = TfsUsername;
                tfsRemote.TfsPassword = TfsPassword;
            }
        }
    }
}
