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
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.Progress.ViewModels
{
    public abstract class CallbackProgressViewModel<TCallback, TTaskResult, TResult, TProgressValue> : ViewModelBase, IProgressViewModel<TTaskResult, TResult, TProgressValue>
    {
        /// <summary>
        /// Cancels the task.
        /// </summary>
        public ReactiveCommand<Unit, Unit> Cancel { get; }

        /// <summary>
        /// Runs the task or returns the result, if the task has already finished.
        /// This command is usually executed and subscribed to by a dialog window.
        /// </summary>
        public ReactiveCommand<Unit, Result<TResult>> Run { get; }

        /// <summary>
        /// Returns whether the task is being cancelled.
        /// </summary>
        public bool IsCancelling { get; private set; } = false;

        /// <summary>
        /// Sets or returns the <see cref="IProgress{TProgressValue}"/> instance.
        /// Can be used e.g. with <see cref="Progress{TProgressValue}"/> to listen to
        /// the progress being made during the task execution.
        /// </summary>
        public IProgress<TProgressValue>? Progress { get; set; }


        private readonly ProgressSynchronizationContext synchronizationContext;

        private readonly TaskCompletionSource<Result<TTaskResult>> tcs = new();

        private readonly AsyncSemaphore initSemaphore = new(1);
        private readonly AsyncSemaphore runSemaphore = new(1);

        private Func<TCallback, Func<Task<Result<TTaskResult>>>> internalTaskFunc;
        private Func<Task<Result<TResult>>> resultTaskFunc;

        private Task<TTaskResult>? task;


        protected CallbackProgressViewModel()
        {
            synchronizationContext = null!; // Won't ever be used
            internalTaskFunc = null!; // Won't ever be used
            resultTaskFunc = null!; // Won't ever be used
            Run = ReactiveCommand.Create(() => new Result<TResult>(ResultStatus.NotRunning, default));
            Cancel = ReactiveCommand.Create(() => { });
        }



        public delegate Func<Task<TTaskResult>> AsyncTaskFunc(TCallback callback);
        protected CallbackProgressViewModel(AsyncTaskFunc taskFunc, ProgressSynchronizationContext? synchronizationContext = null) : this(false, taskFunc, synchronizationContext) { }


        protected CallbackProgressViewModel(ProgressSynchronizationContext? synchronizationContext = null) : this(true, null, synchronizationContext) { }


        private CallbackProgressViewModel(bool allowNullTaskFunc, AsyncTaskFunc? taskFunc, ProgressSynchronizationContext? synchronizationContext = null)
        {
            if (!allowNullTaskFunc && taskFunc == null)
            {
                throw new ArgumentNullException(nameof(taskFunc));
            }
            this.synchronizationContext = synchronizationContext ?? new ProgressSynchronizationContext();
            var taskFuncInitializer = CreateTaskFuncInitializer(taskFunc, this.synchronizationContext);
            if (taskFuncInitializer == null)
            {
                throw new ArgumentNullException(nameof(taskFunc));
            }
            internalTaskFunc = callback => async () =>
            {
                this.synchronizationContext.progress.ProgressChanged += OnProgress;
                try
                {
                    using (await initSemaphore.EnterAsync())
                    {
                        if (task == null)
                        {
                            taskFunc = await taskFuncInitializer.Invoke();
                            if (taskFunc == null)
                            {
                                throw new ArgumentNullException(nameof(taskFunc));
                            }
                            task = taskFunc.Invoke(callback).Invoke();
                        }
                    }
                    await task;
                    return await tcs.Task;
                }
                finally
                {
                    this.synchronizationContext.progress.ProgressChanged -= OnProgress;
                }
            };
            resultTaskFunc = async () =>
            {
                using (await runSemaphore.EnterAsync())
                {
                    return await MapResultAsync(await internalTaskFunc.Invoke(await CreateCallbackAsync()).Invoke());
                }
            };
            Run = ReactiveCommand.CreateFromTask(async () =>
            {
                return await resultTaskFunc.Invoke();
            });
            Cancel = ReactiveCommand.CreateFromTask(async () =>
            {
                SetCancelling();
                await tcs.Task;
            });
        }

        /// <summary>
        /// Returns a function that creates the <see cref="AsyncTaskFunc"/>, i.e. the internal task, to be executed.
        /// By default, this method just forwards the <see cref="AsyncTaskFunc"/> from the constructors.
        /// </summary>
        /// <param name="taskFunc"></param>
        /// <param name="synchronizationContext"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual Func<Task<AsyncTaskFunc>> CreateTaskFuncInitializer(AsyncTaskFunc? taskFunc, ProgressSynchronizationContext synchronizationContext)
        {
            if (taskFunc == null)
            {
                throw new ArgumentNullException(nameof(taskFunc));
            }
            return () => Task.FromResult(taskFunc);
        }

        /// <inheritdoc/>
        public void SetCancelling()
        {
            IsCancelling = true;
            OnCancelling();
        }

        /// <inheritdoc/>
        public void HookInternalTask(Func<Func<Task<Result<TTaskResult>>>, Task<Result<TTaskResult>>> hook)
        {
            if (internalTaskFunc != null)
            {
                lock (this)
                {
                    var currentTaskFunc = internalTaskFunc;
                    internalTaskFunc = callback => () => hook.Invoke(currentTaskFunc.Invoke(callback));
                }
            }
        }

        /// <inheritdoc/>
        public void HookResultTask(Func<Func<Task<Result<TResult>>>, Task<Result<TResult>>> hook)
        {
            if (resultTaskFunc != null)
            {
                lock (this)
                {
                    var currentTaskFunc = resultTaskFunc;
                    resultTaskFunc = () => hook.Invoke(currentTaskFunc);
                }
            }
        }

        /// <inheritdoc/>
        public Result<TTaskResult> CreateInternalResult(ResultStatus status, TTaskResult? result)
        {
            return new Result<TTaskResult>(status, result);
        }

        /// <inheritdoc/>
        public Result<TResult> CreateResult(ResultStatus status, TResult? result)
        {
            return new Result<TResult>(status, result);
        }

        /// <inheritdoc/>
        public Result<TTaskResult> CreateInternalCompletion(TTaskResult? result)
        {
            return new Result<TTaskResult>(ResultStatus.Completed, result);
        }

        /// <inheritdoc/>
        public Result<TResult> CreateCompletion(TResult? result)
        {
            return new Result<TResult>(ResultStatus.Completed, result);
        }

        /// <inheritdoc/>
        public Result<TTaskResult> CreateInternalCancellation(TTaskResult? result)
        {
            return new Result<TTaskResult>(ResultStatus.Cancelled, result);
        }

        /// <inheritdoc/>
        public Result<TResult> CreateCancellation(TResult? result)
        {
            return new Result<TResult>(ResultStatus.Cancelled, result);
        }

        /// <summary>
        /// Must be called upon completion and cancellation. See <see cref="CreateCallbackAsync"/>.
        /// </summary>
        /// <param name="result"></param>
        protected void Finish(Result<TTaskResult> result)
        {
            tcs.TrySetResult(result);
        }

        /// <summary>
        /// Should be called whenever the task has progressed. See <see cref="CreateCallbackAsync"/>.
        /// </summary>
        /// <param name="value"></param>
        protected void ReportProgress(TProgressValue value)
        {
            Progress?.Report(value);
            (synchronizationContext.progress as IProgress<object>).Report(value!);
        }

        /// <summary>
        /// Called when <see cref="IsCancelling"/> is set to true.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        private void OnProgress(object? sender, object value)
        {
            OnProgressChanged((TProgressValue)value);
        }

        /// <summary>
        /// Called when <see cref="IsCancelling"/> is set to true.
        /// </summary>
        protected virtual void OnCancelling()
        {
        }

        /// <summary>
        /// Called when new progress is reported through <see cref="ReportProgress(TProgressValue)"/> by the callback.
        /// The <see cref="System.Threading.SynchronizationContext"/> is determined by the <see cref="ProgressSynchronizationContext"/>.
        /// </summary>
        /// <param name="value"></param>
        protected virtual void OnProgressChanged(TProgressValue value)
        {
        }

        /// <summary>
        /// Returns the callback.
        /// Must call <see cref="Finish"/> on completion and cancellation.
        /// Should check <see cref="IsCancelling"/> for cancellation.
        /// Should call <see cref="ReportProgress(TProgressValue)"/> whenever the task has progressed.
        /// </summary>
        /// <returns></returns>
        protected abstract Task<TCallback> CreateCallbackAsync();

        /// <summary>
        /// Maps the task result to the actual result.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual async Task<Result<TResult>> MapResultAsync(Result<TTaskResult> result)
        {
            return new Result<TResult>(result.Status, await MapResultAsync(result.Value));
        }

        /// <summary>
        /// Maps the task result value to the actual result value.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected abstract Task<TResult?> MapResultAsync(TTaskResult? result);
    }
}