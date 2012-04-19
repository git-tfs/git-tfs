using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsFake
{
    public class Script
    {
        public const string EnvVar = "GIT_TFS_VSFAKE_SCRIPT";

        public static Script Load(string path)
        {
            return new Script().Tap(script => Load(path, script));
        }

        private static void Load(string path, Script script)
        {
            var formatter = new BinaryFormatter();
            using (var stream = File.OpenRead(path))
                script.Changesets.AddRange((List<ScriptedChangeset>)formatter.Deserialize(stream));
        }

        public void Save(string path)
        {
            var formatter = new BinaryFormatter();
            using (var stream = File.Create(path))
                formatter.Serialize(stream, Changesets);
        }

        List<ScriptedChangeset> _changesets = new List<ScriptedChangeset>();
        public List<ScriptedChangeset> Changesets { get { return _changesets; } }
    }

    [Serializable]
    public class ScriptedChangeset
    {
        public int Id { get; set; }
        public string Comment { get; set; }
        public DateTime CheckinDate { get; set; }
        public List<ScriptedChange> Changes { get { return _changes; } }

        List<ScriptedChange> _changes = new List<ScriptedChange>();
    }

    [Serializable]
    public class ScriptedChange
    {
        public TfsChangeType ChangeType { get; set; }
        public TfsItemType ItemType { get; set; }
        public string RepositoryPath { get; set; }
        public string Content { get; set; }
    }
}
