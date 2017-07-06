using System;
using System.Collections.Generic;
using System.Text;

namespace Scaleout.Client.Streaming
{
    internal enum CollectionType : byte { List, LinkedList }

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

        internal static void AddTimeOrdered<T>(this IList<T> source, T item, Func<T, DateTime> timestampSelector)
        {
            DateTime timestamp = timestampSelector(item);
            int insertPosition = source.Count;
            while (insertPosition > 0)
            {
                if (timestamp < timestampSelector(source[insertPosition - 1]))
                    insertPosition--;
                else
                    break;
            }

            source.Insert(insertPosition, item);
        }

        internal static void AddTimeOrdered<T>(this LinkedList<T> source, T item, Func<T, DateTime> timestampSelector)
        {
            DateTime timestamp = timestampSelector(item);
            var nodeToInsertAfter = source.Last;

            while (nodeToInsertAfter != null)
            {
                if (timestamp < timestampSelector(nodeToInsertAfter.Value))
                    nodeToInsertAfter = nodeToInsertAfter.Previous;
                else
                    break;
            }

            // Found the place, now do the insert
            if (nodeToInsertAfter != null)
                source.AddAfter(nodeToInsertAfter, item);
            else
            {
                // adding as first item (or else adding to an empty linked list)
                source.AddFirst(item);
            }
        }
    }
}
