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

using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.Progress
{
    public class ProgressSynchronizationContext
    {
        internal readonly Progress<object> progress;

        public ProgressSynchronizationContext()
        {
            progress = new Progress<object>();
        }
    }

    public enum ResultStatus
    {
        NotRunning,
        Completed,
        Cancelled
    }

    public struct Result<TResult>
    {
        public ResultStatus Status { get; }
        public TResult? Value { get; }

        public Result(ResultStatus status, TResult? value)
        {
            Status = status;
            Value = value;
        }
    }

    public interface IProgressViewModel<TTaskResult, TResult, TProgressValue>
    {
        /// <summary>
        /// Cancels the task.
        /// </summary>
        public ReactiveCommand<Unit, Unit> Cancel { get; }

        /// <summary>
        /// Runs the task, if the task hasn't already finished, and returns the result.
        /// This command is usually executed and subscribed to by a dialog window.
        /// </summary>
        public ReactiveCommand<Unit, Result<TResult>> Run { get; }

        /// <summary>
        /// Returns whether the task is being cancelled.
        /// </summary>
        public bool IsCancelling { get; }

        /// <summary>
        /// Sets or returns the <see cref="IProgress{TProgressValue}"/> instance.
        /// Must be set before the task is executed via <see cref="Run"/>.
        /// Can be used e.g. with <see cref="Progress{TProgressValue}"/> to listen to
        /// the progress being made during the task execution.
        /// </summary>
        public IProgress<TProgressValue>? Progress { get; set; }

        /// <summary>
        /// Hooks the internal task. This allows doing work before or after
        /// the internal task. The internal task can be hooked multiple times.
        /// </summary>
        /// <param name="hook"></param>
        void HookInternalTask(Func<Func<Task<Result<TTaskResult>>>, Task<Result<TTaskResult>>> hook);

        /// <summary>
        /// Hooks the result task. This allows returning a value before the
        /// task is run or to change the result value. The result task can be hooked
        /// multiple times.
        /// </summary>
        /// <param name="hook"></param>
        void HookResultTask(Func<Func<Task<Result<TResult>>>, Task<Result<TResult>>> hook);

        /// <summary>
        /// Sets <see cref="IsCancelling"/> to true.
        /// </summary>
        void SetCancelling();

        /// <summary>
        /// Helper method to create <see cref="Result"/>.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Result<TTaskResult> CreateInternalResult(ResultStatus status, TTaskResult? result);

        /// <summary>
        /// Helper method to create a completed <see cref="Result"/>.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public Result<TTaskResult> CreateInternalCompletion(TTaskResult? result);

        /// <summary>
        /// Helper method to create a completed <see cref="Result"/>.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public Result<TResult> CreateCompletion(TResult? result);

        /// <summary>
        /// Helper method to create a cancelled <see cref="Result"/>.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public Result<TTaskResult> CreateInternalCancellation(TTaskResult? result);

        /// <summary>
        /// Helper method to create a cancelled <see cref="Result"/>.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public Result<TResult> CreateCancellation(TResult? result);
    }
}
