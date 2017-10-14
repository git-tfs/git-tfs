
namespace GitTfs.Core.TfsInterop
{
    public static class TfsExt
    {
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
