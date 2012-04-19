using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{
    public static partial class Ext
    {
        public static T Tap<T>(this T o, Action<T> block)
        {
            block(o);
            return o;
        }

        public static U AndAnd<T, U>(this T o, Func<T, U> expr)
        {
            if (o == null) return default(U);
            return expr(o);
        }

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

        public static OptionSet GetAllOptions(this GitTfsCommand command, IContainer container)
        {
            return container.GetInstance<Globals>().OptionSet.Merge(command.OptionSet);
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

        /// <summary>
        /// Force the <paramref name="stream"/> to use current Windows ANSI code page.
        /// For the StdIn-interactions with git it is important to use the standard ANSI codepage 
        /// (i.e. the codepage returned by  GetACP()), and not the current terminal codepage.
        /// </summary>
        public static StreamWriter WithDefaultEncoding(this StreamWriter stream) {
            return new StreamWriter(stream.BaseStream, Encoding.Default);
        }
    }
}
