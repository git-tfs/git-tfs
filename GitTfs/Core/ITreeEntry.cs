using System.IO;

namespace Sep.Git.Tfs.Core
{
    public interface ITreeEntry
    {
        string FullName { get; }
        Stream OpenRead();
    }
}