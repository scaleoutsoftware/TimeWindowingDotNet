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
using Scaleout.Client.Streaming.Linq;

namespace Scaleout.Client.Streaming
{


    /// <summary>
    /// Transforms a collection into an enumerable collection of session windows and manages 
    /// eviction of old items from the source collection. 
    /// This wrapper can be used to manage the retention policy of the underlying
    /// collection. Objects added through this wrapper are inserted in chronologial
    /// order and evicted according to the policy provided to the constructor.
    /// </summary>
    /// <typeparam name="T">The type of objects in the source collection.</typeparam>
    public class SessionWindowTransform<T> : IEnumerable<ITimeWindow<T>>
    {
        IEnumerable<T> _source;
        List<ITimeWindow<T>> _windows;

        private CollectionType _collType;
        private Func<T, DateTime> _timestampSelector;
        private TimeSpan _idleThreshold;
        private int _boundedSessionCapacity;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="idleThreshold">Maximum allowed time gap between elements before a new session window is started.</param>
        /// <param name="boundedSessionCapacity">
        /// Maximum number of sessions to retain. If a new element is added via the <see cref="Add(T)"/>
        /// method that creates a new session, elements in the oldest session will be evicted from the underlying collection.
        /// </param>
        public SessionWindowTransform(IList<T> source, Func<T, DateTime> timestampSelector, TimeSpan idleThreshold, int boundedSessionCapacity)
        {
            _collType = CollectionType.List;
            Init(source, timestampSelector, idleThreshold, boundedSessionCapacity);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The linked list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="idleThreshold">Maximum allowed time gap between elements before a new session window is started.</param>
        /// <param name="boundedSessionCapacity">
        /// Maximum number of sessions to retain. If a new element is added via the <see cref="Add(T)"/>
        /// method that creates a new session, elements in the oldest session will be evicted from the underlying collection.
        /// </param>
        public SessionWindowTransform(LinkedList<T> source, Func<T, DateTime> timestampSelector, TimeSpan idleThreshold, int boundedSessionCapacity)
        {
            _collType = CollectionType.LinkedList;
            Init(source, timestampSelector, idleThreshold, boundedSessionCapacity);
        }

        private void Init(IEnumerable<T> source, Func<T, DateTime> timestampSelector, TimeSpan idleThreshold, int boundedSessionCapacity)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _timestampSelector = timestampSelector ?? throw new ArgumentNullException(nameof(timestampSelector));
            _idleThreshold = (idleThreshold > TimeSpan.Zero) ? idleThreshold : throw new ArgumentOutOfRangeException(nameof(idleThreshold));
            _boundedSessionCapacity = (boundedSessionCapacity > 0) ? boundedSessionCapacity : throw new ArgumentOutOfRangeException(nameof(boundedSessionCapacity));

            // TODO: Perform eviction like we do with the SlidingTransform? I guess the caller could just use Refresh() if the 
            //       collection is changing underneath him somehow.
        }

        /// <summary>
        /// Adds an element to the underlying collection, inserting it in chronological order
        /// and evicting old sessions if needed.
        /// </summary>
        /// <param name="item">The element to add to the collection.</param>
        public void Add(T item)
        {
            // changing underlying collection, cache of session windows is now invalid.
            _windows = null;

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

            // The new element might cause a new session window to be created.
            PerformEviction(source);
        }

        private void PerformEviction(IList<T> source)
        {
            if (_windows == null)
                _windows = source.ToSessionWindows(_timestampSelector, _idleThreshold).ToList();

            int countOfWindowsToRemove = _windows.Count - _boundedSessionCapacity;

            if (countOfWindowsToRemove > 0)
            {
                int removeCount = _windows.Take(countOfWindowsToRemove).Sum(window => window.Count);
                source.RemoveFirstItems(removeCount);

                // We've changed the underlying collection. Let our cache of windows be regenerated on next access.
                _windows = null;
            }
        }

        private void AddToLinkedList(T item)
        {
            LinkedList<T> source = _source as LinkedList<T>;

            source.AddTimeOrdered(item, _timestampSelector);

            // The new element might cause a new session window to be created.
            PerformEviction(source);
        }

        private void PerformEviction(LinkedList<T> source)
        {
            if (_windows == null)
                _windows = source.ToSessionWindows(_timestampSelector, _idleThreshold).ToList();

            int countOfWindowsToRemove = _windows.Count - _boundedSessionCapacity;

            if (countOfWindowsToRemove > 0)
            {
                int removeCount = _windows.Take(countOfWindowsToRemove).Sum(window => window.Count);
                source.RemoveFirstItems(removeCount);

                // We've changed the underlying collection. Let our cache of windows be regenerated on next access.
                _windows = null;
            }
        }

        /// <summary>
        /// Gets a reference to the transformation's underlying collection.
        /// </summary>
        public IEnumerable<T> Source { get { return _source; } }

        /// <summary>
        /// Refreshes the transformation. For use when the underlying collection is modified
        /// directly instead of through the this <see cref="SessionWindowTransform{T}"/> wrapper.
        /// </summary>
        /// <param name="performEviction">Indicates that eviction should be re-run to remove elements from older session windows.</param>
        public void Refresh(bool performEviction)
        {
            // get rid of our cache of windows:
            _windows = null;

            if (performEviction)
            {
                switch (_collType)
                {
                    case CollectionType.List:
                        var asList = _source as IList<T>;
                        PerformEviction(asList);
                        break;
                    case CollectionType.LinkedList:
                        var asLinkedList = _source as LinkedList<T>;
                        PerformEviction(asLinkedList);
                        break;
                    default:
                        throw new NotImplementedException("Underlying collection type not supported");
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of time windows.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection of <see cref="ITimeWindow{TElement}"/> elements.</returns>
        public IEnumerator<ITimeWindow<T>> GetEnumerator()
        {
            if (_windows == null)
                _windows = _source.ToSessionWindows(_timestampSelector, _idleThreshold).ToList();

            return _windows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
