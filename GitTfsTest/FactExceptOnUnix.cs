using System;
using Xunit;

namespace Sep.Git.Tfs.Test
{
    public class FactExceptOnUnixAttribute : FactAttribute
    {
        public override string Skip
        {
            get
            {
                if (IsUnix())
                    return "Skipped because run on Unix";
                return base.Skip;
            }

            set
            {
                base.Skip = value;
            }
        }

        private bool IsUnix()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;
        }
    }
}
