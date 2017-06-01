using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using Soss.Client.Streaming;
using Soss.Client.Streaming.Linq;

namespace Soss.Client.Streaming.Tests
{
    public class SlidingWindowTests
    {
        private readonly ITestOutputHelper _output;

        public static TheoryData<IEnumerable<DateTime>> TestCollectionSet1
        {
            get
            {
                /* First 5 days in December.
                 * Assuming sliding transform over Dec [1,5)
                 *    with period of 1 day and duration of 2 days:
                 * 
                 * [-)      1,2
                 *  [-)     2,3
                 *   [-)    3,4
                 *    [)    4
                 * |||||    
                 * 12345
                 * 
                 * Assuming tumbling transform over Dec [1,5)
                 *    with duration of 2 days:
                 *    
                 * [-)      1,2
                 *   [-)    3,4
                 * |||||    
                 * 12345
                 **/

                var ret = new TheoryData<IEnumerable<DateTime>>();
                List<DateTime> l1 = new List<DateTime>();
                for (int i = 1; i <= 5; i++)
                    l1.Add(new DateTime(2017, 1, i));

                ret.Add(l1);
                // add a linked list version of the collection, too:
                ret.Add(new LinkedList<DateTime>(l1));
                return ret;
            }
        }

        public SlidingWindowTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(TestCollectionSet1))]
        public void SimpleSliding(IEnumerable<DateTime> coll)
        {
            var start = new DateTime(2017, 1, 1);
            var end = new DateTime(2017, 1, 5);

            var slidingWindows = coll.ToSlidingWindows(dt => dt, start, end, windowDuration: TimeSpan.FromDays(2), every: TimeSpan.FromDays(1));
            Assert.Equal(4, slidingWindows.Count());

            foreach (var window in slidingWindows)
            {
                _output.WriteLine($"{window.StartTime:d} - {window.EndTime:d}");
                foreach (var item in window)
                {
                    _output.WriteLine($"\t{item:d}");
                }
            }

            var list = slidingWindows.ToList();
            Assert.Equal(2, slidingWindows.First().Count());
            Assert.Equal(1, slidingWindows.Last().Count());
        }

        [Fact]
        public void Empty()
        {
            var start = new DateTime(2017, 1, 1);
            var end = new DateTime(2017, 1, 5);

            DateTime[] emptyArr = new DateTime[0];
            var slidingWindows = emptyArr.ToSlidingWindows(dt => dt, start, end, windowDuration: TimeSpan.FromDays(2), every: TimeSpan.FromDays(1));
            Assert.Equal(4, slidingWindows.Count());

            foreach (var window in slidingWindows)
            {
                Assert.Empty(window);
            }
        }


    }
}
