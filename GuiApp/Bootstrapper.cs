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

using DryIoc;
using FitsRatingTool.Common.Services;
using FitsRatingTool.Common.Services.Impl;
using FitsRatingTool.Exporters.Services;
using FitsRatingTool.Exporters.Services.Impl;
using FitsRatingTool.GuiApp.Repositories;
using FitsRatingTool.GuiApp.Repositories.Impl;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.Services.Impl;
using FitsRatingTool.GuiApp.UI.App;
using FitsRatingTool.GuiApp.UI.App.ViewModels;
using FitsRatingTool.GuiApp.UI.App.Views;
using FitsRatingTool.GuiApp.UI.AppConfig;
using FitsRatingTool.GuiApp.UI.AppConfig.ViewModels;
using FitsRatingTool.GuiApp.UI.Evaluation;
using FitsRatingTool.GuiApp.UI.Evaluation.ViewModels;
using FitsRatingTool.GuiApp.UI.Exporters;
using FitsRatingTool.GuiApp.UI.Exporters.ViewModels;
using FitsRatingTool.GuiApp.UI.FileTable;
using FitsRatingTool.GuiApp.UI.FileTable.ViewModels;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;
using FitsRatingTool.GuiApp.UI.ImageAnalysis;
using FitsRatingTool.GuiApp.UI.ImageAnalysis.ViewModels;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels;
using FitsRatingTool.GuiApp.UI.JobConfigurator;
using FitsRatingTool.GuiApp.UI.JobConfigurator.ViewModels;
using FitsRatingTool.GuiApp.UI.JobRunner;
using FitsRatingTool.GuiApp.UI.JobRunner.ViewModels;

namespace FitsRatingTool.GuiApp
{
    public class Bootstrapper
    {
        public static Container Initialize()
        {
            var container = new Container();

            RegisterRepositories(container);
            RegisterServices(container);
            RegisterViewModels(container);
            RegisterViews(container);
            RegisterExporters(container);

            return container;
        }

        private static void RegisterRepositories(Container container)
        {
            container.Register<IAnalysisRepository, AnalysisRepository>(Reuse.Singleton);
            container.Register<IFileRepository, FileRepository>(Reuse.Singleton);
            container.Register<IFitsImageMetadataRepository, FitsImageMetadataRepository>(Reuse.Singleton);
            container.Register<IInstrumentProfileRepository, InstrumentProfileRepository>(Reuse.Singleton);
        }

        private static void RegisterServices(Container container)
        {
            // Common
            container.Register<IFitsImageLoader, AppFitsImageLoader>(Reuse.Singleton);
            container.Register<IGroupingManager, GroupingManager>(Reuse.Singleton);
            container.Register<IEvaluationService, EvaluationService>(Reuse.Singleton);
            container.Register<IJobConfigFactory, JobConfigFactory>(Reuse.Singleton);
            container.Register<IBatchEvaluationService, BatchEvaluationService>(Reuse.Singleton);
            container.Register<IStandaloneEvaluationService, StandaloneEvaluationService>(Reuse.Singleton);
            container.Register<IInstrumentProfileFactory, InstrumentProfileFactory>(Reuse.Singleton);

            // App
            container.Register<IWindowManager, WindowManager>(Reuse.Singleton);
            container.Register<IFitsImageManager, FitsImageManager>(Reuse.Singleton);
            container.Register<IVoyagerIntegration, VoyagerIntegration>(Reuse.Singleton);
            container.Register<IStarSampler, StarSampler>(Reuse.Singleton);
            container.Register<IEvaluationManager, EvaluationManager>(Reuse.Singleton);
            container.Register<IExporterConfiguratorManager, ExporterConfiguratorManager>(Reuse.Singleton);
            container.Register<IFileSystemService, FileSystemService>(Reuse.Singleton);
            container.Register<IOpenFileEventManager, OpenFileEventManager>(Reuse.Singleton);
            container.Register<IAppConfigManager, AppConfigManager>(Reuse.Singleton);
            container.Register<IAppConfig, AppConfig>(Reuse.Singleton);
            container.Register<IInstrumentProfileManager, InstrumentProfileManager>(Reuse.Singleton);
            container.Register<IImageAnalysisManager, ImageAnalysisManager>(Reuse.Singleton);
        }

        private static void RegisterViewModels(Container container)
        {
            container.Register<IAppViewModel, AppViewModel>(Reuse.Singleton, made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IAppProfileSelectorViewModel.IFactory, AppProfileSelectorViewModel.Factory>(Reuse.Singleton);
            container.Register<IAppViewerOverlayViewModel.IFactory, AppViewerOverlayViewModel.Factory>(Reuse.Singleton);
            container.Register<IAppImageItemViewModel.IFactory, AppImageItemViewModel.Factory>(Reuse.Singleton);

            container.Register<IFitsImageAllStatisticsProgressViewModel.IFactory, FitsImageAllStatisticsProgressViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageHeaderRecordViewModel.IFactory, FitsImageHeaderRecordViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageHistogramViewModel.IFactory, FitsImageHistogramViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImagePhotometryViewModel.IFactory, FitsImagePhotometryViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImagePSFViewModel.IFactory, FitsImagePSFViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageStatisticsProgressViewModel.IFactory, FitsImageStatisticsProgressViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageStatisticsViewModel.IFactory, FitsImageStatisticsViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageViewerViewModel.IFactory, FitsImageViewerViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageMultiViewerViewModel.IFactory, FitsImageMultiViewerViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageViewModel.IFactory, FitsImageViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageLoadProgressViewModel.IFactory, FitsImageLoadProgressViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageSectionViewerViewModel.IFactory, FitsImageSectionViewerViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsImageCornerViewerViewModel.IFactory, FitsImageCornerViewerViewModel.Factory>(Reuse.Singleton);

            container.Register<IEvaluationTableViewModel.IFactory, EvaluationTableViewModel.Factory>(Reuse.Singleton);
            container.Register<IEvaluationFormulaViewModel.IFactory, EvaluationFormulaViewModel.Factory>(Reuse.Singleton);
            container.Register<IEvaluationExporterConfiguratorViewModel.IFactory, EvaluationExporterConfiguratorViewModel.Factory>(Reuse.Singleton);
            container.Register<IEvaluationExporterViewModel.IFactory, EvaluationExporterViewModel.Factory>(Reuse.Singleton);
            container.Register<IEvaluationExportProgressViewModel.IFactory, EvaluationExportProgressViewModel.Factory>(Reuse.Singleton);

            container.Register<IJobGroupingConfiguratorViewModel.IFactory, JobGroupingConfiguratorViewModel.Factory>(Reuse.Singleton);
            container.Register<IJobConfiguratorViewModel.IFactory, JobConfiguratorViewModel.Factory>(Reuse.Singleton);
            container.Register<IJobRunnerViewModel.IFactory, JobRunnerViewModel.Factory>(Reuse.Singleton);
            container.Register<IJobRunnerProgressViewModel.IFactory, JobRunnerProgressViewModel.Factory>(Reuse.Singleton);

            container.Register<ICSVExporterConfiguratorViewModel.IFactory, CSVExporterConfiguratorViewModel.Factory>(Reuse.Singleton);
            container.Register<IFitsHeaderExporterConfiguratorViewModel.IFactory, FitsHeaderExporterConfiguratorViewModel.Factory>(Reuse.Singleton);
            container.Register<IVoyagerExporterConfiguratorViewModel.IFactory, VoyagerExporterConfiguratorViewModel.Factory>(Reuse.Singleton);
            container.Register<IFileDeleterExporterConfiguratorViewModel.IFactory, FileDeleterExporterConfiguratorViewModel.Factory>(Reuse.Singleton);
            container.Register<IFileMoverExporterConfiguratorViewModel.IFactory, FileMoverExporterConfiguratorViewModel.Factory>(Reuse.Singleton);

            container.Register<IFileTableViewModel.IFactory, FileTableViewModel.Factory>(Reuse.Singleton);

            container.Register<IAppConfigViewModel.IFactory, AppConfigViewModel.Factory>(Reuse.Singleton);
            container.Register<IAppConfigCategoryViewModel.IFactory, AppConfigCategoryViewModel.Factory>(Reuse.Singleton);

            container.Register<IInstrumentProfileViewModel.IFactory, InstrumentProfileViewModel.Factory>(Reuse.Singleton);
            container.Register<IInstrumentProfileSelectorViewModel.IFactory, InstrumentProfileSelectorViewModel.Factory>(Reuse.Singleton);
            container.Register<IInstrumentProfileConfiguratorViewModel.IFactory, InstrumentProfileConfiguratorViewModel.Factory>(Reuse.Singleton);

            container.Register<IImageAnalysisViewModel.IFactory, ImageAnalysisViewModel.Factory>(Reuse.Singleton);
        }

        private static void RegisterViews(Container container)
        {
            container.Register<AppViewerOverlayView, AppViewerOverlayView>(Reuse.Transient, made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
        }

        private static void RegisterExporters(Container container)
        {
            container.Register<ICSVEvaluationExporterFactory, CSVEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IFitsHeaderEvaluationExporterFactory, FitsHeaderEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IVoyagerEvaluationExporterFactory, VoyagerEvaluationExporterFactory>(Reuse.Singleton);
            container.Register<IFileDeleterExporterFactory, FileDeleterExporterFactory>(Reuse.Singleton);
            container.Register<IFileMoverExporterFactory, FileMoverExporterFactory>(Reuse.Singleton);
        }
    }
}
