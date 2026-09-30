using Scaleout.Streaming.TimeWindowing.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Scaleout.Streaming.TimeWindowing
{
    /// <summary>
    /// Iterable collection of time windows that are generated from an underlying 
    /// collection of elements.
    /// </summary>
    /// <remarks>
    /// The underlying collection is expected to be ordered 
    /// by timestamp, and the time windows are generated based on a sliding window
    /// algorithm. The collection also supports watermark-based closure of time windows
    /// and eviction of old elements from the underlying collection.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class WatermarkedSlidingWindowCollection<T> : IEnumerable<ITimeWindow<T>>
    {
        ICollection<T> _source;

        private Func<T, DateTime> _timestampSelector;
        private Func<DateTime, DateTime> _watermarkGenerator;

        public IReadOnlyCollection<T> Items => _source as IReadOnlyCollection<T>;
        public DateTime CurrentWatermark { get; private set; }
        public DateTime StartTime { get; private set; }
        public TimeSpan WindowDuration { get; private set; }
        public TimeSpan Every { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">An enumerable collection of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that will be shortened for the last window(s) 
        /// in the returned sequence (see remarks).
        /// </param>
        /// <param name="every">The period of time between the start of each sliding window.</param>
        /// <param name="startTime">Start time (inclusive) of the first sliding window.</param>
        /// <param name="watermarkGenerator">A function to generate a watermark. Entries that arrive before the watermark time are evicted.</param>
        public WatermarkedSlidingWindowCollection(ICollection<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator)
        {
            Init(source, timestampSelector, windowDuration, every, startTime, watermarkGenerator, DateTime.MinValue);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">An enumerable collection of elements to wrap with the transformation.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that will be shortened for the last window(s) 
        /// in the returned sequence (see remarks).
        /// </param>
        /// <param name="every">The period of time between the start of each sliding window.</param>
        /// <param name="startTime">Start time (inclusive) of the first sliding window.</param>
        /// <param name="watermarkGenerator">A function to generate a watermark. Entries that arrive before the watermark time are evicted.</param>
        /// <param name="currentWatermark">The current watermark time.</param>
        public WatermarkedSlidingWindowCollection(ICollection<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator, DateTime currentWatermark)
        {
            Init(source, timestampSelector, windowDuration, every, startTime, watermarkGenerator, currentWatermark);
        }

        private void Init(ICollection<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator, DateTime currentWatermark)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _timestampSelector = timestampSelector ?? throw new ArgumentNullException(nameof(timestampSelector));
            _watermarkGenerator = watermarkGenerator ?? throw new ArgumentNullException(nameof(watermarkGenerator));
            if (every > windowDuration)
                throw new ArgumentException("Window duration must be larger than period.");

            WindowDuration = windowDuration;
            Every = every;
            StartTime = startTime;

            // We silently do eviction here before any watermark-based eviction logic is performed.
            // This is to ensure that the collection is in a valid state before we start adding new elements--
            // we don't want any elements that are older than the start time to be in the collection
            // when we start adding new elements and start doing watermark-based eviction.
            // (Another possible option worth discussing would be to throw an exception)
            TrimToStartTime();

            // Unlike the normal sliding window collection, we don't want to start at the timestamp
            // of the first element in the source collection. Instead, we wait for an add to start at the
            // timestamp of the first element that is added to the collection.
            // So we set the watermark to DateTime.MaxValue and update it when the first element is added.
            CurrentWatermark = currentWatermark;
        }

        private void TrimToStartTime()
        {
            // Use add/evict algorithm tuned to underlying collection type.
            switch (_source)
            {
                case IList<T> list:
                    // find index of first element to keep.
                    int countOfItemsToRemove = 0;
                    while (countOfItemsToRemove < list.Count)
                    {
                        if (_timestampSelector(list[countOfItemsToRemove]) < StartTime)
                            countOfItemsToRemove++;
                        else
                            break;
                    }

                    if (countOfItemsToRemove > 0)
                        list.RemoveFirstItems(countOfItemsToRemove);
                    break;
                case LinkedList<T> linkedList:
                    while (linkedList.First != null)
                    {
                        if (_timestampSelector(linkedList.First.Value) < StartTime)
                            linkedList.RemoveFirst();
                        else
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException("Underlying collection type not supported");
            }
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

        private IEnumerable<ITimeWindow<T>> PerformEviction(IList<T> source)
        {
            // We don't implement this as a lazy (yield) enumerator because we need to remove items
            // from the source collection after we have finished generating all of the windows to evict.
            // If the user doesn't enumerate all of the windows to evict then we wouldn't remove items
            // from the source collection.

            if (source.Count == 0)
                return Enumerable.Empty<ITimeWindow<T>>();

            List<ITimeWindow<T>> evictedWindows = new List<ITimeWindow<T>>();

            // location in the source collection where the next window should start looking for elements.
            int startingIndexHint = 0;


            var intervalGen = new OpenSlidingWindowIntervalGenerator<T>(StartTime, Every, WindowDuration);
            foreach (var window in intervalGen)
            {
                if (window.EndTime < CurrentWatermark)
                {
                    startingIndexHint = window.SetItems(source, startingIndexHint, _timestampSelector);
                    evictedWindows.Add(window);
                }
                else
                {
                    // Done looking for windows to evict, since the rest of the windows will be after the watermark.
                    // This window is now the start time for the entire collection. We'll be evicting
                    // all items prior to this window's start time
                    StartTime = window.StartTime;
                    break;
                }
            }

            // Perform eviction of items from the source collection.
            TrimToStartTime();

            return evictedWindows;
        }

        private IEnumerable<ITimeWindow<T>> AddToLinkedList(T item)
        {
            LinkedList<T> source = _source as LinkedList<T>;

            source.AddTimeOrdered(item, _timestampSelector);

            return PerformEviction(source);
        }

        private IEnumerable<ITimeWindow<T>> PerformEviction(LinkedList<T> source)
        {
            // We don't implement this as a lazy (yield) enumerator because we need to remove items
            // from the source collection after we have finished generating all of the windows to evict.
            // If the user doesn't enumerate all of the windows to evict then we wouldn't remove items
            // from the source collection.

            if (source.Count == 0)
                return Enumerable.Empty<ITimeWindow<T>>();

            List<ITimeWindow<T>> evictedWindows = new List<ITimeWindow<T>>();

            var intervalGen = new OpenSlidingWindowIntervalGenerator<T>(StartTime, Every, WindowDuration);

            var startHintNode = source.First;
            foreach (var window in intervalGen)
            {
                if (window.EndTime < CurrentWatermark)
                {
                    startHintNode = window.SetItems(startHintNode, _timestampSelector);
                    evictedWindows.Add(window);
                }
                else
                {
                    // Done looking for windows to evict, since the rest of the windows will be after the watermark.
                    // This window is now the start time for the entire collection. We'll be evicting
                    // all items prior to this window's start time
                    StartTime = window.StartTime;
                    break;
                }
            }

            // Perform eviction of items from the source collection.
            TrimToStartTime();

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

            DateTime endTime = _timestampSelector(_source.Last()).AddTicks(1);
            return _source.ToSlidingWindows(_timestampSelector, StartTime, endTime, WindowDuration, Every).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
