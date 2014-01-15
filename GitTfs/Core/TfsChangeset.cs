using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Core
{
    public class TfsChangeset : ITfsChangeset
    {
        private readonly ITfsHelper _tfs;
        private readonly IChangeset _changeset;
        private readonly TextWriter _stdout;
        private readonly AuthorsFile _authors;
        public TfsChangesetInfo Summary { get; set; }
        public int BaseChangesetId { get; set; }

        public TfsChangeset(ITfsHelper tfs, IChangeset changeset, TextWriter stdout, AuthorsFile authors)
        {
            _tfs = tfs;
            _changeset = changeset;
            _stdout = stdout;
            _authors = authors;
            BaseChangesetId = _changeset.Changes.Max(c => c.Item.ChangesetId) - 1;
        }

        public LogEntry Apply(string lastCommit, GitIndexInfo index, ITfsWorkspace workspace)
        {
            var initialTree = Summary.Remote.Repository.GetObjects(lastCommit);
            var resolver = new PathResolver(Summary.Remote, initialTree);
            var sieve = new ChangeSieve(_changeset, resolver);
            workspace.Get(_changeset.ChangesetId, sieve.ChangesToFetch());
            foreach (var change in sieve.ChangesToApply2())
            {
                Apply(change, index, workspace, initialTree);
            }
            return MakeNewLogEntry();
        }

        private void Apply(ApplicableChange change, GitIndexInfo index, ITfsWorkspace workspace, IDictionary<string, GitObject> initialTree)
        {
            switch (change.Type)
            {
                case ChangeType.Update:
                    Update(change, index, workspace, initialTree);
                    break;
                case ChangeType.Delete:
                    Delete(change.GitPath, index, initialTree);
                    break;
                default:
                    throw new NotImplementedException("Unsupported change type: " + change.Type);
            }
        }

        private void Update(ApplicableChange change, GitIndexInfo index, ITfsWorkspace workspace, IDictionary<string, GitObject> initialTree)
        {
            index.Update(Mode.NewFile, change.GitPath, workspace.GetLocalPath(change.GitPath));
        }

        public IEnumerable<TfsTreeEntry> GetTree()
        {
            return GetFullTree().Where(item => item.Item.ItemType == TfsItemType.File && !Summary.Remote.ShouldSkip(item.FullName));
        }

        public bool IsMergeChangeset
        {
            get
            {
                if (_changeset == null || _changeset.Changes == null || !_changeset.Changes.Any())
                    return false;
                return _changeset.Changes.Any(c => c.ChangeType.IncludesOneOf(TfsChangeType.Merge));
            }
        }

        public IEnumerable<TfsTreeEntry> GetFullTree()
        {
            var treeInfo = Summary.Remote.Repository.GetObjects();
            var resolver = new PathResolver(Summary.Remote, treeInfo);
            
            IItem[] tfsItems;
            if(Summary.Remote.TfsRepositoryPath != null)
            {
                tfsItems = _changeset.VersionControlServer.GetItems(Summary.Remote.TfsRepositoryPath, _changeset.ChangesetId, TfsRecursionType.Full);   
            }
            else
            {
                tfsItems = Summary.Remote.TfsSubtreePaths.SelectMany(x => _changeset.VersionControlServer.GetItems(x, _changeset.ChangesetId, TfsRecursionType.Full)).ToArray();
            }
            var tfsItemsWithGitPaths = tfsItems.Select(item => new { item, gitPath = resolver.GetPathInGitRepo(item.ServerItem) });
            return tfsItemsWithGitPaths.Where(x => x.gitPath != null).Select(x => new TfsTreeEntry(x.gitPath, x.item));
        }

        public LogEntry CopyTree(GitIndexInfo index, ITfsWorkspace workspace)
        {
            var startTime = DateTime.Now;
            var itemsCopied = 0;
            var maxChangesetId = 0;
            var tfsTreeEntries = GetTree().ToArray();
            if (tfsTreeEntries.Length == 0)
            {
                maxChangesetId = _changeset.ChangesetId;
            }
            else
            {
                workspace.Get(_changeset.ChangesetId);
                foreach (var entry in tfsTreeEntries)
                {
                    Add(entry.Item, entry.FullName, index, workspace);
                    maxChangesetId = Math.Max(maxChangesetId, entry.Item.ChangesetId);

                    itemsCopied++;
                    if (DateTime.Now - startTime > TimeSpan.FromSeconds(30))
                    {
                        _stdout.WriteLine("{0} objects created...", itemsCopied);
                        startTime = DateTime.Now;
                    }
                }
            }
            return MakeNewLogEntry(maxChangesetId == _changeset.ChangesetId ? _changeset : _tfs.GetChangeset(maxChangesetId));
        }

        private void Add(IItem item, string pathInGitRepo, GitIndexInfo index, ITfsWorkspace workspace)
        {
            if (item.DeletionId == 0)
            {
                index.Update(Mode.NewFile, pathInGitRepo, workspace.GetLocalPath(pathInGitRepo));
            }
        }

        private void Delete(string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            if (initialTree.ContainsKey(pathInGitRepo))
            {
                index.Remove(initialTree[pathInGitRepo].Path);
                Trace.WriteLine("\tD\t" + pathInGitRepo);
            }
        }

        private LogEntry MakeNewLogEntry()
        {
            return MakeNewLogEntry(_changeset, Summary.Remote);
        }

        private LogEntry MakeNewLogEntry(IChangeset changesetToLog, IGitTfsRemote remote = null)
        {
            var identity = _tfs.GetIdentity(changesetToLog.Committer);
            var name = changesetToLog.Committer;
            var email = changesetToLog.Committer;
            if (_authors != null && _authors.Authors.ContainsKey(changesetToLog.Committer))
            {
                name = _authors.Authors[changesetToLog.Committer].Name;
                email = _authors.Authors[changesetToLog.Committer].Email;
            }
            else if (identity != null)
            {
                //This can be null if the user was deleted from AD.
                //We want to keep their original history around with as little 
                //hassle to the end user as possible
                if (!String.IsNullOrWhiteSpace(identity.DisplayName))
                    name = identity.DisplayName;

                if (!String.IsNullOrWhiteSpace(identity.MailAddress))
                    email = identity.MailAddress;
            }
            else if (!String.IsNullOrWhiteSpace(changesetToLog.Committer))
            {
                string[] split = changesetToLog.Committer.Split('\\');
                if (split.Length == 2)
                {
                    name = split[1].ToLower();
                    email = string.Format("{0}@{1}.tfs.local", name, split[0].ToLower());
                }
            }

            // committer's & author's name and email MUST NOT be empty as otherwise they would be picked
            // by git from user.name and user.email config settings which is bad thing because commit could
            // be different depending on whose machine it fetched
            if (String.IsNullOrWhiteSpace(name))
            {
                name = "Unknown TFS user";
            }
            if (String.IsNullOrWhiteSpace(email))
            {
                email = "unknown@tfs.local";
            }
            return new LogEntry
                       {
                           Date = changesetToLog.CreationDate,
                           Log = changesetToLog.Comment + Environment.NewLine,
                           ChangesetId = changesetToLog.ChangesetId,
                           CommitterName = name,
                           AuthorName = name,
                           CommitterEmail = email,
                           AuthorEmail = email,
                           Remote = remote
                       };
        }
    }
}
