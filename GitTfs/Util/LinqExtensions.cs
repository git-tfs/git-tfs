using System;
using System.Collections.Generic;

namespace Sep.Git.Tfs.Util
{
    /// <summary>
    /// Helper class for LINQ expressions.
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Performs a distinct selection from an IEnumerable based on a property in the collection. 
        /// </summary>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.DistinctBy(keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Performs a distinct selection from an IEnumerable based on a property in the collection. 
        /// </summary>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            var keys = new HashSet<TKey>(comparer);
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    yield return item;
                }
            }
        }
    }
}
