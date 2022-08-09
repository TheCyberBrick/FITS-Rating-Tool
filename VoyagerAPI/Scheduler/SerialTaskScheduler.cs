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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VoyagerAPI.Scheduler
{
    using CancellableTaskFunc = Func<CancellationToken, Task>;

    public class SerialTaskScheduler
    {
        private List<Task<CancellableTaskFunc?>> scheduledProviderTasks = new();
        private Dictionary<Task<CancellableTaskFunc?>, Tuple<TaskProvider, CancellationToken>> providers = new();

        public bool Empty
        {
            get
            {
                return scheduledProviderTasks.Count == 0 && providers.Count == 0;
            }
        }

        public bool Schedule(TaskProvider provider, CancellationToken cancellationToken = default(CancellationToken))
        {
            Task<CancellableTaskFunc?> providerTask = provider.ProvideAsync(cancellationToken);
            if (providerTask != null)
            {
                scheduledProviderTasks.Add(providerTask);
                providers.Add(providerTask, Tuple.Create(provider, cancellationToken));
                return true;
            }
            return false;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (scheduledProviderTasks.Count > 0)
            {
                Task<CancellableTaskFunc?> providerTask = await Task.WhenAny(scheduledProviderTasks);
                scheduledProviderTasks.Remove(providerTask);

                bool reschedule = true;

                try
                {
                    CancellableTaskFunc? task = null;
                    try
                    {
                        task = await providerTask;
                    }
                    catch (Exception)
                    {
                        // If anything goes wrong in the condition task then
                        // we cannot know whether the provider should be rescheduled,
                        // so we need to remove it from the schedule
                        reschedule = false;
                        throw;
                    }

                    if (task != null)
                    {
                        await task.Invoke(cancellationToken);
                    }
                    else
                    {
                        // No work task returned => don't reschedule
                        reschedule = false;
                    }
                }
                finally
                {
                    Tuple<TaskProvider, CancellationToken> provider = providers[providerTask];
                    providers.Remove(providerTask);

                    if (reschedule)
                    {
                        // Reusing the cancellation token from Schedule(...) is fine because
                        // a cancelled provider will never be rescheduled
                        Schedule(provider.Item1, provider.Item2);
                    }
                }
            }
        }
    }
}
