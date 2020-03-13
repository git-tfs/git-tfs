using System;

namespace GitTfs.Core.TfsInterop
{
    // Copy of http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.versioncontrol.client.changetype.aspx
    [Flags]
    public enum TfsChangeType
    {
        None        = 0x0001,
        Add         = 0x0002,
        Edit        = 0x0004,
        Encoding    = 0x0008,
        Rename      = 0x0010,
        Delete      = 0x0020,
        Undelete    = 0x0040, // Unused
        Content     = 0x207F, // Rollup of the preceding change types - and Property Changes

        Branch      = 0x0080,
        Merge       = 0x0100,

        Lock        = 0x0200,
        Rollback    = 0x0400,
        SourceRename= 0x0800,
        TargetRename= 0x1000, // Unused?
        Property    = 0x2000  // Adding to handle softlinks (ie MKLINK) and executable bit (for Team Explorer Everywhere)
    }
}
