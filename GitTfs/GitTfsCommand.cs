using System.Collections.Generic;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs
{
    [PluginFamily]
    public interface GitTfsCommand
    {
        IEnumerable<IOptionResults> ExtraOptions { get; }
        int Run(IList<string> args);
    }
}
