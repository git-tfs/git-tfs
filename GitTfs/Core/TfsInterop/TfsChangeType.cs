using System;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    [Flags]
    public enum TfsChangeType
    {
        None = 1,
        Add = 2,
        Edit = 4,
        Encoding = 8,
        Rename = 0x10,
        Delete = 0x20,
        Undelete = 0x40,

        Branch = 0x80,
        Merge = 0x100,

        Lock = 0x200
    }
}
