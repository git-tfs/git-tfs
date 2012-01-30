using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.Options;

namespace Sep.Git.Tfs.Commands
{
    public static class Helpers
    {
        public static OptionSet Merge(this OptionSet options, params OptionSet[] others)
        {
            var merged = new MergableOptionSet();
            merged.Merge(options);
            foreach(var other in others)
                merged.Merge(other);
            return merged;
        }

        class MergableOptionSet : OptionSet
        {
            public void Merge(OptionSet other)
            {
                foreach(var option in other)
                {
                    if(!Contains(GetKeyForItem(option)))
                    {
                        Add(option);
                    }
                }
            }
        }

        [Obsolete("... migrating to NDesk.Options ...")]
        public static IEnumerable<CommandLine.OptParse.IOptionResults> MakeNestedOptionResults(this GitTfsCommand command, params object[] optionsObjectsOrCommands)
        {
            foreach(var obj in optionsObjectsOrCommands)
            {
                if(obj is GitTfsCommand)
                {
                    foreach(var option in ((GitTfsCommand)obj).ExtraOptions)
                    {
                        yield return option;
                    }
                    yield return new CommandLine.OptParse.PropertyFieldParserHelper(obj);
                }
                else if(obj is CommandLine.OptParse.IOptionResults)
                {
                    yield return (CommandLine.OptParse.IOptionResults) obj;
                }
                else
                {
                    yield return new CommandLine.OptParse.PropertyFieldParserHelper(obj);
                }
            }
        }
    }
}
