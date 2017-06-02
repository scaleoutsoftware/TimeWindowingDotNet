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
    public class TumblingWindowTests
    {
        private readonly ITestOutputHelper _output;

        public static TheoryData<IEnumerable<DateTime>> TestCollectionSet1
        {
            get
            {
                /* First 5 days in December.
                 * Assuming 2-day tumbling transform over Dec [1,5):
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

        public TumblingWindowTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(TestCollectionSet1))]
        public void SimpleTumbling(IEnumerable<DateTime> coll)
        {
            var start = new DateTime(2017, 1, 1);
            var end = new DateTime(2017, 1, 5);

            var tumblingWindows = coll.ToTumblingWindows(elem => elem, start, end, TimeSpan.FromDays(2));

            foreach (var window in tumblingWindows)
            {
                _output.WriteLine($"{window.StartTime:d} - {window.EndTime:d}");
                foreach (var item in window)
                {
                    _output.WriteLine($"\t{item:d}");
                }
            }

            Assert.Equal(2, tumblingWindows.Count());
            Assert.Equal(2, tumblingWindows.First().Count());
            Assert.Equal(2, tumblingWindows.Last().Count());

            var list = tumblingWindows.ToList();
            Assert.Equal(new DateTime(2017, 1, 1), list[0].First());
            Assert.Equal(new DateTime(2017, 1, 2), list[0].Last());
            Assert.Equal(new DateTime(2017, 1, 3), list[1].First());
            Assert.Equal(new DateTime(2017, 1, 4), list[1].Last());
        }
    }
}
