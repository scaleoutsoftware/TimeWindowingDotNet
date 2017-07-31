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
using System.Linq;
using Scaleout.Streaming.TimeWindowing.Linq;

namespace Scaleout.Streaming.TimeWindowing
{
    /// <summary>
    /// Transforms a collection into an enumerable collection of overlapping time windows and manages 
    /// eviction of old items from the source collection. 
    /// This wrapper can be used to manage the retention policy of the underlying collection.
    /// Objects added through this wrapper are inserted in chronologial
    /// order and evicted according to the <c>startTime</c> policy provided to the constructor.
    /// </summary>
    /// <typeparam name="T">The type of objects in the source collection.</typeparam>
    public class SlidingWindowCollection<T> : IEnumerable<ITimeWindow<T>>
    {
        IEnumerable<T> _source;

        private CollectionType _collType;
        private Func<T, DateTime> _timestampSelector;

        DateTime _startTime;
        TimeSpan _windowDuration;
        TimeSpan _every;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that will be shortened for the last window(s) 
        /// in the returned sequence (see remarks).
        /// </param>
        /// <param name="every">The period of time between the start of each sliding window.</param>
        /// <param name="startTime">Start time (inclusive) of the first sliding window.
        /// Items in the underlying collection that fall before this start time will be evicted.</param>
        public SlidingWindowCollection(IList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime)
        {
            _collType = CollectionType.List;
            Init(source, timestampSelector, windowDuration, every, startTime);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The linked list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that will be shortened for the last window(s) 
        /// in the returned sequence (see remarks).
        /// </param>
        /// <param name="every">The period of time between the start of each sliding window.</param>
        /// <param name="startTime">Start time (inclusive) of the first sliding window.
        /// Items in the underlying collection that fall before this start time will be evicted.</param>
        public SlidingWindowCollection(LinkedList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime)
        {
            _collType = CollectionType.LinkedList;
            Init(source, timestampSelector, windowDuration, every, startTime);
        }

        private void Init(IEnumerable<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _timestampSelector = timestampSelector ?? throw new ArgumentNullException(nameof(timestampSelector));
            if (every > windowDuration)
                throw new ArgumentException("Window duration must be larger than period.");

            _windowDuration = windowDuration;
            _every = every;
            _startTime = startTime;

            // We do eviction here because the sliding transform's eviction model is time-based (unlike the
            // the session window's eviction logic that's count-based). So we expect the start time provided
            // to the constructor to change each time the SlidingWindowCollection is constructed.
            PerformEviction();
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
            // Use add/evict algorithm tuned to underlying collection type.
            switch (_collType)
            {
                case CollectionType.List:
                    AddToList(item);
                    break;
                case CollectionType.LinkedList:
                    AddToLinkedList(item);
                    break;
                default:
                    throw new NotImplementedException("Underlying collection type not supported");
            }

        }

        private void AddToList(T item)
        {
            IList<T> source = _source as IList<T>;

            source.AddTimeOrdered(item, _timestampSelector);

            PerformEviction(source);
        }

        private void PerformEviction()
        {
            // Use add/evict algorithm tuned to underlying collection type.
            switch (_collType)
            {
                case CollectionType.List:
                    PerformEviction(_source as IList<T>);
                    break;
                case CollectionType.LinkedList:
                    PerformEviction(_source as LinkedList<T>);
                    break;
                default:
                    throw new NotImplementedException("Underlying collection type not supported");
            }
        }

        private void PerformEviction(IList<T> source)
        {
            // find index of first element to keep.
            int countOfItemsToRemove = 0;
            while (countOfItemsToRemove < source.Count)
            {
                if (_timestampSelector(source[countOfItemsToRemove]) < _startTime)
                    countOfItemsToRemove++;
                else
                    break;
            }

            if (countOfItemsToRemove > 0)
                source.RemoveFirstItems(countOfItemsToRemove);
        }

        private void AddToLinkedList(T item)
        {
            LinkedList<T> source = _source as LinkedList<T>;

            source.AddTimeOrdered(item, _timestampSelector);

            PerformEviction(source);
        }

        private void PerformEviction(LinkedList<T> source)
        {
            while (source.First != null)
            {
                if (_timestampSelector(source.First.Value) < _startTime)
                    source.RemoveFirst();
                else
                    break;
            }
        }

        public IEnumerator<ITimeWindow<T>> GetEnumerator()
        {
            if (_source.Count() == 0)
                return Enumerable.Empty<ITimeWindow<T>>().GetEnumerator();

            DateTime endTime = _timestampSelector(_source.Last()).AddTicks(1);
            return _source.ToSlidingWindows(_timestampSelector, _startTime, endTime, _windowDuration, _every).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
