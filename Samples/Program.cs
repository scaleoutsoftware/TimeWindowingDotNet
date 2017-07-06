using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scaleout.Client.Streaming;
using Scaleout.Client.Streaming.Linq;

namespace Samples
{
    class Program
    {
        static Dictionary<string, Person> _people = new Dictionary<string, Person>();
        static readonly DateTime start = DateTime.Now - TimeSpan.FromDays(31);
        static readonly DateTime end = DateTime.Now - TimeSpan.FromMinutes(1);
        static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        static void Main(string[] args)
        {
            // add a person with some sample heartrate data, one measurement per minute:
            var person = new Person { ID = "User123" };
            foreach (var hr in new HeartRateSimulator(start, end, frequency: OneMinute))
                person.HeartRates.AddLast(hr);

            _people.Add(person.ID, person);

            // Simulate a new heart rate event:
            HandleIncomingHeartRate("User123", new HeartRate(75, DateTime.Now));

            // Use session windowing to find work-out sessions:
            // AnalyzeExercise("User123");
        }


        static void HandleIncomingHeartRate(string userID, HeartRate hr)
        {
            var person = _people[userID];

            // Transform raw HeartRates list to 3-minute sliding windows that advances every 1 minute:
            var slidingHeartrates = new SlidingWindowTransform<HeartRate>(person.HeartRates, 
                                                                         timestampSelector: h => h.Timestamp,
                                                                         windowDuration:    TimeSpan.FromMinutes(3),
                                                                         every:             TimeSpan.FromMinutes(1),
                                                                         startTime:         DateTime.Now.Date - TimeSpan.FromDays(31));

            // Add incoming heartrate via the transform instead of adding directly to person.HeartRates. 
            // This enforces eviction policies while modifying underlying person.HeartRates collection.
            slidingHeartrates.Add(hr);

            // Look at moving average for last 10 minutes:
            foreach (var window in slidingHeartrates.Skip(slidingHeartrates.Count() - 10))
            {
                Console.WriteLine($"{window.StartTime:t} - {window.EndTime:t}: {window.Average(h => h.BPM):N1}");
                foreach (var h in window)
                    Console.WriteLine(hr.BPM);
            }

            // If we were writing the person back to SOSS or some other datastore with, that would happen here.
        }


        static void AnalyzeExercise(string userID)
        {
            var person = _people[userID];

            // Use a Where() operation and the ToSessionWindows() extension method to try to 
            // determine when a person is engaged in sustained exercise, breaking up those
            // sessions into discrete time windows:
            var exerciseSessions = person.HeartRates.Where(hr => hr.BPM > 110) // strip out low heart rates.
                                         .ToSessionWindows(timestampSelector: h => h.Timestamp,
                                                           idleThreshold:     TimeSpan.FromHours(1));

            // find all work-out windows whose duration exceeds 15 minutes.
            foreach (var window in exerciseSessions.Where(w => (w.EndTime - w.StartTime) > TimeSpan.FromMinutes(15)))
            {
                Console.WriteLine($"{window.StartTime:g} - {window.EndTime:t}: {window.Average(h => h.BPM):N1}");
            }

        }
    }
}
