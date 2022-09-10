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


using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.Common.Utils;
using FitsRatingTool.GuiApp.UI.Progress;
using ReactiveUI;
using System;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.JobRunner
{
    public class JobResult
    {
        public Exception? Error { get; }

        public string? ErrorMessage { get; }

        public bool Successful => Error == null && ErrorMessage == null;

        public int NumberOfFiles { get; }

        public int LoadedFiles { get; }

        public int ExportedFiles { get; }

        public string? LogFile { get; }

        public JobResult(int numFiles, int loadedFiles, int exportedFiles, string? logFile)
        {
            Error = null;
            ErrorMessage = null;
            NumberOfFiles = numFiles;
            LoadedFiles = loadedFiles;
            ExportedFiles = exportedFiles;
            LogFile = logFile;
        }

        public JobResult(string errorMessage, Exception? exception = null)
        {
            Error = exception;
            ErrorMessage = errorMessage;
        }

        public JobResult(Exception error)
        {
            Error = error;
            ErrorMessage = null;
        }
    }

    public interface IJobRunnerProgressViewModel : IProgressViewModel<JobResult, JobResult, BatchEvaluationProgressTracker.ProgressState>
    {
        public interface IFactory
        {
            IJobRunnerProgressViewModel Create(string jobConfigFile, string path);
        }

        int NumberOfFiles { get; }

        int LoadedFiles { get; }

        bool IsLoading { get; }

        bool IsEvaluating { get; }

        float ProgressValue { get; }

        float SpeedValue { get; }

        Interaction<ConfirmationEventArgs, ConfirmationEventArgs.Result> ExporterConfirmationDialog { get; }
    }
}
