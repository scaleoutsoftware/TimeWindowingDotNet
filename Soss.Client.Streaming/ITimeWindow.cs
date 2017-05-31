using System;
using System.Collections.Generic;
using System.Text;

namespace Soss.Client.Streaming
{
    /// <summary>
    /// Supports iteration over a collection of elements that fall within a time window.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public interface ITimeWindow<TElement> : IEnumerable<TElement>, ICollection<TElement>
    {
        /// <summary>
        /// Start time of the window.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// End Time of the window.
        /// </summary>
        DateTime EndTime { get; }
    }

}
