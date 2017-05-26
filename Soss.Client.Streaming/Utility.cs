using System;
using System.Collections.Generic;
using System.Text;

namespace Soss.Client.Streaming
{
    internal static class Utility
    {
        /// <summary>
        /// Perform in-place removal of the first N elements in a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="count"></param>
        public static void RemoveFirstItems<T>(this IList<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count < 0 || count > source.Count) throw new ArgumentOutOfRangeException(nameof(count));

            // shift elements down
            for (int from = count, to = 0; from < source.Count; from++, to++)
                source[to] = source[from];

            // remove trailing elements (moving backwards to minimize shifting)
            for (int i = source.Count - 1; i >= (source.Count - count); i--)
                source.RemoveAt(i);
        }
    }
}
