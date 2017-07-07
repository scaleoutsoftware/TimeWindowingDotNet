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

namespace Scaleout.Client.Streaming
{
    /// <summary>
    /// Supports iteration over a collection of elements that fall within a time window.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public interface ITimeWindow<TElement> : IEnumerable<TElement>, ICollection<TElement>
    {
        /// <summary>
        /// Start time of the window.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// End Time of the window.
        /// </summary>
        DateTime EndTime { get; }
    }

}
