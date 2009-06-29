using System.Collections.Generic;
using System.Linq;
using CommandLine.OptParse;

namespace Sep.Git.Tfs
{
    public static class Helpers
    {
        public static IEnumerable<IOptionResults> MakeOptionResults(params object [] optionsObjects)
        {
            return
                optionsObjects.Select(
                    option => option is IOptionResults ? (IOptionResults)option : new PropertyFieldParserHelper(option));
        }
    }
}
