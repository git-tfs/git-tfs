using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommandLine.OptParse;

namespace Sep.Git.Tfs.Core
{
    public static class Ext
    {
        public static Action<T> And<T>(this Action<T> originalAction, params Action<T> [] additionalActions)
        {
            return x => {
                originalAction(x);
                foreach(var action in additionalActions)
                {
                    action(x);
                }
            };
        }

        public static void SetArguments(this ProcessStartInfo startInfo, params string [] args)
        {
            startInfo.Arguments = String.Join(" ", args.Select(arg => QuoteProcessArgument(arg)).ToArray());
        }

        private static string QuoteProcessArgument(string arg)
        {
            return arg.Contains(" ") ? ("\"" + arg + "\"") : arg;
        }

        public static string CombinePaths(params string [] pathParts)
        {
            if(pathParts == null || pathParts.Count == 0) return null;
            var path = pathParts[0];
            for(int i = 1; i < pathParts.Count; i++)
            {
                path = Path.Combine(path, pathParts[i]);
            }
            return path;
        }
    }
}
