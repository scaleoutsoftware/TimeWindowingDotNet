using System;
using System.Collections.Generic;
using System.Text;

namespace Soss.Client.Streaming
{
    public static class Utility
    {
        /// <summary>
        /// Perform in-place removal of the first N elements in a list.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">List of values.</param>
        /// <param name="count">Number of elements to remove from the front of the list.</param>
        public static void RemoveFirstItems<T>(this IList<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count < 0 || count > source.Count) throw new ArgumentOutOfRangeException(nameof(count));

            // shift elements down
            for (int from = count, to = 0; from < source.Count; from++, to++)
                source[to] = source[from];

            // remove trailing elements (moving backwards to minimize shifting)
            int newLastElement = source.Count - count;
            for (int i = source.Count - 1; i >= newLastElement; i--)
                source.RemoveAt(i);
        }


        /// <summary>
        /// Remove the first N elements in a linked list.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">List of values.</param>
        /// <param name="count">Number of elements to remove from the front of the list.</param>
        public static void RemoveFirstItems<T>(this LinkedList<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count < 0 || count > source.Count) throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
                source.RemoveFirst();
        }
    }
}
