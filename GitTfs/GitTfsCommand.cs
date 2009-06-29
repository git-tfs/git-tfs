using System.Collections.Generic;
using StructureMap;

namespace Sep.Git.Tfs
{
    [PluginFamily]
    public interface GitTfsCommand
    {
        IEnumerable<ParseHelper> ExtraOptions { get; }
        int Run(IEnumerable<string> args);
    }
}
