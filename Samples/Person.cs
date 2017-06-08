using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples
{
    public class Person
    {
        public string ID { get; set; }

        public LinkedList<HeartRate> HeartRates { get; } = new LinkedList<HeartRate>();

        public IDictionary<DateTime, int> DailyCalories { get; } = new Dictionary<DateTime, int>();

        public IList<Sleep> SleepPeriods { get; } = new List<Sleep>();

        public IList<Tuple<DateTime, decimal>> BodyTemps { get; } = new List<Tuple<DateTime, decimal>>();
    }


    public struct HeartRate
    {
        public ushort BPM { get; }

        public DateTime Timestamp { get; }

        public HeartRate(ushort bpm, DateTime timestamp)
        {
            BPM = bpm;
            Timestamp = timestamp;
        }

        public override string ToString() => $"{Timestamp:s}: {BPM} bpm";
    }


    public struct Sleep
    {
        public DateTime StartTime { get; }

        public DateTime EndTime { get; }

        public TimeSpan Duration { get => EndTime - StartTime; }

        public Sleep(DateTime start, DateTime end)
        {
            StartTime = start;
            EndTime = end;
        }
    }
}
