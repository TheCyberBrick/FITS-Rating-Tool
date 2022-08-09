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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace VoyagerAPI.Scheduler
{
    using CancellableTaskFunc = Func<CancellationToken, Task>;

    public class TaskProvider
    {
        public interface ISignal
        {
            void Notify();
        }

        // https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-1-asyncmanualresetevent/
        private class AsyncManualResetEvent : ISignal
        {
            private volatile TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            public Task WaitAsync()
            {
                return tcs.Task;
            }

            public void Notify()
            {
                tcs.TrySetResult(true);
            }

            public void Reset()
            {
                while (true)
                {
                    var tcs = this.tcs;
                    if (!tcs.Task.IsCompleted || Interlocked.CompareExchange(ref this.tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                    {
                        return;
                    }
                }
            }
        }

        public static TaskProvider Create(Func<Task> task, CancellableTaskFunc condition)
        {
            return Create(_ => task.Invoke(), condition);
        }

        public static TaskProvider Create(CancellableTaskFunc task, CancellableTaskFunc condition)
        {
            return new TaskProvider(task, condition);
        }

        public static TaskProvider Create(Func<Task> task, int millisecondsDelay)
        {
            return Create(_ => task.Invoke(), millisecondsDelay);
        }

        public static TaskProvider Create(CancellableTaskFunc task, int millisecondsDelay)
        {
            return Create(task, (cancellationToken) => Task.Delay(millisecondsDelay, cancellationToken));
        }

        public static TaskProvider Create(Func<Task> task, out ISignal signal)
        {
            return Create(_ => task.Invoke(), out signal);
        }

        public static TaskProvider Create(CancellableTaskFunc task, out ISignal signal)
        {
            var manualResetEvent = new AsyncManualResetEvent();
            signal = manualResetEvent;
            return Create(task, async (cancellationToken) =>
            {
                try
                {
                    using (cancellationToken.Register(() => manualResetEvent.Notify()))
                    {
                        await manualResetEvent.WaitAsync();
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    manualResetEvent.Reset();
                }
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003")]
        private static async Task<CancellableTaskFunc?> WaitForConditionAsync(CancellableTaskFunc task, Task condition)
        {
            await condition;
            return task;
        }

        private readonly Func<CancellationToken, Task<CancellableTaskFunc?>> func;

        public bool Valid
        {
            get;
            private set;
        } = true;

        private TaskProvider(CancellableTaskFunc task, CancellableTaskFunc condition)
        {
            func = (cancellationToken) =>
            {
                Task conditionTask = condition.Invoke(cancellationToken);
                if (conditionTask != null)
                {
                    return WaitForConditionAsync(task, conditionTask);
                }
                return Task.FromResult<CancellableTaskFunc?>(null);
            };
        }

        public async Task<CancellableTaskFunc?> ProvideAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Valid)
            {
                Task<CancellableTaskFunc?> task = func.Invoke(cancellationToken);
                if (task != null)
                {
                    return await task;
                }
            }
            return null;
        }

        public void Invalidate()
        {
            Valid = false;
        }
    }
}
