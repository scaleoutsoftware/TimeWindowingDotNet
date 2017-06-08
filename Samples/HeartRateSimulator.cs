using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Samples
{
    class HeartRateSimulator : IEnumerable<HeartRate>
    {
        DateTime _startTime;
        DateTime _endTime;
        TimeSpan _frequency;

        
        
        public HeartRateSimulator(DateTime startTime, DateTime endTime, TimeSpan frequency)
        {
            _startTime = startTime;
            _endTime = endTime;
            _frequency = frequency;
        }


        public IEnumerator<HeartRate> GetEnumerator()
        {
            DateTime current = _startTime;
            Random rand = new Random(42);

            bool inExercisePeriod = false;
            DateTime exerciseStart = current.AddDays(2).Date.AddHours(12) + TimeSpan.FromMinutes(rand.Next(-60, 60));
            DateTime exerciseEnd = exerciseStart + TimeSpan.FromMinutes(rand.Next(20, 50));

            while (current < _endTime)
            {
                if (current >= exerciseStart && current <= exerciseEnd)
                    inExercisePeriod = true;
                else if (inExercisePeriod)
                {
                    // just exited exercise period. Set next one.
                    exerciseStart = current.AddDays(2).Date.AddHours(12) + TimeSpan.FromMinutes(rand.Next(-60, 60));
                    exerciseEnd = exerciseStart + TimeSpan.FromMinutes(rand.Next(20, 50));
                    inExercisePeriod = false;
                }
                else
                    inExercisePeriod = false;

                ushort bpm;
                if (inExercisePeriod)
                    bpm = (ushort)(120 + rand.Next(-10, 10));
                else
                    bpm = (ushort)(74 + rand.Next(-10, 10));

                yield return new HeartRate(bpm, current);

                current += _frequency;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
