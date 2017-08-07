/* Copyright 2017 ScaleOut Software, Inc.
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
using System.Text;

namespace Scaleout.Streaming.TimeWindowing
{
    /// <summary>
    /// Transforms a collection into an enumerable collection of fixed-time windows and manages 
    /// eviction of old items from the source collection. 
    /// This wrapper can be used to manage the retention policy of the underlying collection.
    /// Objects added through this wrapper are inserted in chronologial
    /// order and evicted according to the <c>startTime</c> policy provided to the constructor.
    /// </summary>
    /// <typeparam name="T">The type of objects in the source collection.</typeparam>
    public class TumblingWindowCollection<T> : IEnumerable<ITimeWindow<T>>
    {
        // Tumbling windows are just a special case of sliding windows where the duration of each
        // window is the same as the period between the start of each window. So this class is really 
        // just a nice alias--it just wraps a SlidingWindowCollection and lets it do the hard work. 
        SlidingWindowCollection<T> _transform;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that may be shortened for the last window 
        /// in the returned sequence.
        /// </param>
        /// <param name="startTime">Start time (inclusive) of the first window.
        /// Items in the underlying collection that fall before this start time will be evicted.</param>
        public TumblingWindowCollection(IList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, DateTime startTime)
        {
            _transform = new SlidingWindowCollection<T>(source, timestampSelector, windowDuration, windowDuration, startTime);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The linked list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that will be shortened for the last window 
        /// in the returned sequence.
        /// </param>
        /// <param name="startTime">Start time (inclusive) of the first window.
        /// Items in the underlying collection that fall before this start time will be evicted.</param>
        public TumblingWindowCollection(LinkedList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, DateTime startTime)
        {
            _transform = new SlidingWindowCollection<T>(source, timestampSelector, windowDuration, windowDuration, startTime);
        }

        /// <summary>
        /// Adds an element to the underlying collection, inserting it into the underlying source collection
        /// in chronological order. If the timestamp associated with the new element falls before the
        /// <c>startTime</c> provided to this transformation's constructor then the new element will be
        /// evicted immediately.
        /// </summary>
        /// <param name="item">The element to add to the collection.</param>
        public void Add(T item)
        {
            _transform.Add(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of time windows.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection of <see cref="ITimeWindow{TElement}"/> elements.</returns>
        public IEnumerator<ITimeWindow<T>> GetEnumerator()
        {
            return _transform.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
