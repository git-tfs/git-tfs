using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
