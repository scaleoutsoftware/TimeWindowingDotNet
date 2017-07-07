/* 
 * © Copyright 2017 by ScaleOut Software, Inc.
 *
 * LICENSE AND DISCLAIMER
 * ----------------------
 * This material contains sample programming source code ("Sample Code").
 * ScaleOut Software, Inc. (SSI) grants you a nonexclusive license to compile, 
 * link, run, display, reproduce, and prepare derivative works of 
 * this Sample Code.  The Sample Code has not been thoroughly
 * tested under all conditions.  SSI, therefore, does not guarantee
 * or imply its reliability, serviceability, or function. SSI
 * provides no support services for the Sample Code.
 *
 * All Sample Code contained herein is provided to you "AS IS" without
 * any warranties of any kind. THE IMPLIED WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGMENT ARE EXPRESSLY
 * DISCLAIMED.  SOME JURISDICTIONS DO NOT ALLOW THE EXCLUSION OF IMPLIED
 * WARRANTIES, SO THE ABOVE EXCLUSIONS MAY NOT APPLY TO YOU.  IN NO 
 * EVENT WILL SSI BE LIABLE TO ANY PARTY FOR ANY DIRECT, INDIRECT, 
 * SPECIAL OR OTHER CONSEQUENTIAL DAMAGES FOR ANY USE OF THE SAMPLE CODE
 * INCLUDING, WITHOUT LIMITATION, ANY LOST PROFITS, BUSINESS 
 * INTERRUPTION, LOSS OF PROGRAMS OR OTHER DATA ON YOUR INFORMATION
 * HANDLING SYSTEM OR OTHERWISE, EVEN IF WE ARE EXPRESSLY ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGES.
 */

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
