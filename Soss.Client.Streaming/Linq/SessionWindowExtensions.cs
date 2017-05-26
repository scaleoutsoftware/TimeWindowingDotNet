using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soss.Client.Streaming.Linq
{
    public static class SessionWindowExtensions
    {
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
