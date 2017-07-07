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

using Scaleout.Client.Streaming.Linq;

namespace Scaleout.Client.Streaming.Tests
{
    public class SessionWindowTests
    {
        private readonly ITestOutputHelper _output;

        public SessionWindowTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static TheoryData<IEnumerable<DateTime>> TestCollectionSet1
        {
            get
            {
                var ret = new TheoryData<IEnumerable<DateTime>>();

                List<DateTime> l1 = new List<DateTime>();

                DateTime session1 = new DateTime(2017, 1, 1, 13, 45, 13); // 1:45:13pm
                for (int i = 0; i < 7; i++)
                    l1.Add(session1.AddSeconds(i));

                DateTime session2 = new DateTime(2017, 1, 1, 14, 30, 42); // 2:30:42pm
                for (int i = 0; i < 11; i++)
                    l1.Add(session2.AddSeconds(i));


                ret.Add(l1);
                // add a linked list version of the collection, too:
                ret.Add(new LinkedList<DateTime>(l1));
                return ret;
            }
        }

        public static TheoryData<IEnumerable<DateTime>> TestCollectionSetEmpty
        {
            get
            {
                var ret = new TheoryData<IEnumerable<DateTime>>();
                ret.Add(new List<DateTime>());
                ret.Add(new LinkedList<DateTime>());
                return ret;
            }
        }

        [Theory]
        [MemberData(nameof(TestCollectionSet1))]
        public void SimpleSessionLinq(IEnumerable<DateTime> coll)
        {
            var sessionWindows = coll.ToSessionWindows(elem => elem, TimeSpan.FromMinutes(10));
            Assert.Equal(2, sessionWindows.Count());

            foreach (var window in sessionWindows)
            {
                _output.WriteLine($"{window.StartTime:T} - {window.EndTime:T}");
                foreach (var item in window)
                {
                    _output.WriteLine($"\t{item:T}");
                }
            }

            var list = sessionWindows.ToList();
            Assert.Equal(7, sessionWindows.First().Count());
            Assert.Equal(11, sessionWindows.Last().Count());
        }

        [Theory]
        [MemberData(nameof(TestCollectionSetEmpty))]
        public void EmptySessionLinq(IEnumerable<DateTime> coll)
        {
            var sessionWindows = coll.ToSessionWindows(elem => elem, TimeSpan.FromSeconds(1));
            Assert.Equal(0, sessionWindows.Count());
        }

        [Fact]
        public void AddToTranformOfList()
        {
            var source = new List<DateTime>();

            var sessTransform = new SessionWindowTransform<DateTime>(source, elem => elem, TimeSpan.FromMinutes(10), int.MaxValue);
            Assert.Equal(0, sessTransform.Count());

            sessTransform.Add(new DateTime(2017, 1, 1, 14, 30, 42)); // 2:30:42pm
            Assert.Equal(1, source.Count);
            Assert.Equal(1, sessTransform.Count());

            sessTransform.Add(new DateTime(2017, 1, 1, 13, 45, 13)); // 1:45:13pm
            Assert.Equal(2, source.Count);
            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), source[0]);
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), source[1]);
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), sessTransform.Last().Last());

            sessTransform.Add(new DateTime(2017, 1, 1, 13, 45, 14)); // 1:45:14pm
            Assert.Equal(3, source.Count);
            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), source[0]);
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 14), source[1]);
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), source[2]);
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 14), sessTransform.First().Last());

            sessTransform.Add(new DateTime(2017, 1, 1, 14, 30, 43)); // 2:30:43pm
            Assert.Equal(4, source.Count);
            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), source[0]);
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 14), source[1]);
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), source[2]);
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 43), source[3]);
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 43), sessTransform.Last().Last());
        }

        [Fact]
        public void AddToTranformOfLinkedList()
        {
            var source = new LinkedList<DateTime>();

            var sessTransform = new SessionWindowTransform<DateTime>(source, elem => elem, TimeSpan.FromMinutes(10), int.MaxValue);
            Assert.Equal(0, sessTransform.Count());

            sessTransform.Add(new DateTime(2017, 1, 1, 14, 30, 42)); // 2:30:42pm
            Assert.Equal(1, source.Count);
            Assert.Equal(1, sessTransform.Count());

            sessTransform.Add(new DateTime(2017, 1, 1, 13, 45, 13)); // 1:45:13pm
            Assert.Equal(2, source.Count);
            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), source.First.Value);
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), source.ElementAt(1));
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), sessTransform.Last().Last());

            sessTransform.Add(new DateTime(2017, 1, 1, 13, 45, 14)); // 1:45:14pm
            Assert.Equal(3, source.Count);
            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), source.ElementAt(0));
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 14), source.ElementAt(1));
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), source.ElementAt(2));
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 14), sessTransform.First().Last());

            sessTransform.Add(new DateTime(2017, 1, 1, 14, 30, 43)); // 2:30:43pm
            Assert.Equal(4, source.Count);
            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), source.ElementAt(0));
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 14), source.ElementAt(1));
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), source.ElementAt(2));
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 43), source.ElementAt(3));
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 43), sessTransform.Last().Last());
        }

        [Fact]
        public void ListEviction()
        {
            var source = new List<DateTime>();
            var sessTransform = new SessionWindowTransform<DateTime>(source, elem => elem, TimeSpan.FromMinutes(10), 2);

            // Add two sessions worth of data:
            DateTime session1 = new DateTime(2017, 1, 1, 13, 45, 13); // 1:45:13pm
            for (int i = 0; i < 7; i++)
                sessTransform.Add(session1.AddSeconds(i));

            DateTime session2 = new DateTime(2017, 1, 1, 14, 30, 42); // 2:30:42pm
            for (int i = 0; i < 11; i++)
                sessTransform.Add(session2.AddSeconds(i));

            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), sessTransform.First().First());

            // Add a third session, verify that the first is evicted.
            sessTransform.Add(new DateTime(2017, 1, 1, 15, 30, 00)); // 3:30:00pm

            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), sessTransform.First().First());
            Assert.Equal(new DateTime(2017, 1, 1, 15, 30, 00), sessTransform.Last().First());
        }

        [Fact]
        public void LinkedListEviction()
        {
            var source = new LinkedList<DateTime>();
            var sessTransform = new SessionWindowTransform<DateTime>(source, elem => elem, TimeSpan.FromMinutes(10), 2);

            // Add two sessions worth of data:
            DateTime session1 = new DateTime(2017, 1, 1, 13, 45, 13); // 1:45:13pm
            for (int i = 0; i < 7; i++)
                sessTransform.Add(session1.AddSeconds(i));

            DateTime session2 = new DateTime(2017, 1, 1, 14, 30, 42); // 2:30:42pm
            for (int i = 0; i < 11; i++)
                sessTransform.Add(session2.AddSeconds(i));

            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 13, 45, 13), sessTransform.First().First());

            // Add a third session, verify that the first is evicted.
            sessTransform.Add(new DateTime(2017, 1, 1, 15, 30, 00)); // 3:30:00pm

            Assert.Equal(2, sessTransform.Count());
            Assert.Equal(new DateTime(2017, 1, 1, 14, 30, 42), sessTransform.First().First());
            Assert.Equal(new DateTime(2017, 1, 1, 15, 30, 00), sessTransform.Last().First());
        }
    }
}
