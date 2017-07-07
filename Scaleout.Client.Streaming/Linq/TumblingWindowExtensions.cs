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
using System.Text;

namespace Scaleout.Client.Streaming.Linq
{
    public static class TumblingWindowExtensions
    {
        /// <summary>
        /// Transforms a collection into an enumerable collection of fixed-time windows. The source collection
        /// must be sorted chronologically.
        /// </summary>
        /// <typeparam name="TSource">The type of objects in the source collection.</typeparam>
        /// <param name="source">The sequence of elements to transform.</param>
        /// <param name="timestampSelector">A function to extract a timestamp from an element.</param>
        /// <param name="startTime">Start time (inclusive) of the first window.</param>
        /// <param name="endTime">End time (exclusive) for the last window.</param>
        /// <param name="windowDuration">
        /// Duration of each time window. This is a maximum value that may be shortened for the last window 
        /// in the returned sequence (see remarks).
        /// </param>
        /// <returns>An enumerable set of <see cref="ITimeWindow{TElement}"/> collections.</returns>
        /// <remarks>
        /// <para>
        /// The method returns a sequence of  <see cref="ITimeWindow{TElement}"/> elements, where each window is 
        /// an enumerable collection of elements from the source collection whose timestamp falls within its specified range. 
        /// The <see cref="ITimeWindow{TElement}.StartTime"/> is inclusive and the <see cref="ITimeWindow{TElement}.EndTime"/> is exclusive.
        /// </para>
        /// <para>
        /// The last window returned by this method may have a shorter duration than what is specified
        /// by the <paramref name="windowDuration"/> argument. This happens when the duration would cause 
        /// the window to extend beyond the specified <paramref name="endTime"/>.
        /// </para>
        /// </remarks>
        public static IEnumerable<ITimeWindow<TSource>> ToTumblingWindows<TSource>(this IEnumerable<TSource> source, Func<TSource, DateTime> timestampSelector, DateTime startTime, DateTime endTime, TimeSpan windowDuration)
        {
            return SlidingWindowExtensions.ToSlidingWindows(source, timestampSelector, startTime, endTime, windowDuration, windowDuration);
        }
    }
}
