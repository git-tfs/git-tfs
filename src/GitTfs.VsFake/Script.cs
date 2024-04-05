using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

using GitTfs.Core;
using GitTfs.Core.TfsInterop;

namespace GitTfs.VsFake
{
    public class Script
    {
        public const string EnvVar = "GIT_TFS_VSFAKE_SCRIPT";

        public static Script Load(string path) => new Script().Tap(script => Load(path, script));

        private static void Load(string path, Script script)
        {
            var formatter = new BinaryFormatter();
            using (var stream = File.OpenRead(path))
            {
                script.RootBranches.AddRange((List<ScriptedRootBranch>)formatter.Deserialize(stream));
                script.Changesets.AddRange((List<ScriptedChangeset>)formatter.Deserialize(stream));
            }
        }

        public void Save(string path)
        {
            var formatter = new BinaryFormatter();
            using (var stream = File.Create(path))
            {
                formatter.Serialize(stream, RootBranches);
                formatter.Serialize(stream, Changesets);
            }
        }

        private readonly List<ScriptedChangeset> _changesets = new List<ScriptedChangeset>();
        public List<ScriptedChangeset> Changesets => _changesets;

        private readonly List<ScriptedRootBranch> _rootBranches = new List<ScriptedRootBranch>();
        public List<ScriptedRootBranch> RootBranches => _rootBranches;
    }

    [Serializable]
    [DebuggerDisplay("{Id}")]
    public class ScriptedChangeset
    {
        public int Id { get; set; }
        public string Comment { get; set; }
        public DateTime CheckinDate { get; set; }
        public List<ScriptedChange> Changes => _changes;

        public bool IsBranchChangeset { get; set; }
        public BranchChangesetDatas BranchChangesetDatas { get; set; }

        public bool IsMergeChangeset { get; set; }
        public MergeChangesetDatas MergeChangesetDatas { get; set; }
        public string Committer { get; set; }

        private readonly List<ScriptedChange> _changes = new List<ScriptedChange>();
    }

    [Serializable]
    [DebuggerDisplay("{ChangeType}/{ItemType}/{RepositoryPath}/{ItemId}")]
    public class ScriptedChange
    {
        public TfsChangeType ChangeType { get; set; }
        public TfsItemType ItemType { get; set; }
        public string RepositoryPath { get; set; }
        public byte[] Content { get; set; }
        public int? ItemId { get; set; }
    }

    [Serializable]
    public class BranchChangesetDatas
    {
        public int RootChangesetId { get; set; }
        public string BranchPath { get; set; }
        public string ParentBranch { get; set; }
    }

    [Serializable]
    public class MergeChangesetDatas
    {
        public int BeforeMergeChangesetId { get; set; }
        public string BranchPath { get; set; }
        public string MergeIntoBranch { get; set; }
    }

    [Serializable]
    public class ScriptedRootBranch
    {
        public string BranchPath { get; set; }
    }
}
