using System.Collections.Generic;
using CommandLine.OptParse;
using NDesk.Options;
using StructureMap;

namespace Sep.Git.Tfs
{
    [PluginFamily]
    public interface GitTfsCommand
    {
        OptionSet OptionSet { get; }
        [System.Obsolete("Use OptionSet instead")]
        IEnumerable<IOptionResults> ExtraOptions { get; }
    }
}
