using System;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    [Flags]
    public enum TfsChangeType
    {
        Add = 2,
        Branch = 0x80,
        Delete = 0x20,
        Edit = 4,
        Encoding = 8,
        Lock = 0x200,
        Merge = 0x100,
        None = 1,
        Rename = 0x10,
        Undelete = 0x40
    }
}
