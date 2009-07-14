using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Microsoft.TeamFoundation.VersionControl.Client;
using StructureMap;

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

        public static string CombinePaths(string basePath, params string [] pathParts)
        {
            foreach(var part in pathParts)
            {
                basePath = Path.Combine(basePath, part);
            }
            return basePath;
        }

        public static string FormatForGit(this DateTime date)
        {
            return date.ToUniversalTime().ToString("s") + "Z";
        }

        public static bool IncludesOneOf(this ChangeType changeType, params ChangeType[] typesToMatch)
        {
            foreach(var ok in typesToMatch)
            {
                if((ok & changeType) == ok)
                    return true;
            }
            return false;
        }

        public static void CopyTo(this Stream source, Stream destination)
        {
            const int blockSize = 4*1024;
            byte[] buffer = new byte[blockSize];
            int n;
            while(0 != (n = source.Read(buffer, 0, blockSize)))
            {
                destination.Write(buffer, 0, n);
            }
        }

        public static bool IsEmpty(this ICollection c)
        {
            return c == null || c.Count == 0;
        }

        public static IEnumerable<IOptionResults> GetOptionParseHelpers(this GitTfsCommand command)
        {
            yield return new PropertyFieldParserHelper(ObjectFactory.GetInstance<Globals>());
            yield return new PropertyFieldParserHelper(command);
            if(command.ExtraOptions != null)
            {
                foreach(var parseHelper in command.ExtraOptions)
                {
                    yield return parseHelper;
                }
            }
        }
    }
}
