using System;
using System.Diagnostics;
using System.IO;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core
{
    public class TfsWorkspace : ITfsWorkspace
    {
        private readonly Workspace _workspace;
        private readonly string _localDirectory;
        private readonly TextWriter _stdout;
        private readonly TfsChangesetInfo _contextVersion;

        public TfsWorkspace(Workspace workspace, string localDirectory, TextWriter stdout, TfsChangesetInfo contextVersion)
        {
            _workspace = workspace;
            _contextVersion = contextVersion;
            _localDirectory = localDirectory;
            _stdout = stdout;
            _workspace.Get(new ChangesetVersionSpec((int) contextVersion.ChangesetId), GetOptions.None);
        }

        public void Shelve(string shelvesetName)
        {
            _workspace.Shelve(new Shelveset(_workspace.VersionControlServer, shelvesetName, _workspace.OwnerName), _workspace.GetPendingChanges(), ShelvingOptions.Replace);
        }

        public string GetLocalPath(string path)
        {
            return Path.Combine(_localDirectory, path);
        }

        public void Add(string path)
        {
            _stdout.WriteLine(" add " + path);
            var added = _workspace.PendAdd(GetLocalPath(path));
            Debug.Assert(added == 1, "one item should have been added, but actually added " + added + " items.");
        }

        public void Edit(string path)
        {
            _stdout.WriteLine(" edit " + path);
            var edited = _workspace.PendEdit(GetLocalPath(path));
            Debug.Assert(edited == 1, "one item should have been edited, but actually edited " + edited + " items.");
        }

        public void Delete(string path)
        {
            _stdout.WriteLine(" delete " + path);
            var deleted = _workspace.PendDelete(GetLocalPath(path));
            Debug.Assert(deleted == 1, "one item should have been deleted, but actually deleted " + deleted + " items.");
        }
    }
}