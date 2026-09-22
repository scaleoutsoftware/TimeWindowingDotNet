using Scaleout.Streaming.TimeWindowing.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scaleout.Streaming.TimeWindowing
{
    internal class WatermarkedSlidingWindowCollection<T> : IEnumerable<ITimeWindow<T>>
    {
        IEnumerable<T> _source;

        private CollectionType _collType;
        private Func<T, DateTime> _timestampSelector;
        private Func<DateTime, DateTime> _watermarkGenerator;
        private DateTime _currentWatermark;
        private DateTime _startTime;

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
        /// <param name="startTime">Start time (inclusive) of the first sliding window.</param>
        /// <param name="watermarkGenerator">A function to generate a watermark. Entries that arrive before the watermark time are evicted.</param>
        public WatermarkedSlidingWindowCollection(IList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator)
        {
            _collType = CollectionType.List;
            Init(source, timestampSelector, windowDuration, every, startTime, watermarkGenerator);
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
        /// <param name="startTime">Start time (inclusive) of the first sliding window.</param>
        /// <param name="watermarkGenerator">A function to generate a watermark. Entries that arrive before the watermark time are evicted.</param>
        public WatermarkedSlidingWindowCollection(LinkedList<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator)
        {
            _collType = CollectionType.LinkedList;
            Init(source, timestampSelector, windowDuration, every, startTime, watermarkGenerator);
        }

        private void Init(IEnumerable<T> source, Func<T, DateTime> timestampSelector, TimeSpan windowDuration, TimeSpan every, DateTime startTime, Func<DateTime, DateTime> watermarkGenerator)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _timestampSelector = timestampSelector ?? throw new ArgumentNullException(nameof(timestampSelector));
            _watermarkGenerator = watermarkGenerator ?? throw new ArgumentNullException(nameof(watermarkGenerator));
            if (every > windowDuration)
                throw new ArgumentException("Window duration must be larger than period.");

            _windowDuration = windowDuration;
            _every = every;
            _startTime = startTime;

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
            _currentWatermark = DateTime.MinValue;
        }

        private void TrimToStartTime()
        {
            // Use add/evict algorithm tuned to underlying collection type.
            switch (_collType)
            {
                case CollectionType.List:
                    IList<T> list = _source as IList<T>;
                    // find index of first element to keep.
                    int countOfItemsToRemove = 0;
                    while (countOfItemsToRemove < list.Count)
                    {
                        if (_timestampSelector(list[countOfItemsToRemove]) < _startTime)
                            countOfItemsToRemove++;
                        else
                            break;
                    }

                    if (countOfItemsToRemove > 0)
                        list.RemoveFirstItems(countOfItemsToRemove);
                    break;
                case CollectionType.LinkedList:
                    LinkedList<T> linkedList = _source as LinkedList<T>;
                    while (linkedList.First != null)
                    {
                        if (_timestampSelector(linkedList.First.Value) < _startTime)
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
            if (nextWatermark > _currentWatermark)
            {
                _currentWatermark = nextWatermark;
            }

            if (itemTimestamp > _currentWatermark)
            {
                // Use add/evict algorithm tuned to underlying collection type.
                switch (_collType)
                {
                    case CollectionType.List:
                        return AddToList(item);
                    case CollectionType.LinkedList:
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

        private IEnumerable<ITimeWindow<T>> PerformEviction()
        {
            // Use add/evict algorithm tuned to underlying collection type.
            switch (_collType)
            {
                case CollectionType.List:
                    return PerformEviction(_source as IList<T>);
                case CollectionType.LinkedList:
                    return PerformEviction(_source as LinkedList<T>);
                default:
                    throw new NotImplementedException("Underlying collection type not supported");
            }
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


            var intervalGen = new OpenSlidingWindowIntervalGenerator<T>(_startTime, _every, _windowDuration);
            foreach (var window in intervalGen)
            {
                if (window.EndTime < _currentWatermark)
                {
                    startingIndexHint = window.SetItems(source, startingIndexHint, _timestampSelector);
                    evictedWindows.Add(window);
                }
                else
                {
                    // Done looking for windows to evict, since the rest of the windows will be after the watermark.
                    // This window is now the start time for the entire collection. We'll be evicting
                    // all items prior to this window's start time
                    _startTime = window.StartTime;
                    break;
                }
            }
            
            // Perform eviction of items from the source collection.
            // First, figure out how many items to remove.
            int countOfItemsToRemove = 0;
            while (countOfItemsToRemove < source.Count)
            {
                if (_timestampSelector(source[countOfItemsToRemove]) < _startTime)
                    countOfItemsToRemove++;
                else
                    break;
            }

            // Do removal.
            if (countOfItemsToRemove > 0)
                source.RemoveFirstItems(countOfItemsToRemove);

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

            var intervalGen = new OpenSlidingWindowIntervalGenerator<T>(_startTime, _every, _windowDuration);

            var startHintNode = source.First;
            foreach (var window in intervalGen)
            {
                if (window.EndTime < _currentWatermark)
                {
                    startHintNode = window.SetItems(startHintNode, _timestampSelector);
                    evictedWindows.Add(window);
                }
                else
                {
                    // Done looking for windows to evict, since the rest of the windows will be after the watermark.
                    // This window is now the start time for the entire collection. We'll be evicting
                    // all items prior to this window's start time
                    _startTime = window.StartTime;
                    break;
                }
            }

            
            while (source.First != null)
            {
                if (_timestampSelector(source.First.Value) < _startTime)
                    source.RemoveFirst();
                else
                    break;
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

            DateTime endTime = _timestampSelector(_source.Last()).AddTicks(1);
            return _source.ToSlidingWindows(_timestampSelector, _startTime, endTime, _windowDuration, _every).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
