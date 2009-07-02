namespace Sep.Git.Tfs.Core
{
    public static class Ext
    {
        public static Action<T> And(this Action<T> originalAction, params Action<T> [] additionalActions)
        {
            return x => {
                originalAction(x);
                foreach(var action in additionalActions)
                {
                    action(x);
                }
            };
        }
    }
}
