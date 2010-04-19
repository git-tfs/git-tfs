using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public static class TfsExt
    {
        public static bool IncludesOneOf(this ChangeType changeType, params ChangeType[] typesToMatch)
        {
            foreach(var ok in typesToMatch)
            {
                if((ok & changeType) == ok)
                    return true;
            }
            return false;
        }

        public static bool IncludesOneOf(this TfsChangeType changeType, params TfsChangeType[] typesToMatch)
        {
            foreach (var ok in typesToMatch)
            {
                if ((ok & changeType) == ok)
                    return true;
            }
            return false;
        }
    }
}
