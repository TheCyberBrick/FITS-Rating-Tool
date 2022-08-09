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

using Avalonia.Collections;
using FitsRatingTool.Common.Models.Evaluation;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Evaluation;

namespace FitsRatingTool.GuiApp.UI.JobConfigurator
{
    public interface IJobConfiguratorViewModel
    {
        public interface IFactory
        {
            IJobConfiguratorViewModel Create();
        }



        IEvaluationFormulaViewModel EvaluationFormula { get; }



        IJobGroupingConfiguratorViewModel GroupingConfigurator { get; }

        bool GroupingKeysRequired { get; set; }

        bool IsFilteredByGrouping { get; }

        public interface IGroupingFilterViewModel
        {
            ReactiveCommand<Unit, Unit> Remove { get; }

            string? Key { get; set; }

            bool IsKeyValid { get; }

            string? Pattern { get; set; }

            bool IsPatternValid { get; }
        }

        AvaloniaList<IGroupingFilterViewModel> GroupingFilters { get; }

        ReactiveCommand<Unit, Unit> AddNewGroupingFilter { get; }

        void AddGroupingFilter(string? key, string? pattern);

        void ClearGroupingFilters();



        int ParallelIO { get; set; }

        int ParallelTasks { get; set; }



        long MaxImageSize { get; set; }

        int MaxImageWidth { get; set; }

        int MaxImageHeight { get; set; }



        string OutputLogsPath { get; set; }

        ReactiveCommand<Unit, Unit> SelectOutputLogsPathWithOpenFolderDialog { get; }

        Interaction<Unit, string> SelectOutputLogsPathOpenFolderDialog { get; }



        string CachePath { get; set; }

        ReactiveCommand<Unit, Unit> SelectCachePathWithOpenFolderDialog { get; }

        Interaction<Unit, string> SelectCachePathOpenFolderDialog { get; }



        IEvaluationExporterConfiguratorViewModel EvaluationExporterConfigurator { get; }



        bool TryLoadJobConfig(IReadOnlyJobConfig config);



        IReadOnlyJobConfig JobConfig { get; }



        ReactiveCommand<Unit, Unit> SaveJobConfigWithSaveFileDialog { get; }

        Interaction<Unit, string> SaveJobConfigSaveFileDialog { get; }

        public class SaveResult
        {
            public string File { get; }

            public string Config { get; }

            public Exception? Error { get; }

            public bool Successful => Error == null;

            public SaveResult(string file, string config, Exception? exception = null)
            {
                File = file;
                Config = config;
                Error = exception;
            }
        }

        Interaction<SaveResult, Unit> SaveJobConfigResultDialog { get; }

    }
}
