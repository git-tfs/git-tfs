namespace GitTfs.Core.TfsInterop
{
    [Flags]
    public enum TfsCheckinEvaluationOptions
    {
        All = -1,
        Policies = 1,
        Conflicts = 2,
        Notes = 4,
    }
}
