/* Copyright 2017 ScaleOut Software, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using Scaleout.Streaming.TimeWindowing;
using Scaleout.Streaming.TimeWindowing.Linq;

namespace Scaleout.Streaming.TimeWindowing.Tests
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
