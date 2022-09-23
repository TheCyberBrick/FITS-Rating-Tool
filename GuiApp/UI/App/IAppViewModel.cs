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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using FitsRatingTool.GuiApp.UI.Evaluation;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.JobConfigurator;
using FitsRatingTool.GuiApp.UI.JobRunner;
using FitsRatingTool.GuiApp.UI.FileTable;
using FitsRatingTool.GuiApp.UI.AppConfig;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using Avalonia.Collections;

namespace FitsRatingTool.GuiApp.UI.App
{
    public interface IAppViewModel
    {
        #region +++ Misc +++
        ReactiveCommand<Unit, IFileTableViewModel> ShowFileTable { get; }

        ReactiveCommand<Unit, Unit> HideFileTable { get; }

        ReactiveCommand<Unit, Unit> Exit { get; }

        ReactiveCommand<Unit, Unit> ShowAboutDialog { get; }

        ReactiveCommand<Unit, IAppConfigViewModel> ShowSettingsDialog { get; }
        #endregion

        #region +++ Images +++
        IFitsImageMultiViewerViewModel MultiViewer { get; }

        float ThumbnailScale { get; set; }

        ReactiveCommand<float, Unit> IncreaseThumbnailScale { get; }

        ReactiveCommand<float, Unit> DecreaseThumbnailScale { get; }

        AvaloniaList<IAppImageItemViewModel> Items { get; }

        IAppImageItemViewModel? SelectedItem { get; set; }

        ReactiveCommand<string, Unit> LoadImage { get; }

        ReactiveCommand<Unit, Unit> UnloadAllImages { get; }

        ReactiveCommand<string, Unit> UnloadImage { get; }

        ReactiveCommand<Unit, Unit> LoadImagesWithOpenFileDialog { get; }

        Interaction<Unit, IEnumerable<string>> LoadImagesOpenFileDialog { get; }

        ReactiveCommand<IEnumerable<string>, IFitsImageLoadProgressViewModel> LoadImagesWithProgress { get; }

        ReactiveCommand<IEnumerable<string>, Unit> LoadImagesWithProgressDialog { get; }

        Interaction<IFitsImageLoadProgressViewModel, Unit> LoadImagesProgressDialog { get; }

        ReactiveCommand<Unit, Unit> SwitchImage { get; }

        bool IsImageSwitched { get; }

        ReactiveCommand<string, Unit> ShowImageFile { get; }

        int AutoLoadMaxImageCount { get; }
        #endregion

        #region +++ Evaluation +++
        ReactiveCommand<Unit, IFitsImageAllStatisticsProgressViewModel> CalculateAllStatisticsWithProgress { get; }

        ReactiveCommand<Unit, Unit> CalculateAllStatisticsWithProgressDialog { get; }

        Interaction<IFitsImageAllStatisticsProgressViewModel, Unit> CalculateAllStatisticsProgressDialog { get; }

        ReactiveCommand<Unit, IEvaluationTableViewModel> ShowEvaluationTable { get; }

        ReactiveCommand<Unit, IEvaluationFormulaViewModel> ShowEvaluationFormula { get; }

        ReactiveCommand<Unit, Unit> ShowEvaluationTableAndFormula { get; }

        ReactiveCommand<Unit, Unit> HideEvaluationTableAndFormula { get; }

        ReactiveCommand<Unit, IEvaluationExporterViewModel> ShowEvaluationExporter { get; }
        #endregion

        #region +++ Job Configurator +++
        ReactiveCommand<Unit, IJobConfiguratorViewModel> ShowJobConfigurator { get; }

        public class JobConfiguratorLoadResult
        {
            public IJobConfiguratorViewModel? JobConfigurator { get; }

            public Exception? Error { get; }

            public bool Successful => JobConfigurator != null;

            public JobConfiguratorLoadResult(IJobConfiguratorViewModel? jobConfigurator, Exception? error)
            {
                JobConfigurator = jobConfigurator;
                Error = error;
            }
        }

        ReactiveCommand<Unit, JobConfiguratorLoadResult> ShowJobConfiguratorWithOpenFileDialog { get; }

        Interaction<Unit, string> JobConfiguratorOpenFileDialog { get; }

        ReactiveCommand<Unit, IJobRunnerViewModel> ShowJobRunner { get; }
        #endregion

        #region +++ Instrument Profiles +++
        ReactiveCommand<Unit, IInstrumentProfileConfiguratorViewModel> ShowInstrumentProfileConfigurator { get; }

        IAppProfileSelectorViewModel AppProfileSelector { get; }
        #endregion

        #region +++ Voyager Integration +++
        bool IsVoyagerIntegrationEnabled { get; }

        bool IsVoyagerIntegrationConnected { get; }

        bool AutoLoadNewVoyagerImages { get; }
        #endregion
    }
}
