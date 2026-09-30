using Scaleout.Streaming.TimeWindowing.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scaleout.Streaming.TimeWindowing
{
    public class WatermarkedSessionWindowCollection<T> : IEnumerable<ITimeWindow<T>>
    {
        ICollection<T> _source;
        private Func<T, DateTime> _timestampSelector;
        private Func<DateTime, DateTime> _watermarkGenerator;

        public IReadOnlyCollection<T> Items => _source as IReadOnlyCollection<T>;
        public TimeSpan IdleThreshold { get; private set; }
        public DateTime CurrentWatermark { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="idleThreshold">Maximum allowed time gap between elements before a new session window is started.</param>
        /// <param name="watermarkGenerator">A function to generate the watermark based on the time of the latest element added. Entries that arrive before the watermark time are evicted.</param>
        public WatermarkedSessionWindowCollection(ICollection<T> source, Func<T, DateTime> timestampSelector, TimeSpan idleThreshold, Func<DateTime, DateTime> watermarkGenerator) :
            this(source, timestampSelector, idleThreshold, watermarkGenerator, DateTime.MinValue)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The linked list of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="idleThreshold">Maximum allowed time gap between elements before a new session window is started.</param>
        /// <param name="watermarkGenerator">A function to generate the watermark based on the time of the latest element added. Entries that arrive before the watermark time are evicted.</param>
        /// <param name="currentWatermark">The current watermark time.</param>
        public WatermarkedSessionWindowCollection(ICollection<T> source, Func<T, DateTime> timestampSelector, TimeSpan idleThreshold, Func<DateTime, DateTime> watermarkGenerator, DateTime currentWatermark)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _timestampSelector = timestampSelector ?? throw new ArgumentNullException(nameof(timestampSelector));
            IdleThreshold = (idleThreshold > TimeSpan.Zero) ? idleThreshold : throw new ArgumentOutOfRangeException(nameof(idleThreshold));
            _watermarkGenerator = watermarkGenerator ?? throw new ArgumentNullException(nameof(watermarkGenerator));
            CurrentWatermark = currentWatermark;
        }

        /// <summary>
        /// Adds an element to the underlying collection, inserting it into the underlying source collection
        /// in chronological order. If the timestamp associated with the new element falls before the
        /// collection's current watermark provided to this transformation's constructor then the new element will be
        /// evicted immediately.
        /// </summary>
        /// <param name="item">The element to add to the collection.</param>
        public IEnumerable<ITimeWindow<T>> Add(T item)
        {
            DateTime itemTimestamp = _timestampSelector(item);
            DateTime nextWatermark = _watermarkGenerator(itemTimestamp);
            if (nextWatermark > CurrentWatermark)
            {
                CurrentWatermark = nextWatermark;
            }

            if (itemTimestamp > CurrentWatermark)
            {
                // Use add/evict algorithm tuned to underlying collection type.
                switch (_source)
                {
                    case IList<T> list:
                        return AddToList(item);
                    case LinkedList<T> linkedList:
                        return AddToLinkedList(item);
                    default:
                        throw new NotImplementedException("Underlying collection type not supported");
                }
            }
            else
            {
                // Item is older than watermark, so don't add it and
                // don't do any watermark-based eviction.
                return Enumerable.Empty<ITimeWindow<T>>();
            }
        }

        private IEnumerable<ITimeWindow<T>> AddToList(T item)
        {
            IList<T> source = _source as IList<T>;

            source.AddTimeOrdered(item, _timestampSelector);

            return PerformEviction(source);
        }

        private IEnumerable<ITimeWindow<T>> AddToLinkedList(T item)
        {
            LinkedList<T> source = _source as LinkedList<T>;

            source.AddTimeOrdered(item, _timestampSelector);

            return PerformEviction(source);
        }


        private IEnumerable<ITimeWindow<T>> PerformEviction(ICollection<T> source)
        {
            // We don't implement this as a lazy (yield) enumerator because we need to remove items
            // from the source collection after we have finished generating all of the windows to evict.
            // If the user doesn't enumerate all of the windows to evict then we wouldn't remove items
            // from the source collection.

            if (source.Count == 0)
                return Enumerable.Empty<ITimeWindow<T>>();

            List<ITimeWindow<T>> evictedWindows = new List<ITimeWindow<T>>();

            var windows = source.ToSessionWindows(_timestampSelector, IdleThreshold);
            int itemsToRemoveCount = 0;
            foreach (var window in windows)
            {
                if (window.EndTime < CurrentWatermark)
                {
                    evictedWindows.Add(window);
                    itemsToRemoveCount += window.Count;
                }
                else
                {
                    // Done looking for windows to evict, since the rest of the windows will be after the watermark.
                    break;
                }
            }

            switch (source)
            {
                case IList<T> list:
                    list.RemoveFirstItems(itemsToRemoveCount);
                    break;
                case LinkedList<T> linkedList:
                    linkedList.RemoveFirstItems(itemsToRemoveCount);
                    break;
                default:
                    throw new NotImplementedException("Underlying collection type not supported");
            }

            return evictedWindows;
        }

        
        

        /// <summary>
        /// Returns an enumerator that iterates through the collection of time windows.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection of <see cref="ITimeWindow{TElement}"/> elements.</returns>
        public IEnumerator<ITimeWindow<T>> GetEnumerator()
        {
            if (_source.Count() == 0)
                return Enumerable.Empty<ITimeWindow<T>>().GetEnumerator();

            return _source.ToSessionWindows(_timestampSelector, IdleThreshold).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
