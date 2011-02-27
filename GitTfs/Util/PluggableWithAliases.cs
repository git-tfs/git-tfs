using StructureMap;

namespace Sep.Git.Tfs.Util
{
    public class PluggableWithAliases : PluggableAttribute
    {
        public readonly string[] Aliases;

        public PluggableWithAliases(string concreteKey, params string[] aliases)
            : base(concreteKey)
        {
            Aliases = aliases;
        }
    }
}
