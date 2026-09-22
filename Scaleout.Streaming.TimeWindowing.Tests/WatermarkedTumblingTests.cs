using System;
using System.Collections.Generic;
using System.Text;
using Scaleout.Streaming.TimeWindowing;
using Scaleout.Streaming.TimeWindowing.Linq;

namespace Scaleout.Streaming.TimeWindowing.Tests
{
    public class WatermarkedTumblingTests
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public void WatermarkedTumblingEvictionOverLapping()
        {
            List<DateTime> coll = new List<DateTime>();
            var start = new DateTime(2026, 1, 1);

            var wmTumblingColl = new WatermarkedTumblingWindowCollection<DateTime>(
                coll,
                dt => dt,
                windowDuration: TimeSpan.FromMinutes(10),
                startTime: start,
                watermarkGenerator: dt => dt.AddMinutes(-80));
            

            List<ITimeWindow<DateTime>> evictedWindows = new List<ITimeWindow<DateTime>>();
            for (int i = 0; i <= 100; i++)
            {
                var evicted = wmTumblingColl.Add(start.AddMinutes(i));
                if (evicted != null)
                    evictedWindows.AddRange(evicted);
            }

            Assert.NotEmpty(evictedWindows);
        }

        [Fact]
        public void WatermarkedTumblingEvictionOverLapping2()
        {
            LinkedList<DateTime> coll = new LinkedList<DateTime>();
            var start = new DateTime(2026, 1, 1);

            var wmTumblingColl = new WatermarkedTumblingWindowCollection<DateTime>(
                coll,
                dt => dt,
                windowDuration: TimeSpan.FromMinutes(10),
                startTime: start,
                watermarkGenerator: dt => dt.AddMinutes(-80));


            List<ITimeWindow<DateTime>> evictedWindows = new List<ITimeWindow<DateTime>>();
            for (int i = 0; i <= 100; i++)
            {
                var evicted = wmTumblingColl.Add(start.AddMinutes(i));
                if (evicted != null)
                    evictedWindows.AddRange(evicted);
            }

            Assert.NotEmpty(evictedWindows);
        }
    }
}
