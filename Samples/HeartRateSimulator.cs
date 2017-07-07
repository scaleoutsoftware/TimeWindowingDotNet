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
