using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scaleout.Streaming.TimeWindowing
{
    public class WatermarkedTumblingWindowCollection<T> : IEnumerable<ITimeWindow<T>>
    {
        // Tumbling windows are just a special case of sliding windows where the duration of each
        // window is the same as the period between the start of each window. So this class is really 
        // just a nice alias--it just wraps a SlidingWindowCollection and lets it do the hard work. 
        WatermarkedSlidingWindowCollection<T> _transform;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The source collection of elements.</param>
        /// <param name="timestampSelector">A function to extract the timestamp from each element.</param>
        /// <param name="windowDuration">The duration of each tumbling window.</param>
        /// <param name="startTime">The start time for the first window.</param>
        /// <param name="watermarkGenerator">A function to generate the watermark based on the time of the latest element added. Entries that arrive before the watermark time are evicted.</param>
        public WatermarkedTumblingWindowCollection(IList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator)
        {
            _transform = new WatermarkedSlidingWindowCollection<T>(source, timestampSelector, windowDuration, windowDuration, startTime, watermarkGenerator);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The source collection of elements.</param>
        /// <param name="timestampSelector">A function to extract the timestamp from each element.</param>
        /// <param name="windowDuration">The duration of each tumbling window.</param>
        /// <param name="startTime">The start time for the first window.</param>
        /// <param name="watermarkGenerator">A function to generate the watermark based on the time of the latest element added. Entries that arrive before the watermark time are evicted.</param>
        public WatermarkedTumblingWindowCollection(LinkedList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator)
        {
            _transform = new WatermarkedSlidingWindowCollection<T>(source, timestampSelector, windowDuration, windowDuration, startTime, watermarkGenerator);
        }

        /// <summary>
        /// Adds an element to the underlying collection, inserting it into the underlying source collection
        /// in chronological order. If the timestamp associated with the new element falls before the
        /// collection's current watermark provided to this transformation's constructor then the new element will be
        /// evicted immediately.
        /// </summary>
        /// <param name="item">The element to add to the collection.</param>
        public IEnumerable<ITimeWindow<T>> Add(T item) => _transform.Add(item);

        /// <summary>
        /// Returns an enumerator that iterates through the collection of time windows.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection of <see cref="ITimeWindow{TElement}"/> elements.</returns>
        public IEnumerator<ITimeWindow<T>> GetEnumerator() => _transform.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _transform.GetEnumerator();
    }
}
