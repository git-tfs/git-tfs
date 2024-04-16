using GitTfs.Commands;
using GitTfs.Core.TfsInterop;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace GitTfs.Core
{
    public class TfsWorkspace : ITfsWorkspace
    {
        private readonly IWorkspace _workspace;
        private readonly string _localDirectory;
        private readonly TfsChangesetInfo _contextVersion;
        private readonly CheckinOptions _checkinOptions;
        private readonly ITfsHelper _tfsHelper;
        private readonly CheckinPolicyEvaluator _policyEvaluator;

        private const string CheckinPolicyNoteMessage =
            "Note: If the checkin policy fails because the assemblies failed to load, please run the file `enable_checkin_policies_support.bat` in the git-tfs directory and try again.";

        public IGitTfsRemote Remote { get; private set; }

        public TfsWorkspace(IWorkspace workspace, string localDirectory, TfsChangesetInfo contextVersion, IGitTfsRemote remote, CheckinOptions checkinOptions, ITfsHelper tfsHelper, CheckinPolicyEvaluator policyEvaluator)
        {
            _workspace = workspace;
            _policyEvaluator = policyEvaluator;
            _contextVersion = contextVersion;
            _checkinOptions = checkinOptions;
            _tfsHelper = tfsHelper;
            _localDirectory = remote.Repository.IsBare ? Path.GetFullPath(localDirectory) : localDirectory;

            Remote = remote;
        }

        public void Shelve(string shelvesetName, bool evaluateCheckinPolicies, CheckinOptions checkinOptions, Func<string> generateCheckinComment)
        {
            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.IsNullOrEmpty())
                throw new GitTfsException("Nothing to shelve!");

            var shelveset = _tfsHelper.CreateShelveset(_workspace, shelvesetName);
            shelveset.Comment = string.IsNullOrWhiteSpace(_checkinOptions.CheckinComment) && !_checkinOptions.NoGenerateCheckinComment ? generateCheckinComment() : _checkinOptions.CheckinComment;
            shelveset.WorkItemInfo = GetWorkItemInfos(checkinOptions).ToArray();
            if (evaluateCheckinPolicies)
            {
                var checkinProblems = _policyEvaluator.EvaluateCheckin(_workspace, pendingChanges, shelveset.Comment, null, shelveset.WorkItemInfo);
                TraceCheckinPolicyErrors(checkinProblems, false);
            }
            _workspace.Shelve(shelveset, pendingChanges, _checkinOptions.Force ? TfsShelvingOptions.Replace : TfsShelvingOptions.None);
        }

        public void DeleteShelveset(string shelvesetName) => _tfsHelper.DeleteShelveset(_workspace, shelvesetName);

        public int CheckinTool(Func<string> generateCheckinComment)
        {
            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.IsNullOrEmpty())
                throw new GitTfsException("Nothing to checkin!");

            var checkinComment = _checkinOptions.CheckinComment;
            if (string.IsNullOrWhiteSpace(checkinComment) && !_checkinOptions.NoGenerateCheckinComment)
                checkinComment = generateCheckinComment();

            var newChangesetId = _tfsHelper.ShowCheckinDialog(_workspace, pendingChanges, GetWorkItemCheckedInfos(), checkinComment);
            if (newChangesetId <= 0)
                throw new GitTfsException("Checkin canceled!");
            return newChangesetId;
        }

        public void Merge(string sourceTfsPath, string tfsRepositoryPath) => _workspace.Merge(sourceTfsPath, tfsRepositoryPath);

        private static void TraceCheckinPolicyErrors(CheckinPolicyEvaluator.CheckinPolicyEvaluationResult checkinProblems, bool overridePolicyErrors)
        {
            string prefix = overridePolicyErrors ? "[OVERRIDDEN] " : "[ERROR] ";
            foreach (var message in checkinProblems.Messages)
            {
                Trace.TraceWarning(prefix + message);
            }

            if (checkinProblems.HasErrors && !overridePolicyErrors)
                Trace.TraceInformation("Note: If the checkin policy fails because the assemblies failed to load, please run the file `enable_checkin_policies_support.bat` in the git-tfs directory and try again.");
        }

        public int Checkin(CheckinOptions options, Func<string> generateCheckinComment = null)
        {
            if (options == null) options = _checkinOptions;

            var checkinComment = options.CheckinComment;
            if (string.IsNullOrWhiteSpace(checkinComment) && !options.NoGenerateCheckinComment && generateCheckinComment != null)
                checkinComment = generateCheckinComment();

            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.IsNullOrEmpty())
                throw new GitTfsException("Nothing to checkin!");

            var workItemInfos = GetWorkItemInfos(options);
            var checkinNote = _tfsHelper.CreateCheckinNote(options.CheckinNotes);

            var checkinProblems = _policyEvaluator.EvaluateCheckin(_workspace, pendingChanges, checkinComment, checkinNote, workItemInfos);
            if (checkinProblems.HasErrors)
            {
                bool overridePolicyErrors = options.Force && !string.IsNullOrWhiteSpace(options.OverrideReason);
                TraceCheckinPolicyErrors(checkinProblems, overridePolicyErrors);

                if (!options.Force)
                {
                    throw new GitTfsException("No changes checked in.");
                }
                if (string.IsNullOrWhiteSpace(options.OverrideReason))
                {
                    throw new GitTfsException("A reason must be supplied (-f REASON) to override the policy violations.");
                }
            }

            var policyOverride = GetPolicyOverrides(options, checkinProblems.Result);
            try
            {
                var newChangeset = _workspace.Checkin(pendingChanges, checkinComment, options.AuthorTfsUserId, checkinNote, workItemInfos, policyOverride, options.OverrideGatedCheckIn);
                if (newChangeset == 0)
                {
                    throw new GitTfsException("Checkin failed!");
                }
                else
                {
                    return newChangeset;
                }
            }
            catch (GitTfsGatedCheckinException e)
            {
                return LaunchGatedCheckinBuild(e.AffectedBuildDefinitions, e.ShelvesetName, e.CheckInTicket);
            }
        }

        private int LaunchGatedCheckinBuild(ReadOnlyCollection<KeyValuePair<string, Uri>> affectedBuildDefinitions, string shelvesetName, string checkInTicket)
        {
            Trace.TraceInformation("Due to a gated check-in, a shelveset '" + shelvesetName + "' containing your changes has been created and need to be built before it can be committed.");
            KeyValuePair<string, Uri> buildDefinition;
            if (affectedBuildDefinitions.Count == 1)
            {
                buildDefinition = affectedBuildDefinitions.First();
            }
            else
            {
                int choice;
                do
                {
                    Trace.TraceInformation("Build definitions that can be used:");
                    for (int i = 0; i < affectedBuildDefinitions.Count; i++)
                    {
                        Trace.TraceInformation((i + 1) + ": " + affectedBuildDefinitions[i].Key);
                    }
                    Trace.TraceInformation("Please choose the build definition to trigger?");
                } while (!int.TryParse(Console.ReadLine(), out choice) || choice <= 0 || choice > affectedBuildDefinitions.Count);
                buildDefinition = affectedBuildDefinitions.ElementAt(choice - 1);
            }
            return Remote.Tfs.QueueGatedCheckinBuild(buildDefinition.Value, buildDefinition.Key, shelvesetName, checkInTicket);
        }

        private TfsPolicyOverrideInfo GetPolicyOverrides(CheckinOptions options, ICheckinEvaluationResult checkinProblems)
        {
            if (!options.Force || string.IsNullOrWhiteSpace(options.OverrideReason))
                return null;
            return new TfsPolicyOverrideInfo { Comment = options.OverrideReason, Failures = checkinProblems.PolicyFailures };
        }

        public string GetLocalPath(string path) => Path.Combine(_localDirectory, path);

        public void Add(string path)
        {
            Trace.TraceInformation(" add " + path);
            var added = _workspace.PendAdd(GetLocalPath(path));
            if (added != 1) throw new Exception("One item should have been added, but actually added " + added + " items.");
        }

        public void Edit(string path)
        {
            var localPath = GetLocalPath(path);
            Trace.TraceInformation(" edit " + localPath);
            GetFromTfs(localPath);
            var edited = _workspace.PendEdit(localPath);
            if (edited != 1)
            {
                if (_checkinOptions.IgnoreMissingItems)
                {
                    Trace.TraceWarning("Warning: One item should have been edited, but actually edited " + edited + ". Ignoring item.");
                }
                else if (edited == 0 && _checkinOptions.AddMissingItems)
                {
                    Trace.TraceWarning("Warning: One item should have been edited, but was not found. Adding the file instead.");
                    Add(path);
                }
                else
                {
                    throw new Exception("One item should have been edited, but actually edited " + edited + " items.");
                }
            }
        }

        public void Delete(string path)
        {
            path = GetLocalPath(path);
            Trace.TraceInformation(" delete " + path);
            GetFromTfs(path);
            var deleted = _workspace.PendDelete(path);
            if (deleted != 1) throw new Exception("One item should have been deleted, but actually deleted " + deleted + " items.");
        }

        public void Rename(string pathFrom, string pathTo, string score)
        {
            Trace.TraceInformation(" rename " + pathFrom + " to " + pathTo + " (score: " + score + ")");
            GetFromTfs(GetLocalPath(pathFrom));
            var result = _workspace.PendRename(GetLocalPath(pathFrom), GetLocalPath(pathTo));
            if (result != 1) throw new ApplicationException("Unable to rename item from " + pathFrom + " to " + pathTo);
        }

        private void GetFromTfs(string path) => _workspace.ForceGetFile(_workspace.GetServerItemForLocalItem(path), _contextVersion.ChangesetId);

        public void Get(int changesetId) => _workspace.GetSpecificVersion(changesetId);

        public void Get(int changesetId, IEnumerable<IItem> items) => _workspace.GetSpecificVersion(changesetId, items, Remote.RemoteInfo.NoParallel);

        public void Get(IChangeset changeset) => _workspace.GetSpecificVersion(changeset, Remote.RemoteInfo.NoParallel);

        public void Get(int changesetId, IEnumerable<IChange> changes)
        {
            if (changes.Any())
            {
                _workspace.GetSpecificVersion(changesetId, changes, Remote.RemoteInfo.NoParallel);
            }
        }

        public string GetLocalItemForServerItem(string serverItem) => _workspace.GetLocalItemForServerItem(serverItem);

        private IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(CheckinOptions options = null) => GetWorkItemInfosHelper<IWorkItemCheckinInfo>(_tfsHelper.GetWorkItemInfos, options);

        private IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos() => GetWorkItemInfosHelper<IWorkItemCheckedInfo>(_tfsHelper.GetWorkItemCheckedInfos);

        private IEnumerable<T> GetWorkItemInfosHelper<T>(Func<IEnumerable<string>, TfsWorkItemCheckinAction, IEnumerable<T>> func, CheckinOptions options = null)
        {
            var checkinOptions = options ?? _checkinOptions;

            var workItemInfos = func(checkinOptions.WorkItemsToAssociate, TfsWorkItemCheckinAction.Associate);
            workItemInfos = workItemInfos.Concat(
                func(checkinOptions.WorkItemsToResolve, TfsWorkItemCheckinAction.Resolve));
            return workItemInfos;
        }
    }
}
