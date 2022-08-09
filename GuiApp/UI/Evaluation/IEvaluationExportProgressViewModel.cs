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

using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Progress;
using System;

namespace FitsRatingTool.GuiApp.UI.Evaluation
{
    public struct EvaluationExportProgress
    {
        public int numberOfFiles;
        public int currentFile;
        public string currentFilePath;
    }

    public class ExportResult
    {
        public Exception? Error { get; }

        public string? ErrorMessage { get; }

        public bool Successful => Error == null && ErrorMessage == null;

        public int NumFiles { get; }

        public int NumExported { get; }

        public ExportResult(int numFiles, int numExported)
        {
            Error = null;
            ErrorMessage = null;
            NumFiles = numFiles;
            NumExported = numExported;
        }

        public ExportResult(string errorMessage)
        {
            Error = null;
            ErrorMessage = errorMessage;
        }

        public ExportResult(Exception error)
        {
            Error = error;
            ErrorMessage = null;
        }
    }

    public interface IEvaluationExportProgressViewModel : IProgressViewModel<ExportResult, ExportResult, EvaluationExportProgress>
    {
        public interface IFactory
        {
            IEvaluationExportProgressViewModel Create(IExporterConfiguratorManager.IExporterConfiguratorViewModel exporterConfigurator);
        }

        public int NumberOfFiles { get; }

        public int CurrentFile { get; }

        public string CurrentFilePath { get; }

        public string CurrentFileName { get; }

        public float ProgressValue { get; }
    }
}
