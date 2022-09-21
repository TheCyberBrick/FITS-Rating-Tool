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
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;
using Microsoft.VisualStudio.Threading;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Progress;
using FitsRatingTool.GuiApp.Utils;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageViewerViewModel : ViewModelBase, IFitsImageViewerViewModel
    {
        public class Factory : IFitsImageViewerViewModel.IFactory
        {
            private readonly IFitsImageManager manager;
            private readonly IFitsImageViewModel.IFactory fitsImageFactory;
            private readonly IFitsImageSectionViewerViewModel.IFactory fitsImageSectionViewerFactory;
            private readonly IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory;
            private readonly IFitsImageHistogramViewModel.IFactory fitsImageHistogramFactory;
            private readonly IStarSampler starSampler;
            private readonly IAppConfig appConfig;

            public Factory(IFitsImageManager manager, IFitsImageViewModel.IFactory fitsImageFactory, IFitsImageSectionViewerViewModel.IFactory fitsImageSectionViewerFactory,
                IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory, IFitsImageHistogramViewModel.IFactory fitsImageHistogramFactory, IStarSampler starSampler, IAppConfig appConfig)
            {
                this.manager = manager;
                this.fitsImageFactory = fitsImageFactory;
                this.fitsImageSectionViewerFactory = fitsImageSectionViewerFactory;
                this.fitsImageStatisticsFactory = fitsImageStatisticsFactory;
                this.fitsImageHistogramFactory = fitsImageHistogramFactory;
                this.starSampler = starSampler;
                this.appConfig = appConfig;
            }

            public IFitsImageViewerViewModel Create()
            {
                return new FitsImageViewerViewModel(manager, fitsImageFactory, fitsImageSectionViewerFactory, fitsImageStatisticsFactory, fitsImageHistogramFactory, starSampler, appConfig);
            }
        }

        private IDisposable? fitsImageObserverDisposable = null;

        private IFitsImageViewModel? _fitsImage;
        public IFitsImageViewModel? FitsImage
        {
            get => _fitsImage;
            set
            {
                if (_fitsImage != value)
                {
                    lock (this)
                    {
                        if (fitsImageObserverDisposable != null)
                        {
                            fitsImageObserverDisposable.Dispose();
                            fitsImageObserverDisposable = null;
                        }

                        if (value != null)
                        {
                            fitsImageObserverDisposable = value.WhenAnyValue(x => x.HasImage).Subscribe(x =>
                            {
                                this.RaisePropertyChanged(nameof(HasImage));
                            });
                        }
                    }

                    if (value == null && _fitsImage != null)
                    {
                        CancelLoadingTasks();
                    }

                    this.RaisePropertyChanging(nameof(FitsImage));
                    _fitsImage = value;
                    this.RaisePropertyChanged(nameof(FitsImage));

                    this.RaisePropertyChanged(nameof(HasImage));

                    this.RaisePropertyChanged(nameof(IsLoadingFromFile));
                }
            }
        }

        public bool HasImage
        {
            get => FitsImage != null && FitsImage.HasImage;
        }


        private string? _file;
        public string? File
        {
            get => _file;
            set
            {
                if (value == null && _file != null)
                {
                    CancelLoadingTasks();
                }

                this.RaiseAndSetIfChanged(ref _file, value);
                this.RaisePropertyChanged(nameof(FileName));
                this.RaisePropertyChanged(nameof(IsLoadingFromFile));
            }
        }

        public string? FileName
        {
            get
            {
                if (_file != null)
                {
                    return Path.GetFileName(_file);
                }
                return null;
            }
        }


        private bool _isPrimaryViewer = true;
        public bool IsPrimaryViewer
        {
            get => _isPrimaryViewer;
            set => this.RaiseAndSetIfChanged(ref _isPrimaryViewer, value);
        }

        private bool _keepStretch;
        public bool KeepStretch
        {
            get => _keepStretch;
            set => this.RaiseAndSetIfChanged(ref _keepStretch, value);
        }


        private bool _showPhotometry = false;
        public bool ShowPhotometry
        {
            get => _showPhotometry;
            set => this.RaiseAndSetIfChanged(ref _showPhotometry, value);
        }

        private bool _showPhotometryMeasurements;
        public bool ShowPhotometryMeasurements
        {
            get => _showPhotometryMeasurements;
            set => this.RaiseAndSetIfChanged(ref _showPhotometryMeasurements, value);
        }

        private int _maxShownPhotometry = 250;
        public int MaxShownPhotometry
        {
            get => _maxShownPhotometry;
            set => this.RaiseAndSetIfChanged(ref _maxShownPhotometry, value);
        }

        private bool _isShownPhotometryIncomplete;
        public bool IsShownPhotometryIncomplete
        {
            get => _isShownPhotometryIncomplete;
            private set => this.RaiseAndSetIfChanged(ref _isShownPhotometryIncomplete, value);
        }

        private bool _autoCalculateStatistics = true;
        public bool AutoCalculateStatistics
        {
            get => _autoCalculateStatistics;
            set => this.RaiseAndSetIfChanged(ref _autoCalculateStatistics, value);
        }

        private bool _autoSetInterpolationModel = true;
        public bool AutoSetInterpolationMode
        {
            get => _autoSetInterpolationModel;
            set => this.RaiseAndSetIfChanged(ref _autoSetInterpolationModel, value);
        }

        private bool _isPeekViewerEnabled = true;
        public bool IsPeekViewerEnabled
        {
            get => _isPeekViewerEnabled;
            set => this.RaiseAndSetIfChanged(ref _isPeekViewerEnabled, value);
        }

        private int _peekViewerSize = 20;
        public int PeekViewerSize
        {
            get => _peekViewerSize;
            set => this.RaiseAndSetIfChanged(ref _peekViewerSize, value);
        }

        private IFitsImageSectionViewerViewModel? _peekViewer;
        public IFitsImageSectionViewerViewModel? PeekViewer
        {
            get => _peekViewer;
            private set => this.RaiseAndSetIfChanged(ref _peekViewer, value);
        }


        private IFitsImageStatisticsViewModel? _statistics;
        public IFitsImageStatisticsViewModel? Statistics
        {
            get => _statistics;
            private set => this.RaiseAndSetIfChanged(ref _statistics, value);
        }


        public AvaloniaList<IFitsImagePhotometryViewModel> Photometry { get; } = new();


        private IFitsImageHistogramViewModel? _histogram;
        public IFitsImageHistogramViewModel? Histogram
        {
            get => _histogram;
            private set => this.RaiseAndSetIfChanged(ref _histogram, value);
        }


        private IFitsImageHistogramViewModel? _stretchedHistogram;
        public IFitsImageHistogramViewModel? StretchedHistogram
        {
            get => _stretchedHistogram;
            private set => this.RaiseAndSetIfChanged(ref _stretchedHistogram, value);
        }


        public bool IsLoadingFromFile
        {
            get => (FitsImage == null && File != null) || (FitsImage != null && File != null && !FitsImage.File.Equals(File));
        }


        public ReactiveCommand<BitmapInterpolationMode, Unit> SetInterpolationMode { get; }

        public ReactiveCommand<Unit, Unit> ResetStretch { get; }

        public ReactiveCommand<Unit, Unit> ApplyStretchToAll { get; }

        public ReactiveCommand<Unit, Unit> CalculateStatistics { get; }

        public ReactiveCommand<Unit, IFitsImageStatisticsProgressViewModel?> CalculateStatisticsWithProgress { get; }

        public ReactiveCommand<Unit, Unit> CalculateStatisticsWithProgressDialog { get; }

        public Interaction<IFitsImageStatisticsProgressViewModel, Unit> CalculateStatisticsProgressDialog { get; } = new();

        public ReactiveCommand<string?, IFitsImageViewModel?> LoadImage { get; }



        private IFitsImageViewerViewModel.IOverlayFactory? _innerOverlayFactory;
        public IFitsImageViewerViewModel.IOverlayFactory? InnerOverlayFactory
        {
            get => _innerOverlayFactory;
            set
            {
                var oldValue = _innerOverlayFactory;
                this.RaiseAndSetIfChanged(ref _innerOverlayFactory, value);
                if (value != null && value != oldValue)
                {
                    InnerOverlay = value.Create(this);
                }
                else if (value == null)
                {
                    InnerOverlay = null;
                }
            }
        }

        private IFitsImageViewerViewModel.IOverlay? _innerOverlay;
        public IFitsImageViewerViewModel.IOverlay? InnerOverlay
        {
            get => _innerOverlay;
            private set => this.RaiseAndSetIfChanged(ref _innerOverlay, value);
        }

        private IFitsImageViewerViewModel.IOverlayFactory? _outerOverlayFactory;
        public IFitsImageViewerViewModel.IOverlayFactory? OuterOverlayFactory
        {
            get => _outerOverlayFactory;
            set
            {
                var oldValue = _outerOverlayFactory;
                this.RaiseAndSetIfChanged(ref _outerOverlayFactory, value);
                if (value != null && value != oldValue)
                {
                    OuterOverlay = value.Create(this);
                }
                else if (value == null)
                {
                    OuterOverlay = null;
                }
            }
        }

        private IFitsImageViewerViewModel.IOverlay? _outerOverlay;
        public IFitsImageViewerViewModel.IOverlay? OuterOverlay
        {
            get => _outerOverlay;
            private set => this.RaiseAndSetIfChanged(ref _outerOverlay, value);
        }



        public long MaxInputSize { get; set; } = 805306368;

        public int MaxWidth { get; set; } = 8192;

        public int MaxHeight { get; set; } = 8192;


        private static readonly int defaultMaxConcurrentLoadingImages = 3;

        private int _maxConcurrentLoadingImages = defaultMaxConcurrentLoadingImages;
        public int MaxConcurrentLoadingImages
        {
            get => _maxConcurrentLoadingImages;
            set
            {
                _maxConcurrentLoadingImages = value;
                loadingSemaphore = new AsyncSemaphore(value);
            }
        }



        private AsyncSemaphore loadingSemaphore = new(defaultMaxConcurrentLoadingImages);

        private volatile bool unloaded = false;

        private readonly ConcurrentDictionary<IFitsImageViewModel, AsyncSemaphore> imageSemaphores = new();
        private readonly ConcurrentDictionary<IFitsImageViewModel, bool> imageLoadedInternally = new();
        private readonly Dictionary<IFitsImageViewModel, List<IDisposable>> imageDisposables = new();

        private readonly List<CancellationTokenSource> loadingCts = new();

        private readonly IFitsImageManager manager;
        private readonly IFitsImageViewModel.IFactory fitsImageFactory;
        private readonly IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory;
        private readonly IFitsImageHistogramViewModel.IFactory fitsImageHistogramFactory;
        private readonly IStarSampler starSampler;


        private FitsImageViewerViewModel(IFitsImageManager manager, IFitsImageViewModel.IFactory fitsImageFactory, IFitsImageSectionViewerViewModel.IFactory fitsImageSectionFactory,
            IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory, IFitsImageHistogramViewModel.IFactory fitsImageHistogramFactory, IStarSampler starSampler, IAppConfig appConfig)
        {
            this.manager = manager;
            this.fitsImageFactory = fitsImageFactory;
            this.fitsImageStatisticsFactory = fitsImageStatisticsFactory;
            this.fitsImageHistogramFactory = fitsImageHistogramFactory;
            this.starSampler = starSampler;

            MaxInputSize = appConfig.MaxImageSize;
            MaxWidth = appConfig.MaxImageWidth;
            MaxHeight = appConfig.MaxImageHeight;

            var OnFitsImageChange = () => this.WhenAnyValue(x => x.FitsImage);
            OnFitsImageChange.Observe(LoadFitsImageAsync).WithExceptionHandler(ex =>
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);
            }).Subscribe();

            var OnFileChange = () => this.WhenAnyValue(x => x.File);
            OnFileChange.Observe(LoadFitsImageFromFileAsync).WithExceptionHandler(ex =>
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);
            }).Subscribe();

            var OnShowPhotometryChange = () => this.WhenAnyValue(x => x.ShowPhotometry);
            OnShowPhotometryChange.Observe(CheckStatisticsAndPhotometryAsync).WithExceptionHandler(ex =>
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);
            }).Subscribe();

            var OnAutoCalculateStatisticsChange = () => this.WhenAnyValue(x => x.AutoCalculateStatistics);
            OnAutoCalculateStatisticsChange.Observe(CheckStatisticsAndPhotometryAsync).WithExceptionHandler(ex =>
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);
            }).Subscribe();

            this.WhenAnyValue(x => x.FitsImage).Subscribe(_ =>
            {
                Statistics = null;
                Photometry.Clear();
                Histogram = null;
                StretchedHistogram = null;
            });

            var hasPeekView = Observable.CombineLatest(
                this.WhenAnyValue(x => x.FitsImage),
                this.WhenAnyValue(x => x.IsPeekViewerEnabled),
                (i, e) => i != null && e);

            hasPeekView.Subscribe(x =>
            {
                if (x && FitsImage != null)
                {
                    PeekViewer = fitsImageSectionFactory.Create(FitsImage);
                }
                else
                {
                    PeekViewer = null;
                }
            });

            SetInterpolationMode = ReactiveCommand.Create<BitmapInterpolationMode>(mode =>
            {
                var image = FitsImage;
                if (image != null)
                {
                    image.InterpolationMode = mode;
                }
            });

            ResetStretch = ReactiveCommand.CreateFromTask(async () =>
            {
                KeepStretch = false;
                try
                {
                    var image = FitsImage;
                    if (image != null)
                    {
                        await QueueImageTaskAsync(image, async (i, ct) =>
                        {
                            if (i.IsImageDataValid)
                            {
                                await i.ResetStretchParameters.Execute();
                                await i.UpdateOrCreateBitmapAsync(false, ct);
                            }
                            return Unit.Default;
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    // Don't care, image no longer used
                }
            });

            ApplyStretchToAll = ReactiveCommand.Create(() =>
            {
                KeepStretch = true;
            });

            CalculateStatistics = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    var image = FitsImage;
                    if (image != null)
                    {
                        using (var cts = new CancellationTokenSource())
                        {
                            loadingCts.Add(cts);

                            try
                            {
                                await QueueImageTaskAsync(image, async (i, ct) =>
                                {
                                    if (i.IsImageDataValid)
                                    {
                                        ct.ThrowIfCancellationRequested();

                                        var record = manager.Get(i.File);

                                        var stats = record?.Statistics;
                                        var photometry = record?.Photometry;

                                        if ((record != null && record.IsOutdated) || stats == null || photometry == null)
                                        {
                                            // If either one is null or outdated, statistics need to be calculated
                                            photometry = null;
                                            stats = await i.CalculateStatistics.Execute();
                                        }

                                        ct.ThrowIfCancellationRequested();

                                        if (stats != null)
                                        {
                                            using (DelayChangeNotifications())
                                            {
                                                if (record == null)
                                                {
                                                    record = manager.GetOrAdd(i.File);
                                                }

                                                Photometry.Clear();
                                                if (photometry != null)
                                                {
                                                    if (ShowPhotometry) SetShownPhotometry(photometry);
                                                }
                                                else
                                                {
                                                    photometry = await i.CalculatePhotometry.Execute();
                                                    if (ShowPhotometry) SetShownPhotometry(photometry);
                                                    record.Photometry = photometry;
                                                }

                                                // Update stars count of statistics
                                                Statistics = fitsImageStatisticsFactory.Create(stats, photometry.Count());
                                                record.Statistics = Statistics;

                                                record.IsOutdated = false;
                                            }
                                        }
                                    }
                                    return Unit.Default;
                                }, default, cts.Token);
                            }
                            finally
                            {
                                loadingCts.Remove(cts);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Don't care, image no longer used
                }
            });

            CalculateStatisticsWithProgress = ReactiveCommand.CreateFromTask(async () =>
            {
                var image = FitsImage;
                if (image != null)
                {
                    var vm = await image.CalculateStatisticsWithProgress.Execute();

                    vm.HookResultTask(async task =>
                    {
                        try
                        {
                            return await QueueImageTaskAsync(image, async (i, ct) =>
                            {
                                var record = manager.Get(i.File);

                                var statistics = record?.Statistics;
                                var photometry = record?.Photometry;

                                if ((record == null || !record.IsOutdated) && statistics != null && photometry != null && FitsImage == i)
                                {
                                    using (DelayChangeNotifications())
                                    {
                                        Photometry.Clear();
                                        if (ShowPhotometry) SetShownPhotometry(photometry);

                                        Statistics = statistics;
                                    }

                                    return vm.CreateCompletion(statistics);
                                }

                                if (i.IsImageDataValid)
                                {
                                    var result = await task.Invoke();

                                    if (result.Status == ResultStatus.Completed)
                                    {
                                        var stats = result.Value;

                                        if (stats != null && FitsImage == i)
                                        {
                                            if (record == null)
                                            {
                                                record = manager.GetOrAdd(i.File);
                                            }

                                            photometry = await image.CalculatePhotometry.Execute();
                                            Photometry.Clear();
                                            if (ShowPhotometry) SetShownPhotometry(photometry);
                                            record.Photometry = photometry;

                                            // Update stars count of statistics
                                            stats = fitsImageStatisticsFactory.Create(stats, photometry.Count());
                                            Statistics = stats;
                                            record.Statistics = Statistics;

                                            record.IsOutdated = false;
                                        }
                                    }

                                    return result;
                                }

                                return vm.CreateCancellation(null);
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            // Don't care, image no longer used
                        }

                        return vm.CreateCancellation(null);
                    });

                    vm.HookInternalTask(async task =>
                    {
                        using var cts = new CancellationTokenSource();
                        var ct = cts.Token;

                        loadingCts.Add(cts);

                        try
                        {
                            if (image.IsImageDataValid)
                            {
                                using (ct.Register(() => vm.SetCancelling()))
                                {
                                    ct.ThrowIfCancellationRequested();

                                    var result = await task.Invoke();

                                    if (result.Status == ResultStatus.Completed)
                                    {
                                        ct.ThrowIfCancellationRequested();

                                        var stats = result.Value;

                                        if (stats.HasValue)
                                        {
                                            return vm.CreateInternalCompletion(stats.Value);
                                        }
                                    }
                                }
                            }

                            return vm.CreateInternalCancellation(null);
                        }
                        catch (OperationCanceledException)
                        {
                            // Don't care, image no longer used
                        }
                        finally
                        {
                            loadingCts.Remove(cts);
                        }

                        return vm.CreateInternalCancellation(null);
                    });

                    return vm;
                }

                return null;
            });

            CalculateStatisticsWithProgressDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var vm = await CalculateStatisticsWithProgress.Execute();
                if (vm != null)
                {
                    await CalculateStatisticsProgressDialog.Handle(vm);
                }
            });

            LoadImage = ReactiveCommand.CreateFromTask<string?, IFitsImageViewModel?>(async file =>
            {
                try
                {
                    return await LoadFitsImageFromFileAsync(file);
                }
                catch (OperationCanceledException)
                {
                    // Don't care, image no longer used
                }
                return null;
            });

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, FitsImageViewerViewModel>(manager, nameof(manager.RecordChanged), OnRecordChanged);
        }

        private void SetShownPhotometry(IEnumerable<IFitsImagePhotometryViewModel> photometry)
        {
            Photometry.Clear();
            Photometry.AddRange(starSampler.Sample(photometry, MaxShownPhotometry, out var isSubset));
            IsShownPhotometryIncomplete = isSubset;
        }

        public async void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.File.Equals(File))
            {
                if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Statistics || args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Photometry)
                {
                    var record = manager.Get(args.File);

                    var stats = record?.Statistics;
                    if (stats != null)
                    {
                        Statistics = stats;
                    }

                    var photometry = record?.Photometry;
                    if (photometry != null)
                    {
                        Photometry.Clear();
                        if (ShowPhotometry) SetShownPhotometry(photometry);
                    }
                }
                else if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Outdated)
                {
                    var image = FitsImage;
                    if (image != null)
                    {
                        image.InvalidateStatisticsAndPhotometry = true;
                    }

                    await CheckStatisticsAndPhotometryAsync(false);
                }
            }
        }

        private async Task<Unit> CheckStatisticsAndPhotometryAsync(bool _, CancellationToken cancel = default)
        {
            var image = FitsImage;
            var record = image != null ? manager.Get(image.File) : null;

            if (image != null && image.IsImageDataValid && (ShowPhotometry || AutoCalculateStatistics) && ((record != null && record.IsOutdated) || Statistics == null || Photometry.Count == 0))
            {
                using (var cts = new CancellationTokenSource())
                {
                    loadingCts.Add(cts);

                    try
                    {
                        await QueueImageTaskAsync(image, async (i, ct) =>
                        {
                            if (i == FitsImage && i.IsImageDataValid)
                            {
                                record = manager.Get(i.File);

                                var stats = record?.Statistics;
                                var photometry = record?.Photometry;

                                bool needsStatistics;
                                bool needsPhotometry;

                                if (record != null && record.IsOutdated)
                                {
                                    needsStatistics = needsPhotometry = true;
                                }
                                else
                                {
                                    needsStatistics = AutoCalculateStatistics && Statistics == null;
                                    needsPhotometry = ShowPhotometry && (photometry != null || AutoCalculateStatistics) && Photometry.Count == 0;
                                }

                                if ((needsStatistics || needsPhotometry) && ((record != null && record.IsOutdated) || stats == null || photometry == null))
                                {
                                    ct.ThrowIfCancellationRequested();

                                    stats = await i.CalculateStatistics.Execute();

                                    ct.ThrowIfCancellationRequested();

                                    if (stats != null)
                                    {
                                        photometry = await i.CalculatePhotometry.Execute();

                                        // Update stars count of statistics
                                        stats = fitsImageStatisticsFactory.Create(stats, photometry.Count());
                                    }
                                }

                                ct.ThrowIfCancellationRequested();

                                if (i == FitsImage)
                                {
                                    if (needsPhotometry)
                                    {
                                        Photometry.Clear();
                                        if (photometry != null)
                                        {
                                            if (record == null)
                                            {
                                                record = manager.GetOrAdd(i.File);
                                            }
                                            SetShownPhotometry(photometry);
                                            record.Photometry = photometry;
                                        }
                                    }

                                    if (needsStatistics)
                                    {
                                        Statistics = stats;
                                        if (stats != null)
                                        {
                                            if (record == null)
                                            {
                                                record = manager.GetOrAdd(i.File);
                                            }
                                            record.Statistics = stats;
                                        }
                                    }

                                    if (record != null)
                                    {
                                        record.IsOutdated = false;
                                    }
                                }
                            }

                            return Unit.Default;
                        }, default, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Don't care, image no longer used
                    }
                    finally
                    {
                        loadingCts.Remove(cts);
                    }
                }
            }

            return Unit.Default;
        }

        private async Task<IFitsImageViewModel?> LoadFitsImageFromFileAsync(string? file, CancellationToken cancel = default)
        {
            bool queueDisposal = true;
            var previousImage = FitsImage;
            IFitsImageViewModel? newImage = null;
            if (file != null)
            {
                using (var cts = new CancellationTokenSource())
                {
                    CancelLoadingTasks();

                    loadingCts.Add(cts);

                    try
                    {
                        using (await loadingSemaphore.EnterAsync(cts.Token))
                        {
                            if (loadingSemaphore.CurrentCount == 0)
                            {
                                // Ensures that we don't use up too much memory when
                                // images are loaded in quick succession
                                await QueueOldImagesDisposalAsync(previousImage);
                                queueDisposal = false;
                            }

                            newImage = await Task.Run(() =>
                            {
                                var i = fitsImageFactory.Create(file, MaxInputSize, MaxWidth, MaxHeight);
                                i.Owner = this;
                                return i;
                            });

                            if (previousImage != null)
                            {
                                newImage.InterpolationMode = previousImage.InterpolationMode;
                                newImage.PreserveColorBalance = previousImage.PreserveColorBalance;
                                if (KeepStretch)
                                {
                                    newImage.Shadows = previousImage.Shadows;
                                    newImage.Midtones = previousImage.Midtones;
                                    newImage.Highlights = previousImage.Highlights;
                                }
                            }

                            imageSemaphores.TryAdd(newImage, new AsyncSemaphore(1));
                            imageLoadedInternally.AddOrUpdate(newImage, true, (_, _) => true);

                            await QueueImageTaskAsync(newImage, async (i, ct) =>
                            {
                                await i.UpdateOrCreateBitmapAsync(false, ct);
                                ct.ThrowIfCancellationRequested();
                                FitsImage = i;
                                return Unit.Default;
                            }, default, cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Don't care, image no longer used
                    }
                    finally
                    {
                        loadingCts.Remove(cts);

                        if (newImage != null)
                        {
                            await AddImageDisposableAsync(newImage, newImage);
                        }
                    }
                }
            }

            if (newImage != null)
            {
                if (queueDisposal)
                {
                    if (previousImage != null)
                    {
                        await QueueOldImagesDisposalAsync(newImage, previousImage);
                    }
                    else
                    {
                        await QueueOldImagesDisposalAsync(newImage);
                    }
                }
            }
            else
            {
                FitsImage = null;
            }

            return newImage;
        }

        private void CancelLoadingTasks()
        {
            foreach (var cts in loadingCts)
            {
                try
                {
                    cts.Cancel();
                }
                catch (Exception)
                {
                }
            }
            loadingCts.Clear();
        }

        private async Task<Unit> LoadFitsImageAsync(IFitsImageViewModel? image, CancellationToken cancel = default)
        {
            bool cleanup = false;

            if (image != null)
            {
                if (!imageSemaphores.ContainsKey(image))
                {
                    File = image.File;
                }

                if (!imageLoadedInternally.GetValueOrDefault(image, false))
                {
                    // If no semaphore was available yet and owner != this, then
                    // the image was not added through LoadFitsImageFromFileAsync
                    // => should clean up old images
                    cleanup = imageSemaphores.TryAdd(image, new AsyncSemaphore(1));
                }

                await AddImageDisposableAsync(image,
                    image.WhenAnyValue(x => x.PreserveColorBalance)
                        .Skip(1)
                        .Select(x => image)
                        .SelectMany(UpdateBitmapAsync)
                        .Subscribe()
                        );

                await AddImageDisposableAsync(image,
                    image.WhenAnyValue(x => x.Midtones)
                        .Skip(1)
                        .Throttle(TimeSpan.FromMilliseconds(100))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Select(x => image)
                        .SelectMany(UpdateBitmapAsync)
                        .Subscribe()
                        );

                await AddImageDisposableAsync(image,
                    image.WhenAnyValue(x => x.Shadows)
                        .Skip(1)
                        .Throttle(TimeSpan.FromMilliseconds(100))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Select(x => image)
                        .SelectMany(UpdateBitmapAsync)
                        .Subscribe()
                        );

                await AddImageDisposableAsync(image,
                    image.WhenAnyValue(x => x.Highlights)
                        .Skip(1)
                        .Throttle(TimeSpan.FromMilliseconds(100))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Select(x => image)
                        .SelectMany(UpdateBitmapAsync)
                        .Subscribe()
                        );

                await AddImageDisposableAsync(image,
                    image.WhenAnyValue(x => x.Histogram)
                        .Subscribe(x =>
                        {
                            if (image == FitsImage)
                            {
                                if (x != null)
                                {
                                    Histogram = fitsImageHistogramFactory.Create(x, true, 0.01f);
                                }
                                else
                                {
                                    Histogram = null;
                                }
                            }
                        })
                        );

                await AddImageDisposableAsync(image,
                    image.WhenAnyValue(x => x.StretchedHistogram)
                        .Subscribe(x =>
                        {
                            if (image == FitsImage)
                            {
                                if (x != null)
                                {
                                    StretchedHistogram = fitsImageHistogramFactory.Create(x, false, 0.01f);
                                }
                                else
                                {
                                    StretchedHistogram = null;
                                }
                            }
                        })
                        );

                // Adding the image as disposable here even if it may not have
                // been loaded by the viewer, because the viewer checks the owner
                // before the actual disposal
                await AddImageDisposableAsync(image, image);
            }
            else
            {
                cleanup = true;
            }

            if (cleanup)
            {
                await QueueOldImagesDisposalAsync(image);
            }

            // Schedule statistics calculation after cleanup so it doesn't accidentally
            // dispose images loaded after this one before statistics calculation has finished
            if (image != null && image.IsImageDataValid)
            {
                using (var cts = new CancellationTokenSource())
                {
                    loadingCts.Add(cts);
                    try
                    {
                        await QueueImageTaskAsync(image, async (i, ct) =>
                        {
                            if (i.IsImageDataValid && FitsImage == i)
                            {
                                ct.ThrowIfCancellationRequested();

                                var record = manager.Get(i.File);

                                var stats = record?.Statistics;
                                var photometry = record?.Photometry;

                                if ((record == null || !record.IsOutdated) && stats != null && photometry != null)
                                {
                                    // If everything is already cached it can be displayed
                                    // right away
                                    using (DelayChangeNotifications())
                                    {
                                        Photometry.Clear();
                                        if (ShowPhotometry) SetShownPhotometry(photometry);
                                        Statistics = fitsImageStatisticsFactory.Create(stats, photometry.Count());
                                    }
                                    return Unit.Default;
                                }

                                if (AutoCalculateStatistics)
                                {
                                    if ((record != null && record.IsOutdated) || stats == null || photometry == null)
                                    {
                                        // If either one is null or outdated, statistics need to be calculated
                                        photometry = null;
                                        stats = await i.CalculateStatistics.Execute();
                                    }

                                    ct.ThrowIfCancellationRequested();

                                    if (stats != null && FitsImage == i)
                                    {
                                        using (DelayChangeNotifications())
                                        {
                                            photometry = await i.CalculatePhotometry.Execute();

                                            if (FitsImage == i)
                                            {
                                                if (record == null)
                                                {
                                                    record = manager.GetOrAdd(i.File);
                                                }

                                                Photometry.Clear();
                                                if (ShowPhotometry) SetShownPhotometry(photometry);
                                                record.Photometry = photometry;

                                                // Update stars count of statistics
                                                stats = fitsImageStatisticsFactory.Create(stats, photometry.Count());
                                                Statistics = stats;
                                                record.Statistics = stats;

                                                record.IsOutdated = false;
                                            }
                                        }
                                    }
                                }
                            }
                            return Unit.Default;
                        }, default, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Don't care, image no longer used
                    }
                    finally
                    {
                        loadingCts.Remove(cts);
                    }
                }
            }

            return Unit.Default;
        }

        private async Task<Unit> UpdateBitmapAsync(IFitsImageViewModel image, CancellationToken cancel = default)
        {
            return image == FitsImage ? await UpdateBitmapAsync(image, false, cancel) : Unit.Default;
        }

        private async Task<Unit> UpdateBitmapAsync(IFitsImageViewModel image, bool disposeBeforeSwap, CancellationToken cancel = default)
        {
            await QueueImageTaskAsync(image, async (i, ct) =>
            {
                if (i.IsImageDataValid)
                {
                    await i.UpdateOrCreateBitmapAsync(disposeBeforeSwap, ct);
                }
                return Unit.Default;
            });
            return Unit.Default;
        }

        private async Task<T?> QueueImageTaskAsync<T>(IFitsImageViewModel image, Func<IFitsImageViewModel, CancellationToken, Task<T?>> task, T? defaultVal = default, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (imageSemaphores.TryGetValue(image, out var semaphore))
            {
                IDisposable? releaser = null;
                try
                {
                    try
                    {
                        releaser = await semaphore.EnterAsync(ct);
                    }
                    catch (ObjectDisposedException)
                    {
                        return defaultVal;
                    }

                    if (imageSemaphores.ContainsKey(image))
                    {
                        var result = await task.Invoke(image, ct);
                        ct.ThrowIfCancellationRequested();
                        return result;
                    }
                }
                finally
                {
                    releaser?.Dispose();
                }
            }

            return defaultVal;
        }

        private bool ShouldDispose(IDisposable disposable)
        {
            if (disposable is IFitsImageViewModel image)
            {
                // Only dispose an image if it is owned by this viewer
                return image.Owner == this;
            }
            return true;
        }

        private async Task<bool> AddImageDisposableAsync(IFitsImageViewModel image, IDisposable disposable)
        {
            bool result = await QueueImageTaskAsync(image, (i, ct) =>
            {
                if (unloaded)
                {
                    return Task.FromResult(false);
                }
                if (!imageDisposables.TryGetValue(image, out var disposables))
                {
                    imageDisposables.Add(image, disposables = new());
                }
                disposables.Add(disposable);
                return Task.FromResult(true);
            }, false);
            if (!result)
            {
                if (ShouldDispose(disposable))
                {
                    disposable.Dispose();
                }
            }
            return result;
        }

        private async Task QueueOldImagesDisposalAsync(params IFitsImageViewModel?[] except)
        {
            IFitsImageViewModel? image;
            while ((image = imageDisposables.Keys.LastOrDefault(i => !except.Contains(i), null)) != null)
            {
                await QueueImageDisposalAsync(image);
            }
        }

        private async Task QueueImageDisposalAsync(IFitsImageViewModel image)
        {
            await QueueImageTaskAsync(image, (i, ct) =>
            {
                if (imageDisposables.TryGetValue(image, out var disposables))
                {
                    foreach (var disposable in disposables)
                    {
                        if (ShouldDispose(disposable))
                        {
                            disposable.Dispose();
                        }
                    }
                }
                imageDisposables.Remove(image);
                imageSemaphores.Remove(image, out var semaphore);
                semaphore?.Dispose();
                imageLoadedInternally.Remove(image, out var _);
                return Task.FromResult(Unit.Default);
            });
        }

        public void Dispose()
        {
            FitsImage = null;
            File = null;

            foreach (var image in imageDisposables.Keys)
            {
                if (imageDisposables.TryGetValue(image, out var disposables))
                {
                    foreach (var disposable in disposables)
                    {
                        if (ShouldDispose(disposable))
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }
            imageDisposables.Clear();
        }

        public async Task UnloadAsync()
        {
            unloaded = true;

            FitsImage = null;
            File = null;

            CancelLoadingTasks();

            await QueueOldImagesDisposalAsync();
        }

        public void TransferPropertiesFrom(IFitsImageViewerViewModel viewer)
        {
            AutoCalculateStatistics = viewer.AutoCalculateStatistics;
            AutoSetInterpolationMode = viewer.AutoSetInterpolationMode;
            IsPeekViewerEnabled = viewer.IsPeekViewerEnabled;
            //KeepStretch = viewer.KeepStretch;
            KeepStretch = true;
            MaxShownPhotometry = viewer.MaxShownPhotometry;
            ShowPhotometry = viewer.ShowPhotometry;
            ShowPhotometryMeasurements = viewer.ShowPhotometryMeasurements;
            PeekViewerSize = viewer.PeekViewerSize;
        }
    }
}
