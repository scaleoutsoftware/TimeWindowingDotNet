using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Scaleout.Client.Streaming
{
    /// <summary>
    /// Transforms a collection into an enumerable collection of fixed-time windows and manages 
    /// eviction of old items from the source collection. 
    /// This wrapper can be used to manage the retention policy of the underlying collection.
    /// Objects added through this wrapper are inserted in chronologial
    /// order and evicted according to the <c>startTime</c> policy provided to the constructor.
    /// </summary>
    /// <typeparam name="T">The type of objects in the source collection.</typeparam>
    public class TumblingWindowTransform<T> : IEnumerable<ITimeWindow<T>>
    {
        // Tumbling windows are just a special case of sliding windows where the duration of each
        // window is the same as the period between the start of each window. So this class is really 
        // just a nice alias--it just wraps a SlidingWindowTransform and lets it do the hard work. 
        SlidingWindowTransform<T> _transform;

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
        public TumblingWindowTransform(IList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, DateTime startTime)
        {
            _transform = new SlidingWindowTransform<T>(source, timestampSelector, windowDuration, windowDuration, startTime);
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
        public TumblingWindowTransform(LinkedList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime)
        {
            _transform = new SlidingWindowTransform<T>(source, timestampSelector, windowDuration, windowDuration, startTime);
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
