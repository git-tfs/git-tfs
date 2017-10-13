using NDesk.Options;

namespace Sep.Git.Tfs.Commands
{
    public static class Helpers
    {
        public static OptionSet Merge(this OptionSet options, params OptionSet[] others)
        {
            var merged = new MergableOptionSet();
            merged.Merge(options);
            foreach (var other in others)
                merged.Merge(other);
            return merged;
        }

        private class MergableOptionSet : OptionSet
        {
            public void Merge(OptionSet other)
            {
                foreach (var option in other)
                {
                    if (!Contains(GetKeyForItem(option)))
                    {
                        Add(option);
                    }
                }
            }
        }
    }
}
