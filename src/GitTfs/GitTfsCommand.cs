using NDesk.Options;
using StructureMap;

namespace Sep.Git.Tfs
{
    [PluginFamily]
    public interface GitTfsCommand
    {
        OptionSet OptionSet { get; }
    }
}
