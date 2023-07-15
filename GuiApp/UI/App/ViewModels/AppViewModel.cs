﻿/*
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

using FitsRatingTool.Common.Services;
using Microsoft.VisualStudio.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Evaluation;
using FitsRatingTool.GuiApp.UI.Exporters;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.JobConfigurator;
using FitsRatingTool.GuiApp.UI.JobRunner;
using FitsRatingTool.GuiApp.UI.FileTable;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;
using FitsRatingTool.GuiApp.UI.AppConfig;
using System.Collections.Specialized;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using Avalonia.Collections;
using System.ComponentModel.Composition;
using DryIocAttributes;
using FitsRatingTool.IoC;
using FitsRatingTool.GuiApp.UI.Variables;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    [Export(typeof(IAppViewModel)), TransientReuse, AllowDisposableTransient]
    public class AppViewModel : ViewModelBase, IAppViewModel, IDisposable
    {
        public AppViewModel(IRegistrar<IAppViewModel, IAppViewModel.Of> reg)
        {
            reg.RegisterAndReturn<AppViewModel>();
        }

        public ReactiveCommand<Unit, IParameterizedFactory<IFileTableViewModel, IFileTableViewModel.Of>> ShowFileTable { get; }

        public ReactiveCommand<Unit, Unit> HideFileTable { get; }

        public ReactiveCommand<Unit, Unit> Exit { get; }

        public ReactiveCommand<Unit, Unit> ShowAboutDialog { get; }

        public ReactiveCommand<Unit, IParameterizedFactory<IAppConfigViewModel, IAppConfigViewModel.Of>> ShowSettingsDialog { get; }

        public IFitsImageMultiViewerViewModel MultiViewer { get; private set; } = null!;

        public ReactiveCommand<float, Unit> IncreaseThumbnailScale { get; }

        public ReactiveCommand<float, Unit> DecreaseThumbnailScale { get; }

        private float _thumbnailScale = 1.0f;
        public float ThumbnailScale
        {
            get => _thumbnailScale;
            set => this.RaiseAndSetIfChanged(ref _thumbnailScale, value);
        }

        public AvaloniaList<IAppImageItemViewModel> Items { get; } = new();

        private IAppImageItemViewModel? _selectedItem;
        public IAppImageItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public ReactiveCommand<string, Unit> LoadImage { get; }

        public ReactiveCommand<Unit, Unit> UnloadAllImages { get; }

        public ReactiveCommand<string, Unit> UnloadImage { get; }

        public ReactiveCommand<Unit, Unit> LoadImagesWithOpenFileDialog { get; }

        public Interaction<Unit, IEnumerable<string>> LoadImagesOpenFileDialog { get; } = new();

        public ReactiveCommand<IEnumerable<string>, IParameterizedFactory<IFitsImageLoadProgressViewModel, IFitsImageLoadProgressViewModel.OfFiles>> LoadImagesWithProgress { get; }

        public ReactiveCommand<IEnumerable<string>, Unit> LoadImagesWithProgressDialog { get; }

        public Interaction<IFitsImageLoadProgressViewModel, Unit> LoadImagesProgressDialog { get; } = new();

        public ReactiveCommand<Unit, Unit> SwitchImage { get; }

        private bool _isImageSwitched;
        public bool IsImageSwitched
        {
            get => _isImageSwitched;
            private set => this.RaiseAndSetIfChanged(ref _isImageSwitched, value);
        }

        public ReactiveCommand<string, Unit> ShowImageFile { get; }

        private int _autoLoadMaxImageCount = 64;
        public int AutoLoadMaxImageCount
        {
            get => _autoLoadMaxImageCount;
            set => this.RaiseAndSetIfChanged(ref _autoLoadMaxImageCount, value);
        }


        public ReactiveCommand<Unit, IParameterizedFactory<IFitsImageAllStatisticsProgressViewModel, IFitsImageAllStatisticsProgressViewModel.OfFiles>> CalculateAllStatisticsWithProgress { get; }

        public ReactiveCommand<Unit, Unit> CalculateAllStatisticsWithProgressDialog { get; }

        public Interaction<IFitsImageAllStatisticsProgressViewModel, Unit> CalculateAllStatisticsProgressDialog { get; } = new();



        public ReactiveCommand<Unit, IParameterizedFactory<IEvaluationTableViewModel, IEvaluationTableViewModel.Of>> ShowEvaluationTable { get; }

        public ReactiveCommand<Unit, IParameterizedFactory<IEvaluationFormulaViewModel, IEvaluationFormulaViewModel.Of>> ShowEvaluationFormula { get; }

        public ReactiveCommand<Unit, Unit> ShowEvaluationTableAndFormula { get; }

        public ReactiveCommand<Unit, Unit> HideEvaluationTableAndFormula { get; }

        public ReactiveCommand<Unit, IParameterizedFactory<IEvaluationExporterViewModel, IEvaluationExporterViewModel.Of>> ShowEvaluationExporter { get; }


        public ReactiveCommand<Unit, IParameterizedFactory<IJobConfiguratorViewModel, IJobConfiguratorViewModel.Of>> ShowJobConfigurator { get; }

        public ReactiveCommand<Unit, IParameterizedFactory<IJobConfiguratorViewModel, IJobConfiguratorViewModel.OfConfigFile>> ShowJobConfiguratorWithOpenFileDialog { get; }

        public Interaction<Unit, string> JobConfiguratorOpenFileDialog { get; } = new();

        public ReactiveCommand<Unit, IParameterizedFactory<IJobRunnerViewModel, IJobRunnerViewModel.Of>> ShowJobRunner { get; }


        public ReactiveCommand<Unit, IParameterizedFactory<IInstrumentProfileConfiguratorViewModel, IInstrumentProfileConfiguratorViewModel.Of>> ShowInstrumentProfileConfigurator { get; }

        public IAppProfileSelectorViewModel AppProfileSelector { get; private set; } = null!;



        private bool _isVoyagerIntegrationEnabled = false;
        public bool IsVoyagerIntegrationEnabled
        {
            get => _isVoyagerIntegrationEnabled;
            set => this.RaiseAndSetIfChanged(ref _isVoyagerIntegrationEnabled, value);
        }

        private bool _isVoyagerIntegrationConnected = false;
        public bool IsVoyagerIntegrationConnected
        {
            get => _isVoyagerIntegrationConnected;
            set => this.RaiseAndSetIfChanged(ref _isVoyagerIntegrationConnected, value);
        }

        private bool _autoLoadNewVoyagerImages = true;
        public bool AutoLoadNewVoyagerImages
        {
            get => _autoLoadNewVoyagerImages;
            set => this.RaiseAndSetIfChanged(ref _autoLoadNewVoyagerImages, value);
        }





        private string? currentImageFile = null;
        private string? prevImageFile = null;
        private string? preSwitchFile = null;

        private readonly IFitsImageManager manager;
        private readonly IAppConfig appConfig;
        private readonly IVoyagerIntegration voyagerIntegration;
        private readonly IEvaluationExporterManager evaluationExporterManager;
        private readonly IVariableManager variableManager;

        private readonly IContainer<IFitsImageViewModel, IFitsImageViewModel.OfFile> fitsImageContainer;
        private readonly IContainer<IAppImageItemViewModel, IAppImageItemViewModel.OfImage> appImageItemContainer;
        private readonly IContainer<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.Of> appViewerOverlayContainer;
        private readonly IFactoryRoot<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.Of> appViewerOverlayFactory;

        // Designer only
#pragma warning disable CS8618
        public AppViewModel()
        {
            MultiViewer = new FitsImageMultiViewerViewModel();
        }
#pragma warning restore CS8618

        private AppViewModel(IAppViewModel.Of args,
            IFitsImageManager manager,
            IJobConfigFactory jobConfigFactory,
            IFileSystemService fileSystemService,
            IOpenFileEventManager openFileEventManager,
            IAppConfig appConfig,
            IAppConfigManager appConfigManager,
            IVoyagerIntegration voyagerIntegration,
            IEvaluationExporterManager evaluationExporterManager,
            IVariableManager variableManager,
            IContainer<IFitsImageMultiViewerViewModel, IFitsImageMultiViewerViewModel.Of> multiImageViewerContainer,
            IContainer<IFitsImageLoadProgressViewModel, IFitsImageLoadProgressViewModel.OfFiles> imageLoadProgressContainer,
            IContainer<IFitsImageViewModel, IFitsImageViewModel.OfFile> fitsImageContainer,
            IContainer<IFitsImageAllStatisticsProgressViewModel, IFitsImageAllStatisticsProgressViewModel.OfFiles> fitsImageAllStatisticsContainer,
            IContainer<IAppImageItemViewModel, IAppImageItemViewModel.OfImage> appImageItemContainer,
            IContainer<IAppProfileSelectorViewModel, IAppProfileSelectorViewModel.Of> appProfileSelectorContainer,
            IContainer<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.Of> appViewerOverlayContainer,
            IFactoryRoot<IFileTableViewModel, IFileTableViewModel.Of> fileTableFactory,
            IFactoryRoot<IAppConfigViewModel, IAppConfigViewModel.Of> appConfigFactory,
            IFactoryRoot<IFitsImageLoadProgressViewModel, IFitsImageLoadProgressViewModel.OfFiles> fitsImageLoadProgressFactory,
            IFactoryRoot<IFitsImageAllStatisticsProgressViewModel, IFitsImageAllStatisticsProgressViewModel.OfFiles> fitsImageAllStatisticsFactory,
            IFactoryRoot<IEvaluationTableViewModel, IEvaluationTableViewModel.Of> evaluationTableFactory,
            IFactoryRoot<IEvaluationFormulaViewModel, IEvaluationFormulaViewModel.Of> evaluationFormulaFactory,
            IFactoryRoot<IEvaluationExporterViewModel, IEvaluationExporterViewModel.Of> evaluationExporterFactory,
            IFactoryRoot<IJobConfiguratorViewModel, IJobConfiguratorViewModel.Of> jobConfiguratorFactory,
            IFactoryRoot<IJobConfiguratorViewModel, IJobConfiguratorViewModel.OfConfigFile> jobConfiguratorFromFileFactory,
            IFactoryRoot<IJobRunnerViewModel, IJobRunnerViewModel.Of> jobRunnerFactory,
            IFactoryRoot<IInstrumentProfileConfiguratorViewModel, IInstrumentProfileConfiguratorViewModel.Of> instrumentProfileConfiguratorFactory,
            IFactoryRoot<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.Of> appViewerOverlayFactory,
            IContainer<IComponentRegistry<IExporterConfiguratorViewModel>, IComponentRegistry<IExporterConfiguratorViewModel>.Of> exporterConfiguratorRegistryContainer,
            IContainer<IComponentRegistry<IVariableConfiguratorViewModel>, IComponentRegistry<IVariableConfiguratorViewModel>.Of> variableConfiguratorRegistryContainer)
        {
            this.manager = manager;
            this.appConfig = appConfig;
            this.voyagerIntegration = voyagerIntegration;
            this.evaluationExporterManager = evaluationExporterManager;
            this.variableManager = variableManager;
            this.fitsImageContainer = fitsImageContainer;
            this.appImageItemContainer = appImageItemContainer;
            this.appViewerOverlayContainer = appViewerOverlayContainer;
            this.appViewerOverlayFactory = appViewerOverlayFactory;

            multiImageViewerContainer.Singleton().Inject(new IFitsImageMultiViewerViewModel.Of(), vm =>
            {
                MultiViewer = vm;
            });

            appProfileSelectorContainer.Singleton().Inject(new IAppProfileSelectorViewModel.Of(), vm => AppProfileSelector = vm);

            ShowFileTable = ReactiveCommand.Create(() => fileTableFactory.Parameterized(new IFileTableViewModel.Of()));
            HideFileTable = ReactiveCommand.Create(() => { });

            Exit = ReactiveCommand.Create(() => { });

            ShowAboutDialog = ReactiveCommand.Create(() => { });

            ShowSettingsDialog = ReactiveCommand.Create(() => appConfigFactory.Parameterized(new IAppConfigViewModel.Of()));

            LoadImage = ReactiveCommand.CreateFromTask<string>(async (string file) =>
            {
                var image = await Task.Run(async () =>
                {
                    try
                    {
                        var image = fitsImageContainer.Instantiate(new IFitsImageViewModel.OfFile(file, appConfig.MaxImageSize, appConfig.MaxThumbnailWidth, appConfig.MaxThumbnailHeight));
                        image.PreserveColorBalance = false;
                        await image.UpdateOrCreateBitmapAsync();
                        return image;
                    }
                    catch (Exception)
                    {
                    }
                    return null;
                });
                if (image != null && !AddImage(image, out var _))
                {
                    fitsImageContainer.Destroy(image);
                }
            });

            UnloadAllImages = ReactiveCommand.Create(() =>
            {
                IAppImageItemViewModel? item;
                while ((item = Items.FirstOrDefault()) != null)
                {
                    RemoveItem(item);
                    fitsImageContainer.Destroy(item.Image);
                }
            });

            UnloadImage = ReactiveCommand.Create<string>(file =>
            {
                foreach (var item in Items)
                {
                    if (item.Image.File.Equals(file))
                    {
                        RemoveItem(item);
                        fitsImageContainer.Destroy(item.Image);
                        break;
                    }
                }
            });

            LoadImagesWithProgress = ReactiveCommand.Create((IEnumerable<string> files) => fitsImageLoadProgressFactory.Parameterized(new IFitsImageLoadProgressViewModel.OfFiles(files, fitsImageContainer, image =>
            {
                if (!AddImage(image, out var _))
                {
                    fitsImageContainer.Destroy(image);
                }
            })));

            LoadImagesWithProgressDialog = ReactiveCommand.CreateFromTask(async (IEnumerable<string> files) =>
            {
                var factory = await LoadImagesWithProgress.Execute(files);
                if (factory != null)
                {
                    await factory.DoAsync(imageLoadProgressContainer, async vm =>
                    {
                        await LoadImagesProgressDialog.Handle(vm);
                    });
                }
            });

            LoadImagesWithOpenFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var files = await LoadImagesOpenFileDialog.Handle(Unit.Default);

                IAppImageItemViewModel? lastAddedItem = null;

                void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
                {
                    if (e.NewItems != null)
                    {
                        var item = e.NewItems[e.NewItems.Count - 1] as IAppImageItemViewModel;
                        if (files.Contains(item?.Image.File))
                        {
                            lastAddedItem = item;
                        }
                    }
                }

                Items.CollectionChanged += OnItemsChanged;

                try
                {
                    await LoadImagesWithProgressDialog.Execute(files);
                }
                finally
                {
                    Items.CollectionChanged -= OnItemsChanged;
                }

                if (lastAddedItem != null)
                {
                    SelectedItem = lastAddedItem;
                }
                else
                {
                    foreach (var file in files.Reverse())
                    {
                        if (manager.Contains(file))
                        {
                            foreach (var item in Items)
                            {
                                if (item.Image.File == file)
                                {
                                    SelectedItem = item;
                                    return;
                                }
                            }
                        }
                    }
                }
            });

            SwitchImage = ReactiveCommand.Create<Unit>(_ =>
            {
                if (preSwitchFile == null)
                {
                    preSwitchFile = currentImageFile;
                }

                if (prevImageFile != null)
                {
                    foreach (var instance in MultiViewer.Instances)
                    {
                        if (prevImageFile.Equals(instance.Viewer.File))
                        {
                            MultiViewer.SelectedInstance = instance;
                            break;
                        }
                    }
                }

                IsImageSwitched = currentImageFile != null && preSwitchFile != null && !currentImageFile.Equals(preSwitchFile);
            }, this.WhenAnyValue(x => x.MultiViewer.Instances.Count, x => x > 1));

            ShowImageFile = ReactiveCommand.Create<string>(file =>
            {
                if (file != null)
                {
                    fileSystemService.ShowFile(file);
                }
            });


            CalculateAllStatisticsWithProgress = ReactiveCommand.Create(() =>
            {
                List<string> files = new();
                foreach (var item in Items)
                {
                    files.Add(item.Image.File);
                }
                return fitsImageAllStatisticsFactory.Parameterized(new IFitsImageAllStatisticsProgressViewModel.OfFiles(files, true));
            });

            CalculateAllStatisticsWithProgressDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var factory = await CalculateAllStatisticsWithProgress.Execute();
                if (factory != null)
                {
                    await factory.DoAsync(fitsImageAllStatisticsContainer, async vm =>
                    {
                        await CalculateAllStatisticsProgressDialog.Handle(vm);
                    });
                }
            });

            ShowEvaluationTable = ReactiveCommand.Create(() => evaluationTableFactory.Parameterized(new IEvaluationTableViewModel.Of()));

            ShowEvaluationFormula = ReactiveCommand.Create(() => evaluationFormulaFactory.Parameterized(new IEvaluationFormulaViewModel.Of()));

            ShowEvaluationTableAndFormula = ReactiveCommand.Create(() => { });
            HideEvaluationTableAndFormula = ReactiveCommand.Create(() => { });

            ShowEvaluationExporter = ReactiveCommand.Create(() => evaluationExporterFactory.Parameterized(new IEvaluationExporterViewModel.Of()));

            ShowJobConfigurator = ReactiveCommand.Create(() => jobConfiguratorFactory.Parameterized(new IJobConfiguratorViewModel.Of()));

            ShowJobConfiguratorWithOpenFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var file = await JobConfiguratorOpenFileDialog.Handle(Unit.Default);
                if (file.Length > 0)
                {
                    return jobConfiguratorFromFileFactory.Parameterized(new IJobConfiguratorViewModel.OfConfigFile(file));
                }

                return jobConfiguratorFromFileFactory.Parameterized(new IJobConfiguratorViewModel.OfConfigFile(""));
            });

            ShowJobRunner = ReactiveCommand.Create(() => jobRunnerFactory.Parameterized(new IJobRunnerViewModel.Of()));

            ShowInstrumentProfileConfigurator = ReactiveCommand.Create(() => instrumentProfileConfiguratorFactory.Parameterized(new IInstrumentProfileConfiguratorViewModel.Of()));

            IncreaseThumbnailScale = ReactiveCommand.Create<float>(s => ThumbnailScale = Math.Min(1.0f, ThumbnailScale + Math.Max(0.0f, s)));
            DecreaseThumbnailScale = ReactiveCommand.Create<float>(s => ThumbnailScale = Math.Max(0.1f, ThumbnailScale - Math.Max(0.0f, s)));


            SubscribeToEvent<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, AppViewModel>(manager, nameof(manager.RecordChanged), OnRecordChanged);

            SubscribeToEvent<IOpenFileEventManager, IOpenFileEventManager.OpenFileEventArgs, AppViewModel>(openFileEventManager, nameof(openFileEventManager.OnOpenFile), OnOpenFile);

            SubscribeToEvent<IAppConfigManager, IAppConfigManager.ValueChangedEventArgs, AppViewModel>(appConfigManager, nameof(appConfigManager.ValueChanged), OnConfigChanged);
            SubscribeToEvent<IAppConfigManager, IAppConfigManager.ValuesReloadedEventArgs, AppViewModel>(appConfigManager, nameof(appConfigManager.ValuesReloaded), OnConfigChanged);

            exporterConfiguratorRegistryContainer.Singleton().Do(new IComponentRegistry<IExporterConfiguratorViewModel>.Of(), RegisterExporters);
            variableConfiguratorRegistryContainer.Singleton().Do(new IComponentRegistry<IVariableConfiguratorViewModel>.Of(), RegisterVariables);

            RxApp.MainThreadScheduler.Schedule(Initialize);

            if (openFileEventManager.LaunchFilePath != null)
            {
                OnOpenFile(this, new IOpenFileEventManager.OpenFileEventArgs(openFileEventManager.LaunchFilePath));
            }
        }

        protected override void OnInstantiated()
        {
            MultiViewer.OuterOverlayFactory = appViewerOverlayFactory.Delegated(new IAppViewerOverlayViewModel.Of(), appViewerOverlayContainer, isSingleUse: false);

            this.WhenAnyValue(x => x.SelectedItem).Subscribe(item =>
            {
                if (item != null)
                {
                    MultiViewer.File = item.Image.File;
                }
                else
                {
                    MultiViewer.File = null;
                }
            });

            this.WhenAnyValue(x => x.MultiViewer.SelectedInstance!.Viewer.File).Subscribe(file =>
            {
                if (file == null || !file.Equals(prevImageFile))
                {
                    preSwitchFile = null;
                    IsImageSwitched = false;
                }
                prevImageFile = currentImageFile;
                currentImageFile = file;

                manager.CurrentFile = file;
            });

            this.WhenAnyValue(x => x.ThumbnailScale).Subscribe(x =>
            {
                foreach (var item in Items)
                {
                    item.Scale = ThumbnailScale;
                }
            });
        }

        private void RegisterExporters(IComponentRegistry<IExporterConfiguratorViewModel> configuratorRegistry)
        {
            foreach (var id in configuratorRegistry.Ids)
            {
                var factory = configuratorRegistry.GetFactory(id);

                if (factory != null)
                {
                    evaluationExporterManager.Register(id, (ctx, config) =>
                    {
                        var exporter = factory.Do(configurator =>
                        {
                            if (configurator.TryLoadConfig(config))
                            {
                                return configurator.CreateExporter(ctx);
                            }
                            return null;
                        });

                        if (exporter != null)
                        {
                            return exporter;
                        }
                        else
                        {
                            throw new InvalidOperationException("Failed loading exporter config");
                        }
                    });
                }
            }
        }

        private void RegisterVariables(IComponentRegistry<IVariableConfiguratorViewModel> configuratorRegistry)
        {
            foreach (var id in configuratorRegistry.Ids)
            {
                var factory = configuratorRegistry.GetFactory(id);

                if (factory != null)
                {
                    variableManager.Register(id, (config) =>
                    {
                        var exporter = factory.Do(configurator =>
                        {
                            if (configurator.TryLoadConfig(config))
                            {
                                return configurator.CreateVariable();
                            }
                            return null;
                        });

                        if (exporter != null)
                        {
                            return exporter;
                        }
                        else
                        {
                            throw new InvalidOperationException("Failed loading variable config");
                        }
                    });
                }
            }
        }

        private async Task ApplySettingsAsync()
        {
            AutoLoadMaxImageCount = appConfig.AutoLoadMaxImageCount;

            if (appConfig.VoyagerIntegrationEnabled)
            {
                await EnableVoyagerIntegrationAsync();
            }
            else
            {
                await DisableVoyagerIntegrationAsync();
            }
        }

        private async void Initialize()
        {
            await ApplySettingsAsync();
        }

        private async Task EnableVoyagerIntegrationAsync()
        {
            if (!IsVoyagerIntegrationEnabled)
            {
                voyagerIntegration.ApplicationServerHostname = appConfig.VoyagerAddress;
                voyagerIntegration.ApplicationServerPort = appConfig.VoyagerPort;
                voyagerIntegration.ApplicationServerUsername = appConfig.VoyagerUsername;
                voyagerIntegration.ApplicationServerPassword = appConfig.VoyagerPassword;
                voyagerIntegration.RoboTargetSecret = appConfig.RoboTargetSecret;

                voyagerIntegration.NewImage += OnNewVoyagerImage;
                voyagerIntegration.ConnectionChanged += OnConnectionChanged;
                await voyagerIntegration.StartAsync();
                IsVoyagerIntegrationEnabled = true;
            }
        }

        private async Task DisableVoyagerIntegrationAsync()
        {
            if (IsVoyagerIntegrationEnabled)
            {
                await voyagerIntegration.StopAsync();
                voyagerIntegration.NewImage -= OnNewVoyagerImage;
                voyagerIntegration.ConnectionChanged -= OnConnectionChanged;
                IsVoyagerIntegrationEnabled = false;
            }
        }

        private bool AddImage(IFitsImageViewModel image, out IAppImageItemViewModel? item)
        {
            item = null;
            if (!manager.Contains(image.File))
            {
                var record = manager.GetOrAdd(image.File);
                record.Metadata = image;

                var newItem = item = appImageItemContainer.Instantiate(new IAppImageItemViewModel.OfImage(record.Id, image));
                newItem.Scale = ThumbnailScale;
                newItem.Remove.Subscribe(_ =>
                {
                    RemoveItem(newItem);
                    fitsImageContainer.Destroy(newItem.Image);
                });

                Items.Add(item);

                return true;
            }
            return false;
        }

        private bool RemoveItem(IAppImageItemViewModel item)
        {
            // TODO Temp
            // This must remove the item from the appImageItemContainer.
            // Or better, bind appImageItemContainer to Items and remove item
            // from container only.

            var image = item.Image;

            if (SelectedItem == image)
            {
                SelectedItem = null;
            }

            var closeInstances = new List<IFitsImageMultiViewerViewModel.Instance>();

            foreach (var instance in MultiViewer.Instances)
            {
                bool close = false;

                if (instance.Viewer.FitsImage == image)
                {
                    instance.Viewer.FitsImage = null;
                    close = true;
                }

                if (image.File.Equals(instance.Viewer.File))
                {
                    instance.Viewer.File = null;
                    close = true;
                }

                if (instance.IsCloseable && close)
                {
                    closeInstances.Add(instance);
                }
            }

            foreach (var instance in closeInstances)
            {
                instance.Close.Execute().Subscribe();
            }

            bool removed = false;

            removed |= Items.Remove(item);
            removed |= manager.Remove(image.File) != null;

            return removed;
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.File && args.Removed)
            {
                OnImageRemoved(args.File);
            }
        }

        private void OnImageRemoved(string file)
        {
            if (SelectedItem != null && file.Equals(SelectedItem.Image.File))
            {
                var img = SelectedItem.Image;
                SelectedItem = null;
                fitsImageContainer.Destroy(img);
            }

            var closeInstances = new List<IFitsImageMultiViewerViewModel.Instance>();

            foreach (var instance in MultiViewer.Instances)
            {
                bool close = false;

                if (instance.Viewer.FitsImage != null && file.Equals(instance.Viewer.FitsImage.File))
                {
                    var img = instance.Viewer.FitsImage;
                    instance.Viewer.FitsImage = null;
                    fitsImageContainer.Destroy(img);
                    // TODO This shouldn't be needed anymore
                    //img.Dispose();
                    close = true;
                }

                if (file.Equals(instance.Viewer.File))
                {
                    instance.Viewer.File = null;
                    close = true;
                }

                if (instance.IsCloseable && close)
                {
                    closeInstances.Add(instance);
                }
            }

            foreach (var instance in closeInstances)
            {
                instance.Close.Execute().Subscribe();
            }

            foreach (var item in Items)
            {
                if (file.Equals(item.Image.File))
                {
                    Items.Remove(item);
                    fitsImageContainer.Destroy(item.Image);
                    break;
                }
            }
        }

        private async Task AutoLoadNewImageAsync(string file, bool select)
        {
            // Check if image is already loaded, and if so, select it
            foreach (var item in Items)
            {
                if (file.Equals(item.Image.File))
                {
                    if (select)
                    {
                        SelectedItem = item;
                    }
                    return;
                }
            }

            var image = await Task.Run(async () =>
            {
                try
                {
                    var image = fitsImageContainer.Instantiate(new IFitsImageViewModel.OfFile(file, appConfig.MaxImageSize, appConfig.MaxThumbnailWidth, appConfig.MaxThumbnailHeight));
                    image.PreserveColorBalance = false;
                    await image.UpdateOrCreateBitmapAsync();
                    return image;
                }
                catch (Exception)
                {
                }
                return null;
            });

            if (image != null)
            {
                image.IsAutoLoaded = true;

                if (AddImage(image, out var newItem))
                {
                    int autoLoadedCount = 0;

                    for (int i = Items.Count - 1; i >= 0; --i)
                    {
                        var item = Items[i];

                        if (item.Image.IsAutoLoaded)
                        {
                            ++autoLoadedCount;

                            if (autoLoadedCount > AutoLoadMaxImageCount)
                            {
                                RemoveItem(item);
                                fitsImageContainer.Destroy(item.Image);
                            }
                        }
                    }

                    if (select)
                    {
                        SelectedItem = newItem;
                    }
                }
                else
                {
                    fitsImageContainer.Destroy(image);
                }
            }
        }

        private void OnOpenFile(object? sender, IOpenFileEventManager.OpenFileEventArgs e)
        {
            if (File.Exists(e.File))
            {
                async void open()
                {
                    await AutoLoadNewImageAsync(e.File, true);
                };
                RxApp.MainThreadScheduler.Schedule(open);
            }
        }

        private async void OnNewVoyagerImage(object? sender, IVoyagerIntegration.NewImageEventArgs e)
        {
            if (AutoLoadNewVoyagerImages && File.Exists(e.File))
            {
                await AutoLoadNewImageAsync(e.File, true);
            }
        }

        private void OnConnectionChanged(object? sender, IVoyagerIntegration.ConnectionChangedEventArgs e)
        {
            IsVoyagerIntegrationConnected = e.Connected;
        }

        private async void OnConfigChanged(object? sender, IAppConfigManager.ValueEventArgs e)
        {
            await ApplySettingsAsync();

            if (IsVoyagerIntegrationEnabled
                && (e.IsAffected(nameof(appConfig.VoyagerAddress)) || e.IsAffected(nameof(appConfig.VoyagerPassword))
                 || e.IsAffected(nameof(appConfig.VoyagerPort)) || e.IsAffected(nameof(appConfig.VoyagerUsername))
                 || e.IsAffected(nameof(appConfig.RoboTargetSecret))))
            {
                // Restart voyager integration to apply new config values
                await DisableVoyagerIntegrationAsync();
                await EnableVoyagerIntegrationAsync();
            }
        }

        public void Dispose()
        {
            appViewerOverlayFactory.Dispose();
        }
    }
}
