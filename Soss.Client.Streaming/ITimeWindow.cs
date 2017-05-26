using System;
using System.Collections.Generic;
using System.Text;

namespace Soss.Client.Streaming
{
    public interface ITimeWindow<TElement> : IEnumerable<TElement>, ICollection<TElement>
    {
        DateTime StartTime { get; }

        DateTime EndTime { get; }
    }

}
