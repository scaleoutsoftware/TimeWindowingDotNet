using System;
using System.Collections.Generic;
using System.Text;
using Scaleout.Streaming.TimeWindowing;
using Scaleout.Streaming.TimeWindowing.Linq;

namespace Scaleout.Streaming.TimeWindowing.Tests
{
    public class WatermarkedSessionTests
    {
        [Fact]
        public void WatermarkedSessionEviction()
        {
            List<DateTime> coll = new List<DateTime>();
            var start = new DateTime(2026, 1, 1);

            var wmSessionColl = new WatermarkedSessionWindowCollection<DateTime>(
                coll,
                dt => dt,
                idleThreshold: TimeSpan.FromMinutes(10),
                watermarkGenerator: dt => dt.AddMinutes(-60));


            List<ITimeWindow<DateTime>> evictedWindows = new List<ITimeWindow<DateTime>>();
            DateTime session1 = new DateTime(2026, 1, 1, 13, 45, 00); // 1:45:00pm
            for (int i = 0; i < 7; i++)
            {
                var evicted = wmSessionColl.Add(session1.AddSeconds(i));
                Assert.Empty(evicted);
            }

            DateTime session2 = new DateTime(2026, 1, 1, 16, 45, 00); // 4:45:00pm
            for (int i = 0; i < 11; i++)
            {
                var evicted = wmSessionColl.Add(session2.AddSeconds(i));
                evictedWindows.AddRange(evicted);
            }

            Assert.Single(evictedWindows);
        }

        [Fact]
        public void WatermarkedSessionEviction2()
        {
            LinkedList<DateTime> coll = new LinkedList<DateTime>();
            var start = new DateTime(2026, 1, 1);

            var wmSessionColl = new WatermarkedSessionWindowCollection<DateTime>(
                coll,
                dt => dt,
                idleThreshold: TimeSpan.FromMinutes(10),
                watermarkGenerator: dt => dt.AddMinutes(-60));


            List<ITimeWindow<DateTime>> evictedWindows = new List<ITimeWindow<DateTime>>();
            DateTime session1 = new DateTime(2026, 1, 1, 13, 45, 00); // 1:45:00pm
            for (int i = 0; i < 7; i++)
            {
                var evicted = wmSessionColl.Add(session1.AddSeconds(i));
                Assert.Empty(evicted);
            }

            DateTime session2 = new DateTime(2026, 1, 1, 16, 45, 00); // 4:45:00pm
            for (int i = 0; i < 11; i++)
            {
                var evicted = wmSessionColl.Add(session2.AddSeconds(i));
                evictedWindows.AddRange(evicted);
            }

            Assert.Single(evictedWindows);
        }
    }
}
