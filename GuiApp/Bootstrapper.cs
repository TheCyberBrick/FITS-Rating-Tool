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
using FitsRatingTool.GuiApp.Models;
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
            var container = new Container(rules => rules.WithAutoConcreteTypeResolution());

            RegisterRepositories(container);
            RegisterServices(container);
            RegisterViewModels(container);
            RegisterViews(container);
            RegisterExporters(container);

            //Test(container);

            return container;
        }

        public static void Test(Container container)
        {
            container.Register<ITestVM0, TestVM0>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments), setup: Setup.With(allowDisposableTransient: true));
            container.Register<ITestVM1, TestVM1>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments), setup: Setup.With(allowDisposableTransient: true));
            container.Register<ITestVM2, TestVM2>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments), setup: Setup.With(allowDisposableTransient: true));

            container.Register<IService1, Service1>(Reuse.ScopedTo(TestVM1.MY_SCOPE, ResolutionScopeName.Of<ITestVM1>()));

            //var c0 = new FitsRatingTool.GuiApp.Models.Container<ITestVM0, ITestVM0.Args>(container);

            var d = container.Resolve<IContainerRoot<ITestVM0, ITestVM0.Args>>().Instantiate(new ITestVM0.Args(), out var c0, out var t0);

            t0.Test();

            d.Dispose();
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
            container.Register(typeof(IContainer<,>), typeof(Container<,>));
            container.Register(typeof(IContainerRoot<,>), typeof(ContainerRoot<,>));
            container.Register(typeof(IInstantiatorFactory<,>), typeof(InstantiatorFactory<,>), setup: Setup.With(allowDisposableTransient: true));
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
            // TODO Temp
            // Could use a cleanup

            container.Register<IAppViewModel, AppViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments), setup: Setup.With(allowDisposableTransient: true));
            container.Register<IAppProfileSelectorViewModel, AppProfileSelectorViewModel>();
            container.Register<IAppViewerOverlayViewModel, AppViewerOverlayViewModel>();
            container.Register<IAppImageItemViewModel, AppImageItemViewModel>();

            container.Register<IFitsImageAllStatisticsProgressViewModel, FitsImageAllStatisticsProgressViewModel>();
            container.Register<IFitsImageHeaderRecordViewModel, FitsImageHeaderRecordViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IFitsImageHistogramViewModel, FitsImageHistogramViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IFitsImagePhotometryViewModel, FitsImagePhotometryViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IFitsImagePSFViewModel, FitsImagePSFViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IFitsImageStatisticsProgressViewModel, FitsImageStatisticsProgressViewModel>();
            container.Register<IFitsImageStatisticsViewModel, FitsImageStatisticsViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IFitsImageViewerViewModel, FitsImageViewerViewModel>(setup: Setup.With(allowDisposableTransient: true));
            container.Register<IFitsImageMultiViewerViewModel, FitsImageMultiViewerViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments), setup: Setup.With(allowDisposableTransient: true));
            container.Register<IFitsImageViewModel, FitsImageViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments), setup: Setup.With(allowDisposableTransient: true));
            container.Register<IFitsImageLoadProgressViewModel, FitsImageLoadProgressViewModel>();
            container.Register<IFitsImageSectionViewerViewModel, FitsImageSectionViewerViewModel>();
            container.Register<IFitsImageCornerViewerViewModel, FitsImageCornerViewerViewModel>();

            container.Register<IEvaluationTableViewModel, EvaluationTableViewModel>();
            container.Register<IEvaluationFormulaViewModel, EvaluationFormulaViewModel>();
            container.Register<IEvaluationExporterConfiguratorViewModel, EvaluationExporterConfiguratorViewModel>();
            container.Register<IEvaluationExporterViewModel, EvaluationExporterViewModel>();
            container.Register<IEvaluationExportProgressViewModel, EvaluationExportProgressViewModel>();

            container.Register<IJobGroupingConfiguratorViewModel, JobGroupingConfiguratorViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IJobConfiguratorViewModel, JobConfiguratorViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IJobRunnerViewModel, JobRunnerViewModel>();
            container.Register<IJobRunnerProgressViewModel, JobRunnerProgressViewModel>();

            container.Register<ICSVExporterConfiguratorViewModel, CSVExporterConfiguratorViewModel>();
            container.Register<IFitsHeaderExporterConfiguratorViewModel, FitsHeaderExporterConfiguratorViewModel>();
            container.Register<IVoyagerExporterConfiguratorViewModel, VoyagerExporterConfiguratorViewModel>();
            container.Register<IFileDeleterExporterConfiguratorViewModel, FileDeleterExporterConfiguratorViewModel>();
            container.Register<IFileMoverExporterConfiguratorViewModel, FileMoverExporterConfiguratorViewModel>();

            container.Register<IFileTableViewModel, FileTableViewModel>();

            container.Register<IAppConfigViewModel, AppConfigViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
            container.Register<IAppConfigCategoryViewModel, AppConfigCategoryViewModel>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));

            container.Register<IInstrumentProfileViewModel, InstrumentProfileViewModel>();
            container.Register<IInstrumentProfileSelectorViewModel, InstrumentProfileSelectorViewModel>();
            container.Register<IInstrumentProfileConfiguratorViewModel, InstrumentProfileConfiguratorViewModel>();

            container.Register<IImageAnalysisViewModel, ImageAnalysisViewModel>();
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
