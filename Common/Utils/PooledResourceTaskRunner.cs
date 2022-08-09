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

using System.Threading.Tasks.Dataflow;

namespace FitsRatingTool.Common.Utils
{
    public static class PooledResourceTaskRunner
    {
        public static async Task RunAsync<TResource>(IEnumerable<TResource> resources, IEnumerable<Func<TResource, Func<CancellationToken, Task>>> taskGenerator, CancellationToken cancellationToken = default)
        {
            await RunAsync(resources, taskGenerator, (ct, task) => task.Invoke(ct), cancellationToken);
        }

        private static async Task RunAsync<TResource, TFunc>(IEnumerable<TResource> resources, IEnumerable<Func<TResource, TFunc>> taskGenerator, Func<CancellationToken, TFunc, Task> taskRunner, CancellationToken cancellationToken = default)
        {
            BufferBlock<TResource> pool = new();
            foreach (TResource res in resources)
            {
                pool.Post(res);
            }

            List<Task> scheduledTasks = new();
            Task<TResource>? poolTask = null;

            var enumerator = taskGenerator.GetEnumerator();

            bool tasksExhausted = !enumerator.MoveNext();

            if (tasksExhausted)
            {
                return;
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!tasksExhausted)
                {
                    poolTask ??= pool.ReceiveAsync(cancellationToken);
                }

                Task<Task>? anyTask1 = null;
                Task<Task<Task>>? anyTask2 = null;
                Task<Task<TResource>>? anyTask3 = null;

                Task<Task>? anyScheduledTask = null;

                if (poolTask == null && scheduledTasks.Count == 0)
                {
                    break;
                }
                else if (poolTask != null && scheduledTasks.Count > 0)
                {
                    anyScheduledTask = Task.WhenAny(scheduledTasks);
                    anyTask1 = Task.WhenAny(poolTask, anyScheduledTask);
                }
                else if (poolTask != null)
                {
                    anyTask3 = Task.WhenAny(poolTask);
                }
                else
                {
                    anyScheduledTask = Task.WhenAny(scheduledTasks);
                    anyTask2 = Task.WhenAny(anyScheduledTask);
                }

                Task task;
                if (anyTask1 != null)
                {
                    task = await anyTask1;
                }
                else if (anyTask2 != null)
                {
                    task = await anyTask2;
                }
                else
                {
                    task = await anyTask3!;
                }

                if (task == poolTask)
                {
                    var res = await poolTask;
                    poolTask = null;

                    if (!tasksExhausted)
                    {
                        var newTask = enumerator.Current.Invoke(res);

                        async Task RunAsync()
                        {
                            try
                            {
                                await taskRunner.Invoke(cancellationToken, newTask);
                            }
                            finally
                            {
                                pool.Post(res);
                            }
                        }

                        scheduledTasks.Add(RunAsync());
                    }
                    else
                    {
                        pool.Post(res);
                    }

                    tasksExhausted = !enumerator.MoveNext();
                }
                else if (task == anyScheduledTask)
                {
                    var scheduledTask = await anyScheduledTask;
                    scheduledTasks.Remove(scheduledTask);
                }
            }
        }
    }
}
