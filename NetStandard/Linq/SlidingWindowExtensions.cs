﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Scaleout.Client.Streaming.Linq
{
    public static class SlidingWindowExtensions
    {
        /// <summary>
        /// Transforms a collection into an enumerable collection of sliding windows. The source collection
        /// must be sorted chronologically.
        /// </summary>
        /// <typeparam name="TSource">The type of objects in the source collection.</typeparam>
        /// <param name="source">The sequence of elements to transform.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="startTime">Start time (inclusive) of the first sliding window.</param>
        /// <param name="endTime">End time (exclusive) for the last of sliding window(s).</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that will be shortened for the last window(s) 
        /// in the returned sequence (see remarks).
        /// </param>
        /// <param name="every">The period of time between the start of each sliding window.</param>
        /// <returns>An enumerable set of <see cref="ITimeWindow{TElement}"/> collections.</returns>
        /// <remarks>
        /// <para>
        /// The method returns a sequence of overlapping <see cref="ITimeWindow{TElement}"/> elements, where each window is 
        /// an enumerable collection of elements from the source collection whose timestamp falls within its specified range. 
        /// The <see cref="ITimeWindow{TElement}.StartTime"/> is inclusive and the <see cref="ITimeWindow{TElement}.EndTime"/> is exclusive.
        /// </para>
        /// <para>
        /// The last sliding windows returned by this method may have a shorter duration than what is specified
        /// by the <paramref name="windowDuration"/> argument. This happens when the duration would cause 
        /// the window to extend beyond the specified <paramref name="endTime"/>. These trailing windows can be skipped
        /// by using a Linq Where() operation on the returned sequence to filters out windows with short durations.
        /// </para>
        /// </remarks>
        public static IEnumerable<ITimeWindow<TSource>> ToSlidingWindows<TSource>(this IEnumerable<TSource> source, Func<TSource, DateTime> timestampSelector, DateTime startTime, DateTime endTime, TimeSpan windowDuration, TimeSpan every)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (timestampSelector == null) throw new ArgumentNullException(nameof(timestampSelector));

            var intervalGen = new SlidingWindowIntervalGenerator<TSource>(startTime, endTime, every, windowDuration);

            // location in the source collection where the next window should start looking for elements.
            int startingIndexHint = 0;

            switch (source)
            {
                case IList<TSource> list:
                    foreach (var window in intervalGen)
                    {
                        startingIndexHint = window.SetItems(list, startingIndexHint, timestampSelector);
                        yield return window;
                    }
                    break;
                case LinkedList<TSource> linkedList:
                    var startHintNode = linkedList.First;
                    foreach (var window in intervalGen)
                    {
                        startHintNode = window.SetItems(startHintNode, timestampSelector);
                        yield return window;
                    }
                    break;
                default:
                    // any old IEnumerable.
                    foreach (var window in intervalGen)
                    {
                        startingIndexHint = window.SetItems(source, startingIndexHint, timestampSelector);
                        yield return window;
                    }
                    break;
            }

        }
    }

    internal class SlidingWindowIntervalGenerator<TElement> : IEnumerable<TimeWindow<TElement>>
    {
        private DateTime _startTime;
        private DateTime _endTime;
        private TimeSpan _period;
        private TimeSpan _duration;
        public SlidingWindowIntervalGenerator(DateTime start, DateTime end, TimeSpan period, TimeSpan duration)
        {
            if (start > end) throw new ArgumentException($"{nameof(start)} cannot be greater than {nameof(end)}");
            if (period > duration) throw new ArgumentException($"{nameof(period)} cannot be longer than interval's {nameof(duration)}");

            _startTime = start;
            _endTime = end;
            _period = period;
            _duration = duration;
        }

        public IEnumerator<TimeWindow<TElement>> GetEnumerator()
        {
            var start = _startTime;
            var end = _endTime;
            while (start < end)
            {
                var dur = _duration;
                if ((start + dur) > end)
                    dur = end - start;

                yield return new TimeWindow<TElement>(start, start + dur);
                start = start + _period;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    internal class TimeWindow<TElement> : ITimeWindow<TElement>
    {
        private List<TElement> _items;

        public TimeWindow(DateTime startTime, DateTime endTime)
        {
            EndTime = endTime;
            StartTime = startTime;
        }

        public DateTime EndTime { get; }

        public DateTime StartTime { get; }

        public int Count
        {
            get {
                if (_items != null)
                    return _items.Count;
                else
                    return 0;
            }
        }

        public bool IsReadOnly { get { return true; } }

        public int SetItems(IList<TElement> list, int startingIndexHint, Func<TElement, DateTime> timestampSelector)
        {
            bool foundItem = false;
            for (int i = startingIndexHint; i < list.Count; ++i)
            {
                var timestamp = timestampSelector(list[i]);
                if (timestamp < StartTime)
                    continue; // keep looking

                if (timestamp >= EndTime)
                    break; // we assume the list is sorted, so we won't find anything else

                if (!foundItem)
                {
                    // about to add the first item to this window.
                    foundItem = true;
                    startingIndexHint = i;
                    _items = new List<TElement>(1);
                }

                _items.Add(list[i]);
            }

            return startingIndexHint;
        }

        public LinkedListNode<TElement> SetItems(LinkedListNode<TElement> startingNode, Func<TElement, DateTime> timestampSelector)
        {
            bool foundItem = false;

            var currentNode = startingNode;
            while (currentNode != null)
            {
                var timestamp = timestampSelector(currentNode.Value);
                if (timestamp < StartTime)
                {
                    startingNode = currentNode.Next;
                }
                else if (timestamp >= EndTime)
                {
                    break; // we assume the list is sorted, so we won't find anything else
                }
                else
                {
                    // node falls within the window time
                    if (!foundItem)
                    {
                        foundItem = true;
                        _items = new List<TElement>(1);
                    }
                    _items.Add(currentNode.Value);
                }

                currentNode = currentNode.Next;
            }
            return startingNode;
        }

        public int SetItems(IEnumerable<TElement> source, int startingIndexHint, Func<TElement, DateTime> timestampSelector)
        {
            bool foundItem = false;
            foreach (var item in source)
            {
                var timestamp = timestampSelector(item);
                if (timestamp < StartTime)
                {
                    startingIndexHint++;
                    continue; // keep looking
                }

                if (timestamp >= EndTime)
                    break; // we assume the list is sorted, so we won't find anything else

                if (!foundItem)
                {
                    foundItem = true;
                    _items = new List<TElement>(1);
                }
                _items.Add(item);
            }

            return startingIndexHint;
        }


        public IEnumerator<TElement> GetEnumerator()
        {
            if (_items == null)
                return Enumerable.Empty<TElement>().GetEnumerator();
            else
                return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TElement item)
        {
            throw new NotSupportedException("The collection is read-only");
        }

        public void Clear()
        {
            throw new NotSupportedException("The collection is read-only");
        }

        public bool Contains(TElement item)
        {
            if (_items == null)
                return false;
            else
                return _items.Contains(item);
        }

        public void CopyTo(TElement[] array, int arrayIndex)
        {
            if (_items != null)
                _items.CopyTo(array, arrayIndex);
        }

        public bool Remove(TElement item)
        {
            throw new NotSupportedException("The collection is read-only");
        }
    }
}