using GitTfs.Commands;

using NDesk.Options;

using StructureMap;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GitTfs.Core
{
    using System.Collections.Concurrent;
    using System.Threading;

    public static class Ext
    {
        public static T Tap<T>(this T o, Action<T> block)
        {
            block(o);
            return o;
        }

        public static U Try<T, U>(this T o, Func<T, U> expr) => Try(o, expr, () => default(U));

        public static U Try<T, U>(this T o, Func<T, U> expr, Func<U> makeDefault)
        {
            if (o == null) return makeDefault();
            return expr(o);
        }

        public static Action<T> And<T>(this Action<T> originalAction, params Action<T>[] additionalActions) => x =>
                                                                                                                        {
                                                                                                                            originalAction(x);
                                                                                                                            foreach (var action in additionalActions)
                                                                                                                            {
                                                                                                                                action(x);
                                                                                                                            }
                                                                                                                        };

        public static T GetOrAdd<K, T>(this Dictionary<K, T> dictionary, K key) where T : new()
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, new T());
            return dictionary[key];
        }

        public static bool Empty<T>(this IEnumerable<T> e) => !e.Any();

        public static bool IncludesOneOf<T>(this T flagged, params T[] flags) where T : Enum
            => flags.Any(t => flagged.HasFlag(t));

        public static void SetArguments(this ProcessStartInfo startInfo, params string[] args) => startInfo.Arguments = string.Join(" ", args.Select(arg => QuoteProcessArgument(arg)).ToArray());

        private static string QuoteProcessArgument(string arg) => arg.Contains(" ") ? ("\"" + arg + "\"") : arg;

        public static string FormatForGit(this DateTime date) => date.ToUniversalTime().ToString("s") + "Z";

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> c) => c?.Any() != true;

        public static OptionSet GetAllOptions(this GitTfsCommand command, IContainer container) => container.GetInstance<Globals>().OptionSet.Merge(command.OptionSet);

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
        public static StreamWriter WithEncoding(this StreamWriter stream, Encoding encoding) => new StreamWriter(stream.BaseStream, encoding);

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

        /// <summary>
        /// Translate the <paramref name="source"/> to the sequence of array's items
        /// </summary>
        /// <typeparam name="TSource">The source item type</typeparam>
        /// <typeparam name="TResult">The output item type</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="selector">The delegate to use to translate</param>
        /// <param name="batchSize">the of item in the batch array</param>
        /// <returns>The <see cref="IEnumerable{T}"/> with arrays of items sized by <paramref name="batchSize"/></returns>
        public static IEnumerable<TResult[]> ToBatch<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, int batchSize)
        {
            var batch = new List<TResult>(batchSize);

            foreach (var item in source)
            {
                if (batch.Count >= batchSize)
                {
                    yield return batch.ToArray();
                    batch.Clear();
                }

                batch.Add(selector(item));
            }

            if (batch.Count > 0)
            {
                yield return batch.ToArray();
            }
        }

        /// <summary>
        /// Translate the <paramref name="source"/> to the sequence of array's items
        /// </summary>
        /// <typeparam name="T">The source item type</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="batchSize">the of item in the batch array</param>
        /// <returns>The <see cref="IEnumerable{T}"/> with arrays of items sized by <paramref name="batchSize"/></returns>
        [DebuggerStepThrough]
        public static IEnumerable<T[]> ToBatch<T>(this IEnumerable<T> source, int batchSize) => ToBatch(source, e => e, batchSize);

        /// <summary>
        /// Executes the <paramref name="action"/> for each item in the <paramref name="source"/> simultaneously.
        /// </summary>
        /// <param name="source">The source sequence.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="parallelizeActions">set to true to enable parallel processing</param>
        /// <typeparam name="T">The source item type</typeparam>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action, bool parallelizeActions)
        {
#if DEBUG && NO_PARALLEL
            parallelizeActions = false;
#endif
            if (!parallelizeActions)
            {
                foreach (var item in source)
                {
                    action(item);
                }
            }
            else
            {
                (source as ParallelQuery<T> ?? source.AsParallel()).ForAll(action);
            }
        }

        /// <summary>
        /// TRuns the <paramref name="action"/> on each item in the <paramref name="source"/> in parallel
        /// </summary>
        /// <param name="source">
        /// The source collection of items.
        /// </param>
        /// <param name="action">
        /// The action to process of the item.
        /// </param>
        /// <param name="retryInterval">
        /// The delay between retries.
        /// </param>
        /// <param name="parallelizeActions">set to true to enable parallel processing</param>
        /// <typeparam name="T">The type of items in the <paramref name="source"/>.</typeparam>
        /// <returns>
        /// Returns <b>true</b> when all items processed successfully.</returns>
        public static void ForEachRetry<T>(
            this IEnumerable<T> source,
            Action<T> action,
            TimeSpan retryInterval,
            bool parallelizeActions) => ForEachRetry(source, action, 10, retryInterval, parallelizeActions);

        /// <summary>
        /// TRuns the <paramref name="action"/> on each item in the <paramref name="source"/> in parallel
        /// </summary>
        /// <param name="source">
        /// The source collection of items.
        /// </param>
        /// <param name="action">
        /// The action to process of the item.
        /// </param>
        /// <param name="parallelizeActions">set to true to enable parallel processing</param>
        /// <typeparam name="T">The type of items in the <paramref name="source"/>.</typeparam>
        /// <returns>
        /// Returns <b>true</b> when all items processed successfully.</returns>
        public static void ForEachRetry<T>(this IEnumerable<T> source, Action<T> action, bool parallelizeActions) => ForEachRetry(source, action, 10, TimeSpan.FromSeconds(1), parallelizeActions);

        /// <summary>
        /// TRuns the <paramref name="action"/> on each item in the <paramref name="source"/> in parallel
        /// </summary>
        /// <param name="source">
        /// The source collection of items.
        /// </param>
        /// <param name="action">
        /// The action to process of the item.
        /// </param>
        /// <param name="retryCount">
        /// TThe number of retries.
        /// </param>
        /// <param name="parallelizeActions">set to true to enable parallel processing</param>
        /// <typeparam name="T">The type of items in the <paramref name="source"/>.</typeparam>
        /// <returns>
        /// Returns <b>true</b> when all items processed successfully.</returns>
        public static void ForEachRetry<T>(this IEnumerable<T> source, Action<T> action, int retryCount, bool parallelizeActions) => ForEachRetry(source, action, retryCount, TimeSpan.FromSeconds(1), parallelizeActions);

        /// <summary>
        /// TRuns the <paramref name="action"/> on each item in the <paramref name="source"/> in parallel
        /// </summary>
        /// <param name="source">
        /// The source collection of items.
        /// </param>
        /// <param name="action">
        /// The action to process of the item.
        /// </param>
        /// <param name="retryCount">
        /// TThe number of retries.
        /// </param>
        /// <param name="retryInterval">
        /// The delay between retries.
        /// </param>
        /// <param name="parallelizeActions">set to true to enable parallel processing</param>
        /// <typeparam name="T">The type of items in the <paramref name="source"/>.</typeparam>
        /// <returns>
        /// Returns <b>true</b> when all items processed successfully.</returns>
        public static void ForEachRetry<T>(this IEnumerable<T> source, Action<T> action, int retryCount, TimeSpan retryInterval, bool parallelizeActions)
        {
            var fails = new ConcurrentBag<Exception>();

            source.ForEach(
                item =>
                {
                    List<Exception> exceptions = null;

                    for (var i = 0; i < retryCount; i++)
                    {
                        if (i != 0)
                        {
                            Thread.Sleep(retryInterval);
                        }

                        try
                        {
                            action(item);
                            return;
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError("The action is failing: {0}", e.Message);

                            if (exceptions == null)
                            {
                                exceptions = new List<Exception>();
                            }

                            exceptions.Add(e);
                        }
                    }

                    if (exceptions != null)
                    {
                        fails.Add(new AggregateException(exceptions));
                    }
                }, parallelizeActions);

            if (fails.Count > 0)
            {
                throw new AggregateException(fails);
            }
        }
    }
}
