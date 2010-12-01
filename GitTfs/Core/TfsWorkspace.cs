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
        private readonly IGitTfsRemote _remote;
        private readonly CheckinOptions _checkinOptions;
        private readonly ITfsHelper _tfsHelper;

        public TfsWorkspace(IWorkspace workspace, string localDirectory, TextWriter stdout, TfsChangesetInfo contextVersion, IGitTfsRemote remote, CheckinOptions checkinOptions, ITfsHelper tfsHelper)
        {
            _workspace = workspace;
            _contextVersion = contextVersion;
            _remote = remote;
            _checkinOptions = checkinOptions;
            _tfsHelper = tfsHelper;
            _localDirectory = localDirectory;
            _stdout = stdout;
        }

        public void Shelve(string shelvesetName)
        {
            var pendingChanges = _workspace.GetPendingChanges();

            if (pendingChanges.Count() == 0)
            {
                _stdout.WriteLine(" nothing to shelve");
            }
            else
            {
                var shelveset = _tfsHelper.CreateShelveset(_workspace, shelvesetName);
                shelveset.Comment = _checkinOptions.CheckinComment;
                shelveset.WorkItemInfo = GetWorkItemInfos().ToArray();
                var shelvingOptions = _checkinOptions.Force ? TfsShelvingOptions.Replace : TfsShelvingOptions.None;
                //if(_checkinOptions.Interactive)
                //    if(!_tfsHelper.ShowShelveDialog(_workspace, shelveset, ref pendingChanges, ref shelvingOptions))
                //        return;
                _workspace.Shelve(shelveset, pendingChanges, shelvingOptions);
            }
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
            _stdout.WriteLine(" edit " + path);
            GetFromTfs(path);
            var edited = _workspace.PendEdit(GetLocalPath(path));
            if(edited != 1) throw new Exception("One item should have been edited, but actually edited " + edited + " items.");
        }

        public void Delete(string path)
        {
            _stdout.WriteLine(" delete " + path);
            GetFromTfs(path);
            var deleted = _workspace.PendDelete(GetLocalPath(path));
            if (deleted != 1) throw new Exception("One item should have been deleted, but actually deleted " + deleted + " items.");
        }

        public void Rename(string pathFrom, string pathTo, string score)
        {
            _stdout.WriteLine(" rename " + pathFrom + " to " + pathTo + " (score: " + score + ")");
            GetFromTfs(pathFrom);
            var result = _workspace.PendRename(GetLocalPath(pathFrom), GetLocalPath(pathTo));
            if (result != 1) throw new ApplicationException("Unable to rename item from " + pathFrom + " to " + pathTo);
        }

        private void GetFromTfs(string path)
        {
            _workspace.ForceGetFile(_remote.TfsRepositoryPath + "/" + path, (int) _contextVersion.ChangesetId);
        }

        private IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos()
        {
            var workItemInfos = _tfsHelper.GetWorkItemInfos(_checkinOptions.WorkItemsToAssociate, TfsWorkItemCheckinAction.Associate);
            workItemInfos =
                workItemInfos.Append(_tfsHelper.GetWorkItemInfos(_checkinOptions.WorkItemsToResolve, TfsWorkItemCheckinAction.Resolve));
            return workItemInfos;
        }
    }
}
