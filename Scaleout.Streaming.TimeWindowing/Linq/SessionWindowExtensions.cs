﻿/* Copyright 2017-2018 ScaleOut Software, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scaleout.Streaming.TimeWindowing.Linq
{
    /// <summary>
    /// Provides extension method for session windowing.
    /// </summary>
    public static class SessionWindowExtensions
    {
        /// <summary>
        /// Transforms a collection into an enumerable collection of session windows. The source collection
        /// must be sorted chronologically.
        /// </summary>
        /// <typeparam name="TSource">The type of objects in the source collection.</typeparam>
        /// <param name="source">The sequence of elements to transform.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="idleThreshold">Maximum allowed time gap between elements before a new session window is started.</param>
        /// <returns>An enumerable set of <see cref="ITimeWindow{TElement}"/> collections.</returns>
        public static IEnumerable<ITimeWindow<TSource>> ToSessionWindows<TSource>(this IEnumerable<TSource> source, Func<TSource, DateTime> timestampSelector, TimeSpan idleThreshold)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (timestampSelector == null) throw new ArgumentNullException(nameof(timestampSelector));
            if (idleThreshold <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(idleThreshold));

            SessionWindow<TSource> currentWindow = null;

            foreach (var item in source)
            {
                var timestamp = timestampSelector(item);

                if (currentWindow == null)
                {
                    // we're processing the first item in source collection
                    currentWindow = new SessionWindow<TSource>(timestamp);
                    currentWindow.AddItem(item, timestamp);
                }
                else
                {
                    if (timestamp - currentWindow.EndTime > idleThreshold)
                    {
                        // idle threshold exceeded; close out prior window and create new one.
                        yield return currentWindow;
                        currentWindow = new SessionWindow<TSource>(timestamp);
                    }
                    currentWindow.AddItem(item, timestamp);
                }
            }

            // close out the last session window:
            if (currentWindow != null)
            {
                yield return currentWindow;
            }
            else
            {
                // source collection was empty
                yield break;
            }
        }
    }

    internal class SessionWindow<TElement> : ITimeWindow<TElement>
    {
        List<TElement> _items = new List<TElement>();

        public SessionWindow(DateTime startTime)
        {
            StartTime = startTime;
        }

        public void AddItem(TElement item, DateTime timestamp)
        {
            if (_items == null)
            {
                _items = new List<TElement>(1);
                StartTime = timestamp;
            }

            _items.Add(item);
            EndTime = timestamp;
        }

        public DateTime EndTime { get; private set; }

        public DateTime StartTime { get; private set; }

        public int Count { get { return _items.Count; } }

        public bool IsReadOnly { get { return true; } }

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
            return _items.Contains(item);
        }

        public void CopyTo(TElement[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public bool Remove(TElement item)
        {
            throw new NotSupportedException("The collection is read-only");
        }
    }
}
