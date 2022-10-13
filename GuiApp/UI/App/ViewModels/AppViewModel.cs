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
using Avalonia.Utilities;
using FitsRatingTool.GuiApp.UI.FileTable;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;
using FitsRatingTool.GuiApp.UI.AppConfig;
using System.Collections.Specialized;
using System.Text;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using Avalonia.Collections;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppViewModel : ViewModelBase, IAppViewModel
    {
        public ReactiveCommand<Unit, IFileTableViewModel> ShowFileTable { get; }

        public ReactiveCommand<Unit, Unit> HideFileTable { get; }

        public ReactiveCommand<Unit, Unit> Exit { get; }

        public ReactiveCommand<Unit, Unit> ShowAboutDialog { get; }

        public ReactiveCommand<Unit, IAppConfigViewModel> ShowSettingsDialog { get; }

        public IFitsImageMultiViewerViewModel MultiViewer { get; }

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

        public ReactiveCommand<IEnumerable<string>, IFitsImageLoadProgressViewModel> LoadImagesWithProgress { get; }

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


        public ReactiveCommand<Unit, IFitsImageAllStatisticsProgressViewModel> CalculateAllStatisticsWithProgress { get; }

        public ReactiveCommand<Unit, Unit> CalculateAllStatisticsWithProgressDialog { get; }

        public Interaction<IFitsImageAllStatisticsProgressViewModel, Unit> CalculateAllStatisticsProgressDialog { get; } = new();



        public ReactiveCommand<Unit, IEvaluationTableViewModel> ShowEvaluationTable { get; }

        public ReactiveCommand<Unit, IEvaluationFormulaViewModel> ShowEvaluationFormula { get; }

        public ReactiveCommand<Unit, Unit> ShowEvaluationTableAndFormula { get; }

        public ReactiveCommand<Unit, Unit> HideEvaluationTableAndFormula { get; }

        public ReactiveCommand<Unit, IEvaluationExporterViewModel> ShowEvaluationExporter { get; }


        public ReactiveCommand<Unit, IJobConfiguratorViewModel> ShowJobConfigurator { get; }

        public ReactiveCommand<Unit, IAppViewModel.JobConfiguratorLoadResult> ShowJobConfiguratorWithOpenFileDialog { get; }

        public Interaction<Unit, string> JobConfiguratorOpenFileDialog { get; } = new();

        public ReactiveCommand<Unit, IJobRunnerViewModel> ShowJobRunner { get; }


        public ReactiveCommand<Unit, IInstrumentProfileConfiguratorViewModel> ShowInstrumentProfileConfigurator { get; }

        public IAppProfileSelectorViewModel AppProfileSelector { get; }



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
        private readonly IFitsImageViewModel.IFactory fitsImageFactory;
        private readonly IAppImageItemViewModel.IFactory appImageItemFactory;
        private readonly IVoyagerIntegration voyagerIntegration;
        private readonly IAppConfig appConfig;

        // Designer only
#pragma warning disable CS8618
        public AppViewModel()
        {
            MultiViewer = new FitsImageMultiViewerViewModel();
        }
#pragma warning restore CS8618

        public AppViewModel(IFitsImageManager manager, IFitsImageMultiViewerViewModel.IFactory multiImageViewerFactory, IFitsImageLoadProgressViewModel.IFactory imageLoadProgressFactory,
            IEvaluationTableViewModel.IFactory evaluationTableFactory, IEvaluationFormulaViewModel.IFactory evaluationFormulaFactory,
            IFitsImageViewModel.IFactory fitsImageFactory, IFitsImageAllStatisticsProgressViewModel.IFactory fitsImageAllStatisticsFactory, IAppImageItemViewModel.IFactory appImageItemFactory,
            IVoyagerIntegration voyagerIntegration, IJobConfiguratorViewModel.IFactory jobConfiguratorFactory, IExporterConfiguratorManager exporterConfiguratorManager,
            IJobConfigFactory jobConfigFactory, ICSVExporterConfiguratorViewModel.IFactory csvExporterConfiguratorFactory,
            IFitsHeaderExporterConfiguratorViewModel.IFactory fitsHeaderExporterConfiguratorFactory, IVoyagerExporterConfiguratorViewModel.IFactory voyagerExporterConfiguratorFactory,
            IEvaluationExporterViewModel.IFactory evaluationExporterFactory, IJobRunnerViewModel.IFactory jobRunnerFactory, IFileTableViewModel.IFactory fileTableFactory,
            IFileSystemService fileSystemService, IOpenFileEventManager openFileEventManager, IAppConfig appConfig, IAppConfigManager appConfigManager,
            IAppConfigViewModel.IFactory appConfigFactory, IInstrumentProfileConfiguratorViewModel.IFactory instrumentProfileConfiguratorFactory,
            IFileDeleterExporterConfiguratorViewModel.IFactory fileDeleterExporterConfiguratorFactory, IAppProfileSelectorViewModel.IFactory appProfileSelectorFactory,
            IAppViewerOverlayViewModel.IFactory appViewerOverlayFactory, IFileMoverExporterConfiguratorViewModel.IFactory fileMoverExporterConfiguratorFactory)
        {
            this.manager = manager;
            this.fitsImageFactory = fitsImageFactory;
            this.appImageItemFactory = appImageItemFactory;
            this.voyagerIntegration = voyagerIntegration;
            this.appConfig = appConfig;

            RegisterExporterConfigurators(exporterConfiguratorManager, csvExporterConfiguratorFactory, fitsHeaderExporterConfiguratorFactory, voyagerExporterConfiguratorFactory, fileDeleterExporterConfiguratorFactory, fileMoverExporterConfiguratorFactory);

            MultiViewer = multiImageViewerFactory.Create();
            MultiViewer.OuterOverlayFactory = appViewerOverlayFactory;

            AppProfileSelector = appProfileSelectorFactory.Create();

            ShowFileTable = ReactiveCommand.Create(() => fileTableFactory.Create());
            HideFileTable = ReactiveCommand.Create(() => { });

            Exit = ReactiveCommand.Create(() => { });

            ShowAboutDialog = ReactiveCommand.Create(() => { });

            ShowSettingsDialog = ReactiveCommand.Create(() => appConfigFactory.Create());

            LoadImage = ReactiveCommand.CreateFromTask<string>(async file =>
            {
                var image = await Task.Run(async () =>
                {
                    try
                    {
                        var image = fitsImageFactory.Create(file, appConfig.MaxImageSize, appConfig.MaxThumbnailWidth, appConfig.MaxThumbnailHeight);
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
                    image.Dispose();
                }
            });

            UnloadAllImages = ReactiveCommand.Create(() =>
            {
                IAppImageItemViewModel? item;
                while ((item = Items.FirstOrDefault()) != null)
                {
                    RemoveItem(item);
                    item.Image.Dispose();
                }
            });

            UnloadImage = ReactiveCommand.Create<string>(file =>
            {
                foreach (var item in Items)
                {
                    if (item.Image.File.Equals(file))
                    {
                        RemoveItem(item);
                        item.Image.Dispose();
                        break;
                    }
                }
            });

            LoadImagesWithProgress = ReactiveCommand.Create<IEnumerable<string>, IFitsImageLoadProgressViewModel>(files => imageLoadProgressFactory.Create(files, image =>
            {
                if (!AddImage(image, out var _))
                {
                    image.Dispose();
                }
            }));

            LoadImagesWithProgressDialog = ReactiveCommand.CreateFromTask<IEnumerable<string>>(async files =>
            {
                var vm = await LoadImagesWithProgress.Execute(files);
                if (vm != null)
                {
                    await LoadImagesProgressDialog.Handle(vm);
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
                return fitsImageAllStatisticsFactory.Create(files, true);
            });

            CalculateAllStatisticsWithProgressDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var vm = await CalculateAllStatisticsWithProgress.Execute();
                if (vm != null)
                {
                    await CalculateAllStatisticsProgressDialog.Handle(vm);
                }
            });

            ShowEvaluationTable = ReactiveCommand.Create(() => evaluationTableFactory.Create());
            ShowEvaluationFormula = ReactiveCommand.Create(() => evaluationFormulaFactory.Create());
            ShowEvaluationTableAndFormula = ReactiveCommand.Create(() => { });
            HideEvaluationTableAndFormula = ReactiveCommand.Create(() => { });
            ShowEvaluationExporter = ReactiveCommand.Create(() => evaluationExporterFactory.Create());

            ShowJobConfigurator = ReactiveCommand.Create(() => jobConfiguratorFactory.Create());
            ShowJobConfiguratorWithOpenFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                Exception? error = null;

                var file = await JobConfiguratorOpenFileDialog.Handle(Unit.Default);
                if (file.Length > 0)
                {
                    try
                    {
                        var config = jobConfigFactory.Load(await File.ReadAllTextAsync(file, Encoding.UTF8));

                        var vm = jobConfiguratorFactory.Create();

                        if (vm.TryLoadJobConfig(config))
                        {
                            return new IAppViewModel.JobConfiguratorLoadResult(vm, null);
                        }
                        else
                        {
                            error = new Exception("Failed loading job config into job configurator");
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                }

                return new IAppViewModel.JobConfiguratorLoadResult(null, error);
            });
            ShowJobRunner = ReactiveCommand.Create(() => jobRunnerFactory.Create());

            ShowInstrumentProfileConfigurator = ReactiveCommand.Create(() => instrumentProfileConfiguratorFactory.Create());

            IncreaseThumbnailScale = ReactiveCommand.Create<float>(s => ThumbnailScale = Math.Min(1.0f, ThumbnailScale + Math.Max(0.0f, s)));
            DecreaseThumbnailScale = ReactiveCommand.Create<float>(s => ThumbnailScale = Math.Max(0.1f, ThumbnailScale - Math.Max(0.0f, s)));


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

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, AppViewModel>(manager, nameof(manager.RecordChanged), OnRecordChanged);

            WeakEventHandlerManager.Subscribe<IOpenFileEventManager, IOpenFileEventManager.OpenFileEventArgs, AppViewModel>(openFileEventManager, nameof(openFileEventManager.OnOpenFile), OnOpenFile);

            WeakEventHandlerManager.Subscribe<IAppConfigManager, IAppConfigManager.ValueChangedEventArgs, AppViewModel>(appConfigManager, nameof(appConfigManager.ValueChanged), OnConfigChanged);
            WeakEventHandlerManager.Subscribe<IAppConfigManager, IAppConfigManager.ValuesReloadedEventArgs, AppViewModel>(appConfigManager, nameof(appConfigManager.ValuesReloaded), OnConfigChanged);

            RxApp.MainThreadScheduler.Schedule(Initialize);

            if (openFileEventManager.LaunchFilePath != null)
            {
                OnOpenFile(this, new IOpenFileEventManager.OpenFileEventArgs(openFileEventManager.LaunchFilePath));
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

                var newItem = item = appImageItemFactory.Create(record.Id, image);
                newItem.Scale = ThumbnailScale;
                newItem.Remove.Subscribe(_ =>
                {
                    RemoveItem(newItem);
                    newItem.Image.Dispose();
                });

                Items.Add(item);

                return true;
            }
            return false;
        }

        private bool RemoveItem(IAppImageItemViewModel item)
        {
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
                img.Dispose();
            }

            var closeInstances = new List<IFitsImageMultiViewerViewModel.Instance>();

            foreach (var instance in MultiViewer.Instances)
            {
                bool close = false;

                if (instance.Viewer.FitsImage != null && file.Equals(instance.Viewer.FitsImage.File))
                {
                    var img = instance.Viewer.FitsImage;
                    instance.Viewer.FitsImage = null;
                    img.Dispose();
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
                    item.Image.Dispose();
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
                    var image = fitsImageFactory.Create(file, appConfig.MaxImageSize, appConfig.MaxThumbnailWidth, appConfig.MaxThumbnailHeight);
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
                                item.Image.Dispose();
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
                    image.Dispose();
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

        private void RegisterExporterConfigurators(IExporterConfiguratorManager exporterConfiguratorManager, ICSVExporterConfiguratorViewModel.IFactory csvExporterConfiguratorFactory,
            IFitsHeaderExporterConfiguratorViewModel.IFactory fitsHeaderExporterConfiguratorFactory, IVoyagerExporterConfiguratorViewModel.IFactory voyagerExporterConfiguratorFactory,
            IFileDeleterExporterConfiguratorViewModel.IFactory fileDeleterExporterConfiguratorFactory, IFileMoverExporterConfiguratorViewModel.IFactory fileMoverExporterConfiguratorFactory)
        {
            exporterConfiguratorManager.Register("csv", new IExporterConfiguratorManager.Factory("CSV", csvExporterConfiguratorFactory.Create));
            exporterConfiguratorManager.Register("fits_header", new IExporterConfiguratorManager.Factory("FITS Header", fitsHeaderExporterConfiguratorFactory.Create));
            exporterConfiguratorManager.Register("voyager", new IExporterConfiguratorManager.Factory("Voyager RoboTarget", voyagerExporterConfiguratorFactory.Create));
            exporterConfiguratorManager.Register("file_deleter", new IExporterConfiguratorManager.Factory("File Deleter", fileDeleterExporterConfiguratorFactory.Create));
            exporterConfiguratorManager.Register("file_mover", new IExporterConfiguratorManager.Factory("File Mover", fileMoverExporterConfiguratorFactory.Create));
        }
    }
}
