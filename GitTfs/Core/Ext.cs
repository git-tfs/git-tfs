using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public static partial class Ext
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

        public static IEnumerable<T> Append<T>(this IEnumerable<T> set1, params IEnumerable<T> [] moreSets)
        {
            foreach(var item in set1)
            {
                yield return item;
            }
            foreach(var set in moreSets)
            {
                foreach(var item in set)
                {
                    yield return item;
                }
            }
        }

        public static T FirstOr<T>(this IEnumerable<T> e, T defaultValue)
        {
            foreach (var x in e) return x;
            return defaultValue;
        }

        public static bool Empty<T>(this IEnumerable<T> e)
        {
            return !e.Any();
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

        public static bool IsEmpty<T>(this IEnumerable<T> c)
        {
            return c == null || !c.Any();
        }

        public static IEnumerable<IOptionResults> GetOptionParseHelpers(this GitTfsCommand command, IContainer container)
        {
            yield return new PropertyFieldParserHelper(container.GetInstance<Globals>());
            yield return new PropertyFieldParserHelper(command);
            if(command.ExtraOptions != null)
            {
                foreach(var parseHelper in command.ExtraOptions)
                {
                    yield return parseHelper;
                }
            }
        }

        private static readonly Regex sha1OnlyRegex = new Regex("^" + GitTfsConstants.Sha1 + "$");
        public static void AssertValidSha(this String sha)
        {
            if (!sha1OnlyRegex.IsMatch(sha))
                throw new Exception("Invalid sha1: " + sha);
        }

        public static string Read(this TextReader reader, int length)
        {
            var chars = new char[length];
            var charsRead = reader.Read(chars, 0, length);
            return new string(chars, 0, charsRead);
        }
    }
}
