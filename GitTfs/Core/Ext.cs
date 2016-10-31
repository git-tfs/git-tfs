using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NDesk.Options;
using Sep.Git.Tfs.Commands;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public static class Ext
    {
        public static T Tap<T>(this T o, Action<T> block)
        {
            block(o);
            return o;
        }

        public static U Try<T, U>(this T o, Func<T, U> expr)
        {
            return Try(o, expr, () => default(U));
        }

        public static U Try<T, U>(this T o, Func<T, U> expr, Func<U> makeDefault)
        {
            if (o == null) return makeDefault();
            return expr(o);
        }

        public static Action<T> And<T>(this Action<T> originalAction, params Action<T>[] additionalActions)
        {
            return x =>
            {
                originalAction(x);
                foreach (var action in additionalActions)
                {
                    action(x);
                }
            };
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> set1, params IEnumerable<T>[] moreSets)
        {
            foreach (var item in set1)
            {
                yield return item;
            }
            foreach (var set in moreSets)
            {
                foreach (var item in set)
                {
                    yield return item;
                }
            }
        }

        public static T GetOrAdd<K, T>(this Dictionary<K, T> dictionary, K key) where T : new()
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, new T());
            return dictionary[key];
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

        public static void SetArguments(this ProcessStartInfo startInfo, params string[] args)
        {
            startInfo.Arguments = string.Join(" ", args.Select(arg => QuoteProcessArgument(arg)).ToArray());
        }

        private static string QuoteProcessArgument(string arg)
        {
            return arg.Contains(" ") ? ("\"" + arg + "\"") : arg;
        }

        public static string CombinePaths(string basePath, params string[] pathParts)
        {
            foreach (var part in pathParts)
            {
                basePath = Path.Combine(basePath, part);
            }
            return basePath;
        }

        public static string FormatForGit(this DateTime date)
        {
            return date.ToUniversalTime().ToString("s") + "Z";
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
        /// The encoding used by a stream is a read-only property. Use this method to
        /// create a new stream based on <paramref name="stream"/> that uses
        /// the given <paramref name="encoding"/> instead.
        /// </summary>
        public static StreamWriter WithEncoding(this StreamWriter stream, Encoding encoding)
        {
            return new StreamWriter(stream.BaseStream, encoding);
        }

        public static bool Contains(this IEnumerable<string> list, string toCheck, StringComparison comp)
        {
            return list.Any(listMember => listMember.IndexOf(toCheck, comp) >= 0);
        }

        /// <summary>
        /// Optionally handle exceptions with "this" action. If there isn't a handler, don't catch exceptions.
        /// </summary>
        public static void Catch<TException>(this Action<TException> handler, Action work) where TException : Exception
        {
            if (handler == null)
            {
                work();
            }
            else
            {
                try
                {
                    work();
                }
                catch (TException e)
                {
                    handler(e);
                }
            }
        }
    }
}
