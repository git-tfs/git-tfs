using StructureMap;

namespace GitTfs.Util
{
    [StructureMapSingleton]
    public class GitTfsCommandFactory
    {
        private readonly IContainer _container;

        public GitTfsCommandFactory(IContainer container)
        {
            _container = container;
        }

        private Dictionary<string, string> _aliasMap;
        public Dictionary<string, string> AliasMap => _aliasMap ?? (_aliasMap = CreateAliasMap());

        private Dictionary<string, string> CreateAliasMap()
        {
            var aliasMap = new Dictionary<string, string>();
            var commandPluginType = _container.Model.PluginTypes.First(p => p.PluginType == typeof(GitTfsCommand));

            foreach (var instance in commandPluginType.Instances)
            {
                var attribte = instance.ConcreteType.GetCustomAttributes(typeof(PluggableWithAliases), true)
                    .Cast<PluggableWithAliases>().FirstOrDefault();

                if (attribte != null)
                {
                    foreach (var alias in attribte.Aliases)
                    {
                        aliasMap[alias] = instance.Name;
                    }
                }
            }

            return aliasMap;
        }

        public GitTfsCommand GetCommand(string name) => _container.TryGetInstance<GitTfsCommand>(GetCommandName(name));

        private string GetCommandName(string name)
        {
            string commandName;
            return AliasMap.TryGetValue(name, out commandName) ? commandName : name;
        }

        public IEnumerable<string> GetAliasesForCommandName(string name) => AliasMap.Where(p => p.Value == name).Select(p => p.Key);
    }
}
