using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NDesk.Options;
using GitTfs.Core;
using StructureMap;
using GitTfs.Util;

namespace GitTfs.Commands
{
    [Pluggable("labels")]
    [Description("labels [options] [tfsRemoteId]\n ex : git tfs labels\n      git tfs labels -i myRemoteBranche\n      git tfs labels --all")]
    [RequiresValidGitRepository]
    public class Labels : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly AuthorsFile _authors;

        public string TfsUsername { get; set; }
        public string TfsPassword { get; set; }
        public bool LabelAllBranches { get; set; }
        public string NameFilter { get; set; }
        public string ExcludeNameFilter { get; set; }

        public Labels(Globals globals, AuthorsFile authors)
        {
            _globals = globals;
            _authors = authors;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "all|fetch-all", "Fetch all the labels on all the TFS remotes (For TFS 2010 and later)", v => LabelAllBranches = v != null },
                    { "n|label-name=", "Fetch all the labels respecting this name filter", v => NameFilter = v },
                    { "e|exclude-label-name=", "Exclude all the labels respecting this regex name filter", v => ExcludeNameFilter = v },
                    { "u|username=", "TFS username", v => TfsUsername = v },
                    { "p|password=", "TFS password", v => TfsPassword = v },
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

            return CreateLabelsForTfsBranch(tfsRemote);
        }

        public int Run()
        {
            if (!LabelAllBranches)
            {
                return Run(_globals.RemoteId);
            }

            var allRemotes = _globals.Repository.ReadAllTfsRemotes();

            foreach (var tfsRemote in allRemotes)
            {
                CreateLabelsForTfsBranch(tfsRemote);
            }
            return GitTfsExitCodes.OK;
        }

        private int CreateLabelsForTfsBranch(IGitTfsRemote tfsRemote)
        {
            if (string.IsNullOrWhiteSpace(NameFilter))
                NameFilter = null;
            else
                NameFilter = NameFilter.Trim();

            UpdateRemote(tfsRemote);
            Trace.TraceInformation("Looking for label on " + tfsRemote.TfsRepositoryPath + "...");
            var labels = tfsRemote.Tfs.GetLabels(tfsRemote.TfsRepositoryPath, NameFilter).ToList();
            Trace.TraceInformation(labels.Count() + " labels found!");

            Regex exludeRegex = null;
            if (ExcludeNameFilter != null)
                exludeRegex = new Regex(ExcludeNameFilter);

            foreach (var label in labels)
            {
                if (ExcludeNameFilter != null && exludeRegex.IsMatch(label.Name))
                    continue;

                Trace.WriteLine("LabelId:" + label.Id + "/ChangesetId:" + label.ChangesetId + "/LabelName:" + label.Name + "/Owner:" + label.Owner);
                Trace.WriteLine("Try to find changeset in git repository...");
                string sha1TagCommit = _globals.Repository.FindCommitHashByChangesetId(label.ChangesetId);
                if (string.IsNullOrWhiteSpace(sha1TagCommit))
                {
                    Trace.WriteLine("This label does not match an existing commit...");
                    continue;
                }
                Trace.WriteLine("Commit found! sha1 : " + sha1TagCommit);

                string ownerName;
                string ownerEmail;
                if (_authors.Authors.ContainsKey(label.Owner))
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
                var labelName = (label.IsTransBranch ? label.Name + "(" + tfsRemote.Id + ")" : label.Name).ToGitRefName();
                Trace.TraceInformation("Writing label '" + labelName + "'...");
                _globals.Repository.CreateTag(labelName, sha1TagCommit, label.Comment, ownerName, ownerEmail, label.Date);
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
