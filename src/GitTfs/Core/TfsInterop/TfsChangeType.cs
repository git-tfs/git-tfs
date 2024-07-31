namespace GitTfs.Core.TfsInterop
{
    // Copy of http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.versioncontrol.client.changetype.aspx
    [Flags]
    public enum TfsChangeType
    {
        None = 0x0001,
        Add = 0x0002,
        Edit = 0x0004,
        Encoding = 0x0008,
        Rename = 0x0010,
        Delete = 0x0020,
        Undelete = 0x0040,
        Content = 0x007F, // Rollup of the preceding change types

        Branch = 0x0080,
        Merge = 0x0100,

        Lock = 0x0200,

        Rollback = 0x0400,
        Property = 0x2000,
        SourceRename = 0x0800,
    }
}
