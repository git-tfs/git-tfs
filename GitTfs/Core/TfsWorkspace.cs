using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class TfsWorkspace : ITfsWorkspace
    {
        private readonly IWorkspace _workspace;
        private readonly string _localDirectory;
        private readonly TextWriter _stdout;
        private readonly TfsChangesetInfo _contextVersion;
        private readonly CheckinOptions _checkinOptions;
        private readonly ITfsHelper _tfsHelper;
        private readonly CheckinPolicyEvaluator _policyEvaluator;

        public IGitTfsRemote Remote { get; private set; }

        public TfsWorkspace(IWorkspace workspace, string localDirectory, TextWriter stdout, TfsChangesetInfo contextVersion, IGitTfsRemote remote, CheckinOptions checkinOptions, ITfsHelper tfsHelper, CheckinPolicyEvaluator policyEvaluator)
        {
            _workspace = workspace;
            _policyEvaluator = policyEvaluator;
            _contextVersion = contextVersion;
            _checkinOptions = checkinOptions;
            _tfsHelper = tfsHelper;
            _localDirectory = localDirectory;
            _stdout = stdout;

            this.Remote = remote;
        }

        public void Shelve(string shelvesetName, bool evaluateCheckinPolicies, Func<string> generateCheckinComment)
        {
            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.IsEmpty())
                throw new GitTfsException("Nothing to shelve!");

            var shelveset = _tfsHelper.CreateShelveset(_workspace, shelvesetName);
            shelveset.Comment = string.IsNullOrWhiteSpace(_checkinOptions.CheckinComment) && !_checkinOptions.NoGenerateCheckinComment ? generateCheckinComment() : _checkinOptions.CheckinComment;
            shelveset.WorkItemInfo = GetWorkItemInfos().ToArray();
            if (evaluateCheckinPolicies)
            {
                foreach (var message in _policyEvaluator.EvaluateCheckin(_workspace, pendingChanges, shelveset.Comment, null, shelveset.WorkItemInfo).Messages)
                {
                    _stdout.WriteLine("[Checkin Policy] " + message);
                }
            }
            _workspace.Shelve(shelveset, pendingChanges, _checkinOptions.Force ? TfsShelvingOptions.Replace : TfsShelvingOptions.None);
        }

        public long CheckinTool(Func<string> generateCheckinComment)
        {
            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.IsEmpty())
                throw new GitTfsException("Nothing to checkin!");

            var checkinComment = _checkinOptions.CheckinComment;
            if (string.IsNullOrWhiteSpace(checkinComment) && !_checkinOptions.NoGenerateCheckinComment)
                checkinComment = generateCheckinComment();

            var newChangesetId = _tfsHelper.ShowCheckinDialog(_workspace, pendingChanges, GetWorkItemCheckedInfos(), checkinComment);
            if (newChangesetId <= 0)
                throw new GitTfsException("Checkin cancelled!");
            return newChangesetId;
        }

        public void Merge(string sourceTfsPath, string tfsRepositoryPath)
        {
            _workspace.Merge(sourceTfsPath, tfsRepositoryPath);
        }

        public long Checkin(CheckinOptions options)
        {
            if (options == null) options = _checkinOptions;

            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.IsEmpty())
                throw new GitTfsException("Nothing to checkin!");

            var workItemInfos = GetWorkItemInfos(options);
            var checkinNote = _tfsHelper.CreateCheckinNote(options.CheckinNotes);

            var checkinProblems = _policyEvaluator.EvaluateCheckin(_workspace, pendingChanges, options.CheckinComment, checkinNote, workItemInfos);
            if (checkinProblems.HasErrors)
            {
                foreach (var message in checkinProblems.Messages)
                {
                    if (options.Force && string.IsNullOrWhiteSpace(options.OverrideReason) == false)
                    {
                        _stdout.WriteLine("[OVERRIDDEN] " + message);
                    }
                    else
                    {
                        _stdout.WriteLine("[ERROR] " + message);
                    }
                }
                if (!options.Force)
                {
                    throw new GitTfsException("No changes checked in.");
                }
                if (String.IsNullOrWhiteSpace(options.OverrideReason))
                {
                    throw new GitTfsException("A reason must be supplied (-f REASON) to override the policy violations.");
                }
            }

            var policyOverride = GetPolicyOverrides(options, checkinProblems.Result);
            var newChangeset = _workspace.Checkin(pendingChanges, options.CheckinComment, options.AuthorTfsUserId, checkinNote, workItemInfos, policyOverride, options.OverrideGatedCheckIn);
            if (newChangeset == 0)
            {
                throw new GitTfsException("Checkin failed!");
            }
            else
            {
                return newChangeset;
            }
        }

        private TfsPolicyOverrideInfo GetPolicyOverrides(CheckinOptions options, ICheckinEvaluationResult checkinProblems)
        {
            if (!options.Force || String.IsNullOrWhiteSpace(options.OverrideReason))
                return null;
            return new TfsPolicyOverrideInfo { Comment = options.OverrideReason, Failures = checkinProblems.PolicyFailures };
        }

        public string GetLocalPath(string path)
        {
            return Path.Combine(_localDirectory, path);
        }

        public void Add(string path)
        {
            _stdout.WriteLine(" add " + path);
            var added = _workspace.PendAdd(GetLocalPath(path));
            if (added != 1) throw new Exception("One item should have been added, but actually added " + added + " items.");
        }

        public void Edit(string path)
        {
            path = GetLocalPath(path);
            _stdout.WriteLine(" edit " + path);
            GetFromTfs(path);
            var edited = _workspace.PendEdit(path);
            if (edited != 1) throw new Exception("One item should have been edited, but actually edited " + edited + " items.");
        }

        public void Delete(string path)
        {
            path = GetLocalPath(path);
            _stdout.WriteLine(" delete " + path);
            GetFromTfs(path);
            var deleted = _workspace.PendDelete(path);
            if (deleted != 1) throw new Exception("One item should have been deleted, but actually deleted " + deleted + " items.");
        }

        public void Rename(string pathFrom, string pathTo, string score)
        {
            _stdout.WriteLine(" rename " + pathFrom + " to " + pathTo + " (score: " + score + ")");
            GetFromTfs(GetLocalPath(pathFrom));
            var result = _workspace.PendRename(GetLocalPath(pathFrom), GetLocalPath(pathTo));
            if (result != 1) throw new ApplicationException("Unable to rename item from " + pathFrom + " to " + pathTo);
        }

        private void GetFromTfs(string path)
        {
            _workspace.ForceGetFile(_workspace.GetServerItemForLocalItem(path), (int)_contextVersion.ChangesetId);
        }

        public void Get(int changesetId)
        {
            _workspace.GetSpecificVersion(changesetId);
        }

        public void Get(IChangeset changeset)
        {
            _workspace.GetSpecificVersion(changeset);
        }

        public void Get(int changesetId, IEnumerable<IChange> changes)
        {
            if (changes.Any())
            {
                _workspace.GetSpecificVersion(changesetId, changes);
            }
        }

        private IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(CheckinOptions options = null)
        {
            return GetWorkItemInfosHelper<IWorkItemCheckinInfo>(_tfsHelper.GetWorkItemInfos, options);
        }

        private IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos()
        {
            return GetWorkItemInfosHelper<IWorkItemCheckedInfo>(_tfsHelper.GetWorkItemCheckedInfos);
        }

        private IEnumerable<T> GetWorkItemInfosHelper<T>(Func<IEnumerable<string>, TfsWorkItemCheckinAction, IEnumerable<T>> func, CheckinOptions options = null)
        {
            var checkinOptions = options ?? _checkinOptions;

            var workItemInfos = func(checkinOptions.WorkItemsToAssociate, TfsWorkItemCheckinAction.Associate);
            workItemInfos = workItemInfos.Append(
                func(checkinOptions.WorkItemsToResolve, TfsWorkItemCheckinAction.Resolve));
            return workItemInfos;
        }
    }
}
