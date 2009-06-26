using System.Collections.Generic;
using StructureMap;

namespace Sep.Git.Tfs
{
    [PluginFamily]
    public interface GitTfsCommand
    {
        int Run(IEnumerable<string> args);
    }
}
