using System.Collections.Generic;
using System.Linq;
using StructureMap;

namespace Sep.Git.Tfs.Util
{
    [StructureMapSingleton]
    public class GitTfsCommandFactory
    {
        private readonly IContainer container;

        public GitTfsCommandFactory(IContainer container)
        {
            this.container = container;
        }

        private Dictionary<string, string> _aliasMap;
        public Dictionary<string, string> AliasMap
        {
            get { return _aliasMap ?? (_aliasMap = CreateAliasMap()); }
        }

        private Dictionary<string, string> CreateAliasMap()
        {
            var aliasMap = new Dictionary<string, string>();
            var commandPluginType = container.Model.PluginTypes.First(p => p.PluginType == typeof (GitTfsCommand));

            foreach (var instance in commandPluginType.Instances)
            {
                var attribte = instance.ConcreteType.GetCustomAttributes(typeof (PluggableWithAliases), true)
                    .Cast<PluggableWithAliases>().FirstOrDefault();

                if(attribte != null)
                {
                    foreach (var alias in attribte.Aliases)
                    {
                        aliasMap[alias] = instance.Name;
                    }
                }
            }

            return aliasMap;
        }

        public GitTfsCommand GetCommand(string name)
        {
            return container.TryGetInstance<GitTfsCommand>(GetCommandName(name));
        }

        private string GetCommandName(string name)
        {
            string commandName;
            return AliasMap.TryGetValue(name, out commandName) ? commandName : name;
        }

        public IEnumerable<string> GetAliasesForCommandName(string name)
        {
            return AliasMap.Where(p => p.Value == name).Select(p => p.Key);
        }
    }
}
