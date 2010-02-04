using System.Collections.Generic;
using System.Linq;
using CommandLine.OptParse;

namespace Sep.Git.Tfs.Commands
{
    public static class Helpers
    {
        public static IEnumerable<IOptionResults> MakeOptionResults(this GitTfsCommand command, params object[] optionsObjects)
        {
            return
                optionsObjects.Select(
                    option => option is IOptionResults ? (IOptionResults)option : new PropertyFieldParserHelper(option));
        }

        public static IEnumerable<IOptionResults> MakeNestedOptionResults(this GitTfsCommand command, params object[] optionsObjectsOrCommands)
        {
            foreach(var obj in optionsObjectsOrCommands)
            {
                if(obj is GitTfsCommand)
                {
                    foreach(var option in ((GitTfsCommand)obj).ExtraOptions)
                    {
                        yield return option;
                    }
                    yield return new PropertyFieldParserHelper(obj);
                }
                else if(obj is IOptionResults)
                {
                    yield return (IOptionResults) obj;
                }
                else
                {
                    yield return new PropertyFieldParserHelper(obj);
                }
            }
        }
    }
}
