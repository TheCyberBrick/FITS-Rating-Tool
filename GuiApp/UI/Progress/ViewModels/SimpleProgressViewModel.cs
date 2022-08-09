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
using System.Reactive;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.UI.Progress.ViewModels
{
    public abstract class SimpleProgressViewModel<TResult, TProgressValue> : CallbackProgressViewModel<Unit, TResult, TResult, TProgressValue>
    {
        // Don't need this
        private SimpleProgressViewModel(AsyncTaskFunc taskFunc, ProgressSynchronizationContext? synchronizationContext = null) : base(taskFunc, synchronizationContext) { }

        protected SimpleProgressViewModel(ProgressSynchronizationContext? synchronizationContext = null) : base(synchronizationContext)
        {
        }

        protected abstract Func<Task<Result<TResult>>> CreateTask(ProgressSynchronizationContext synchronizationContext);

        protected sealed override Func<Task<AsyncTaskFunc>> CreateTaskFuncInitializer(AsyncTaskFunc? taskFunc, ProgressSynchronizationContext synchronizationContext)
        {
            return () =>
            {
                taskFunc = callback => async () =>
                {
                    var result = await CreateTask(synchronizationContext).Invoke();
                    Finish(result);
                    return result.Value!;
                };
                return Task.FromResult(taskFunc);
            };
        }

        protected sealed override Task<Unit> CreateCallbackAsync()
        {
            return Task.FromResult(Unit.Default);
        }

        protected override Task<TResult?> MapResultAsync(TResult? result)
        {
            return Task.FromResult(result);
        }
    }
}
