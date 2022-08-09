/*
    FITS Rating Tool
    Copyright (C) 2022 TheCyberBrick
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Microsoft.VisualStudio.Threading;

namespace FitsRatingTool.Common.Utils
{
    public static class BufferedOrderedConverter
    {
        public static async Task RunAsync<TIn, TOut>(int buffer, IEnumerable<TIn> input, Func<TIn, CancellationToken, Task<TOut>> converter, Action<TOut>? consumer, CancellationToken cancellationToken = default)
        {
            var tasks = new Dictionary<Task<TOut>, int>();

            var enumerator = input.GetEnumerator();

            bool hasItem = enumerator.MoveNext();

            if (!hasItem)
            {
                return;
            }

            int queuedIndex = 0;
            void queueNext()
            {
                var item = enumerator.Current;
                tasks.Add(converter.Invoke(item, cancellationToken), queuedIndex++);
                hasItem = enumerator.MoveNext();
            }

            for (int i = 0; i < buffer && hasItem; ++i)
            {
                cancellationToken.ThrowIfCancellationRequested();

                queueNext();
            }

            var outQueue = new Dictionary<int, TOut>();

            int nextIndex = 0;
            while (tasks.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var task = await Task.WhenAny(tasks.Keys);
                tasks.Remove(task, out var index);

                var item = await task;

                if (item != null)
                {
                    outQueue.Add(index, item);

                    // Dequeue in order
                    while (outQueue.TryGetValue(nextIndex, out var dequeuedItem))
                    {
                        outQueue.Remove(nextIndex);

                        // Queue next item, if available
                        if (hasItem) queueNext();

                        consumer?.Invoke(dequeuedItem);

                        ++nextIndex;
                    }
                }
            }
        }
    }
}
