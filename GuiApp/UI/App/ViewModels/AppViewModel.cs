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
using System.Collections.ObjectModel;
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
using static FitsRatingTool.GuiApp.UI.App.IAppViewModel;
using FitsRatingTool.GuiApp.UI.FileTable;
using FitsRatingTool.GuiApp.UI.FitsImage.ViewModels;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppViewModel : ViewModelBase, IAppViewModel
    {
        public ReactiveCommand<Unit, IFileTableViewModel> ShowFileTable { get; }

        public ReactiveCommand<Unit, Unit> HideFileTable { get; }

        public ReactiveCommand<Unit, Unit> Exit { get; }

        public ReactiveCommand<Unit, Unit> ShowAboutDialog { get; }

        public IFitsImageMultiViewerViewModel MultiViewer { get; }

        public ObservableCollection<Item> Items { get; } = new();

        private Item? _selectedItem;
        public Item? SelectedItem
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

        private int _autoLoadMaxImageCount = 64;
        public int AutoLoadMaxImageCount
        {
            get => _autoLoadMaxImageCount;
            set => this.RaiseAndSetIfChanged(ref _autoLoadMaxImageCount, value);
        }

        private bool _autoLoadNewVoyagerImages = true;
        public bool AutoLoadNewVoyagerImages
        {
            get => _autoLoadNewVoyagerImages;
            set => this.RaiseAndSetIfChanged(ref _autoLoadNewVoyagerImages, value);
        }

        public ReactiveCommand<Unit, Unit> EnableVoyagerIntegration { get; }

        public ReactiveCommand<Unit, Unit> DisableVoyagerIntegration { get; }

        public ReactiveCommand<Unit, Unit> ToggleVoyagerIntegration { get; }




        private string? currentImageFile = null;
        private string? prevImageFile = null;
        private string? preSwitchFile = null;

        private readonly IFitsImageManager manager;
        private readonly IFitsImageViewModel.IFactory fitsImageFactory;
        private readonly IVoyagerIntegration voyagerIntegration;

        // Designer only
#pragma warning disable CS8618
        public AppViewModel()
        {
            MultiViewer = new FitsImageMultiViewerViewModel();
        }
#pragma warning restore CS8618

        public AppViewModel(IFitsImageManager manager, IFitsImageMultiViewerViewModel.IFactory multiImageViewerFactory, IFitsImageLoadProgressViewModel.IFactory imageLoadProgressFactory,
            IEvaluationTableViewModel.IFactory evaluationTableFactory, IEvaluationFormulaViewModel.IFactory evaluationFormulaFactory,
            IFitsImageViewModel.IFactory fitsImageFactory, IFitsImageAllStatisticsProgressViewModel.IFactory fitsImageAllStatisticsFactory,
            IVoyagerIntegration voyagerIntegration, IJobConfiguratorViewModel.IFactory jobConfiguratorFactory, IExporterConfiguratorManager exporterConfiguratorManager,
            IJobConfigManager jobConfigManager, ICSVExporterConfiguratorViewModel.IFactory csvExporterConfiguratorFactory,
            IFitsHeaderExporterConfiguratorViewModel.IFactory fitsHeaderExporterConfiguratorFactory, IVoyagerExporterConfiguratorViewModel.IFactory voyagerExporterConfiguratorFactory,
            IEvaluationExporterViewModel.IFactory evaluationExporterFactory, IJobRunnerViewModel.IFactory jobRunnerFactory, IFileTableViewModel.IFactory fileTableFactory,
            IFileSystemService fileSystemService, IOpenFileEventManager openFileEventManager)
        {
            this.manager = manager;
            this.fitsImageFactory = fitsImageFactory;
            this.voyagerIntegration = voyagerIntegration;

            RegisterExporterConfigurators(exporterConfiguratorManager, csvExporterConfiguratorFactory, fitsHeaderExporterConfiguratorFactory, voyagerExporterConfiguratorFactory);

            MultiViewer = multiImageViewerFactory.Create();

            ShowFileTable = ReactiveCommand.Create(() => fileTableFactory.Create());
            HideFileTable = ReactiveCommand.Create(() => { });

            Exit = ReactiveCommand.Create(() => { });

            ShowAboutDialog = ReactiveCommand.Create(() => { });

            LoadImage = ReactiveCommand.CreateFromTask<string>(async file =>
            {
                var image = await Task.Run(async () =>
                {
                    try
                    {
                        var image = fitsImageFactory.Create(file, 805306368, 256, 256);
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
                Item? item;
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

                await LoadImagesWithProgressDialog.Execute(files);
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
                        var config = jobConfigManager.Load(file);

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

            EnableVoyagerIntegration = ReactiveCommand.CreateFromTask(DoEnableVoyagerIntegrationAsync);
            DisableVoyagerIntegration = ReactiveCommand.CreateFromTask(DoDisableVoyagerIntegrationAsync);
            ToggleVoyagerIntegration = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsVoyagerIntegrationEnabled)
                {
                    await DoDisableVoyagerIntegrationAsync();
                }
                else
                {
                    await DoEnableVoyagerIntegrationAsync();
                }
            });


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
            });

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, AppViewModel>(manager, nameof(manager.RecordChanged), OnRecordChanged);

            WeakEventHandlerManager.Subscribe<IOpenFileEventManager, IOpenFileEventManager.OpenFileEventArgs, AppViewModel>(openFileEventManager, nameof(openFileEventManager.OnOpenFile), OnOpenFile);

            RxApp.MainThreadScheduler.Schedule(Initialize);

            if (openFileEventManager.LaunchFilePath != null)
            {
                OnOpenFile(this, new IOpenFileEventManager.OpenFileEventArgs(openFileEventManager.LaunchFilePath));
            }
        }

        private async void Initialize()
        {
            await DoEnableVoyagerIntegrationAsync();
        }

        private async void Shutdown()
        {
            await DoDisableVoyagerIntegrationAsync();
        }

        private async Task DoEnableVoyagerIntegrationAsync()
        {
            if (!IsVoyagerIntegrationEnabled)
            {
                //TODO
                voyagerIntegration.ApplicationServerHostname = "172.30.203.126";

                voyagerIntegration.NewImage += OnNewVoyagerImage;
                voyagerIntegration.ConnectionChanged += OnConnectionChanged;
                await voyagerIntegration.StartAsync();
                IsVoyagerIntegrationEnabled = true;
            }
        }

        private async Task DoDisableVoyagerIntegrationAsync()
        {
            if (IsVoyagerIntegrationEnabled)
            {
                await voyagerIntegration.StopAsync();
                voyagerIntegration.NewImage -= OnNewVoyagerImage;
                voyagerIntegration.ConnectionChanged -= OnConnectionChanged;
                IsVoyagerIntegrationEnabled = false;
            }
        }

        private bool AddImage(IFitsImageViewModel image, out Item? item)
        {
            item = null;
            if (!manager.Contains(image.File))
            {
                var newItem = item = new Item(image);
                newItem.Remove.Subscribe(_ =>
                {
                    RemoveItem(newItem);
                    newItem.Image.Dispose();
                });

                Items.Add(item);
                manager.GetOrAdd(image.File).Metadata = image;

                return true;
            }
            return false;
        }

        private bool RemoveItem(Item item)
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
                    var image = fitsImageFactory.Create(file, 805306368, 256, 256);
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

        private void RegisterExporterConfigurators(IExporterConfiguratorManager exporterConfiguratorManager, ICSVExporterConfiguratorViewModel.IFactory csvExporterConfiguratorFactory,
            IFitsHeaderExporterConfiguratorViewModel.IFactory fitsHeaderExporterConfiguratorFactory, IVoyagerExporterConfiguratorViewModel.IFactory voyagerExporterConfiguratorFactory)
        {
            exporterConfiguratorManager.Register("csv", new IExporterConfiguratorManager.Factory("CSV", csvExporterConfiguratorFactory.Create));
            exporterConfiguratorManager.Register("fits_header", new IExporterConfiguratorManager.Factory("FIT Header", fitsHeaderExporterConfiguratorFactory.Create));
            exporterConfiguratorManager.Register("voyager", new IExporterConfiguratorManager.Factory("Voyager RoboTarget", voyagerExporterConfiguratorFactory.Create));
        }
    }
}
