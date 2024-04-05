using Xunit;

namespace GitTfs.Test
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

            set => base.Skip = value;
        }

        private bool IsUnix() => Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;
    }
}
