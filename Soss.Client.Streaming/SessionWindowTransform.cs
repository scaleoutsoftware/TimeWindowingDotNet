using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Soss.Client.Streaming.Linq;

namespace Soss.Client.Streaming
{
    internal enum CollectionType : byte { List, LinkedList }

    public class SessionWindowTransform<T> : IEnumerable<ITimeWindow<T>>
    {
        IEnumerable<T> _source;
        List<ITimeWindow<T>> _windows;

        private CollectionType _collType;
        private Func<T, DateTime> _timestampSelector;
        private TimeSpan _idleThreshold;
        private int _boundedSessionCapacity;

        public SessionWindowTransform(IList<T> source, Func<T, DateTime> timestampSelector, TimeSpan idleThreshold, int boundedSessionCapacity)
        {
            _collType = CollectionType.List;
            Init(source, timestampSelector, idleThreshold, boundedSessionCapacity);
        }

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
        }

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
            DateTime timestamp = _timestampSelector(item);
            IList<T> source = _source as IList<T>;

            int insertPosition = source.Count;
            while (insertPosition > 0)
            {
                if (timestamp < _timestampSelector(source[insertPosition - 1]))
                    insertPosition--;
                else
                    break;
            }

            source.Insert(insertPosition, item);

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
            DateTime timestamp = _timestampSelector(item);
            LinkedList<T> source = _source as LinkedList<T>;

            var nodeToInsertAfter = source.Last;
            while (nodeToInsertAfter != null)
            {
                if (timestamp < _timestampSelector(nodeToInsertAfter.Value))
                    nodeToInsertAfter = nodeToInsertAfter.Previous;
                else
                    break;
            }

            // Found the place, now do the insert
            if (nodeToInsertAfter != null)
                source.AddAfter(nodeToInsertAfter, item);
            else
            {
                // adding as first item (or else adding to an empty linked list)
                source.AddFirst(item);
            }

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

        IEnumerable<T> Source { get { return _source; } }

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
