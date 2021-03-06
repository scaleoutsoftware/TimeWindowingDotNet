﻿/* Copyright 2017-2018 ScaleOut Software, Inc.
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
    public class SlidingWindowTests
    {
        private readonly ITestOutputHelper _output;
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        public static TheoryData<IEnumerable<DateTime>> TestCollectionSet1
        {
            get
            {
                /* First 5 days in January.
                 * Assuming sliding transform over Jan [1,5)
                 *    with period of 1 day and duration of 2 days:
                 * 
                 * [-)      1,2
                 *  [-)     2,3
                 *   [-)    3,4
                 *    [)    4
                 * |||||    
                 * 12345
                 * 
                 * Assuming tumbling transform over Jan [1,5)
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
        public void SimpleSlidingLinq(IEnumerable<DateTime> coll)
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
        public void EmptyLinq()
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

        [Fact]
        public void AddToTranformOfList()
        {
            DateTime startTime = new DateTime(2017, 1, 1, 13, 0, 0); // 1pm
            var source = new List<DateTime>();
            var slidingTransform = new SlidingWindowCollection<DateTime>(source, dt => dt,
                                                              windowDuration: TimeSpan.FromMinutes(10),
                                                              every: OneMinute,
                                                              startTime: startTime);
            Assert.Empty(slidingTransform);

            // Add a element prior to date. Ensure it's immediately evicted.
            slidingTransform.Add(new DateTime(2017, 1, 1, 12, 59, 59)); //12:59pm
            Assert.Empty(slidingTransform);
            Assert.Empty(source);

            slidingTransform.Add(new DateTime(2017, 1, 1, 13, 0, 0)); // 1pm
            Assert.Equal(1, source.Count);
            Assert.Equal(1, slidingTransform.Count());

            slidingTransform.Add(new DateTime(2017, 1, 1, 13, 0, 1)); // 1:00:01pm
            Assert.Equal(2, source.Count);
            Assert.Equal(1, slidingTransform.Count());

            slidingTransform.Add(new DateTime(2017, 1, 1, 13, 1, 0)); // 1:01:00pm
            Assert.Equal(3, source.Count);
            Assert.Equal(2, slidingTransform.Count()); // 2 windows
            Assert.Equal(3, slidingTransform.First().Count); // first window has all three
            Assert.Equal(1, slidingTransform.Last().Count);  // second window just has the 1:01:00 element.


            // do a second transform with a later start time. Make sure eviction is performed.
            var transform2 = new SlidingWindowCollection<DateTime>(source, dt => dt,
                                                                  windowDuration: TimeSpan.FromMinutes(10),
                                                                  every: OneMinute,
                                                                  startTime: startTime + OneMinute);

            Assert.Equal(1, source.Count); // first two items in source collection shouldn've been evicted.
            Assert.Equal(1, transform2.Count()); // 1 window now
            Assert.Equal(1, transform2.First().Count); // first (and only) window just has the 1:01:00 element

        }

        [Fact]
        public void AddToTranformOfLinkedList()
        {
            DateTime startTime = new DateTime(2017, 1, 1, 13, 0, 0); // 1pm
            var source = new LinkedList<DateTime>();
            var slidingTransform = new SlidingWindowCollection<DateTime>(source, dt => dt,
                                                              windowDuration: TimeSpan.FromMinutes(10),
                                                              every: OneMinute,
                                                              startTime: startTime);
            Assert.Empty(slidingTransform);

            // Add a element prior to date. Ensure it's immediately evicted.
            slidingTransform.Add(new DateTime(2017, 1, 1, 12, 59, 59)); //12:59pm
            Assert.Empty(slidingTransform);
            Assert.Empty(source);

            slidingTransform.Add(new DateTime(2017, 1, 1, 13, 0, 0)); // 1pm
            Assert.Equal(1, source.Count);
            Assert.Equal(1, slidingTransform.Count());

            slidingTransform.Add(new DateTime(2017, 1, 1, 13, 0, 1)); // 1:00:01pm
            Assert.Equal(2, source.Count);
            Assert.Equal(1, slidingTransform.Count());

            slidingTransform.Add(new DateTime(2017, 1, 1, 13, 1, 0)); // 1:01:00pm
            Assert.Equal(3, source.Count);
            Assert.Equal(2, slidingTransform.Count()); // 2 windows
            Assert.Equal(3, slidingTransform.First().Count); // first window has all three
            Assert.Equal(1, slidingTransform.Last().Count);  // second window just has the 1:01:00 element.


            // do a second transform with a later start time. Make sure eviction is performed.
            var transform2 = new SlidingWindowCollection<DateTime>(source, dt => dt,
                                                                  windowDuration: TimeSpan.FromMinutes(10),
                                                                  every: OneMinute,
                                                                  startTime: startTime + OneMinute);

            Assert.Equal(1, source.Count); // first two items in source collection shouldn've been evicted.
            Assert.Equal(1, transform2.Count()); // 1 window now
            Assert.Equal(1, transform2.First().Count); // first (and only) window just has the 1:01:00 element
        }


    }
}
