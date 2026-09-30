using System;
using System.Collections.Generic;
using System.Text;
using Scaleout.Streaming.TimeWindowing;
using Scaleout.Streaming.TimeWindowing.Linq;

namespace Scaleout.Streaming.TimeWindowing.Tests
{
    public class WatermarkedSlidingTests
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public void WatermarkedWindowedEvictionOverLapping()
        {
            List<DateTime> coll = new List<DateTime>();
            var start = new DateTime(2026, 1, 1);

            var wmSlidingColl = new WatermarkedSlidingWindowCollection<DateTime>(
                coll,
                dt => dt,
                windowDuration: TimeSpan.FromMinutes(10),
                every: OneMinute,
                startTime: start,
                watermarkGenerator: dt => dt.AddMinutes(-80));
            

            List<ITimeWindow<DateTime>> evictedWindows = new List<ITimeWindow<DateTime>>();
            for (int i = 0; i < 100; i++)
            {
                var evicted = wmSlidingColl.Add(start.AddMinutes(i));
                if (evicted != null)
                    evictedWindows.AddRange(evicted);
            }

            Assert.NotEmpty(evictedWindows);
            Assert.NotEmpty(wmSlidingColl.Items);
        }

        [Fact]
        public void WatermarkedWindowedEvictionOverLapping2()
        {
            LinkedList<DateTime> coll = new LinkedList<DateTime>();
            var start = new DateTime(2026, 1, 1);

            var wmSlidingColl = new WatermarkedSlidingWindowCollection<DateTime>(
                coll,
                dt => dt,
                windowDuration: TimeSpan.FromMinutes(10),
                every: OneMinute,
                startTime: start,
                watermarkGenerator: dt => dt.AddMinutes(-80));


            List<ITimeWindow<DateTime>> evictedWindows = new List<ITimeWindow<DateTime>>();
            for (int i = 0; i < 100; i++)
            {
                var evicted = wmSlidingColl.Add(start.AddMinutes(i));
                if (evicted != null)
                    evictedWindows.AddRange(evicted);
            }

            Assert.NotEmpty(evictedWindows);
            Assert.NotEmpty(wmSlidingColl.Items);
        }

        [Fact]
        public void SerializeToJson()
        {
            List<DateTime> coll = new List<DateTime>();
            var start = new DateTime(2026, 1, 1);
            var wmSlidingColl = new WatermarkedSlidingWindowCollection<DateTime>(
                coll,
                dt => dt,
                windowDuration: TimeSpan.FromMinutes(10),
                every: OneMinute,
                startTime: start,
                watermarkGenerator: dt => dt.AddMinutes(-80));
            for (int i = 0; i < 100; i++)
            {
                wmSlidingColl.Add(start.AddMinutes(i));
            }
            var json = System.Text.Json.JsonSerializer.Serialize(wmSlidingColl.Items);
            Assert.NotNull(json);

            var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<DateTime>>(json);
            var wmSlidingColl2 = new WatermarkedSlidingWindowCollection<DateTime>(
                deserialized,
                dt => dt,
                windowDuration: TimeSpan.FromMinutes(10),
                every: OneMinute,
                startTime: start,
                watermarkGenerator: dt => dt.AddMinutes(-80),
                currentWatermark: wmSlidingColl.CurrentWatermark);

            Assert.NotNull(wmSlidingColl2);
            Assert.Equal(wmSlidingColl.Items.Count, wmSlidingColl2.Items.Count);
        }
    }
}
