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

using Avalonia.Media.Imaging;
using Avalonia.Visuals.Media.Imaging;
using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.Common.Services;
using FitsRatingTool.FitsLoader.Models;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageViewModel : ViewModelBase, IFitsImageViewModel, IFitsImageContainer
    {
        public class Factory : IFitsImageViewModel.IFactory
        {
            private readonly IFitsImageLoader imageLoader;
            private readonly IFitsImageHeaderRecordViewModel.IFactory fitsImageHeaderRecordFactory;
            private readonly IFitsImageManager fitsImageManager;
            private readonly IFitsImageStatisticsProgressViewModel.IFactory fitsImageStatisticsProgressFactory;
            private readonly IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory;
            private readonly IFitsImagePhotometryViewModel.IFactory fitsImagePhotometryFactory;
            private readonly IAppConfig appConfig;

            public Factory(IFitsImageLoader imageLoader, IFitsImageHeaderRecordViewModel.IFactory fitsImageHeaderRecordFactory, IFitsImageManager fitsImageManager,
                IFitsImageStatisticsProgressViewModel.IFactory fitsImageStatisticsProgressFactory, IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory,
                IFitsImagePhotometryViewModel.IFactory fitsImagePhotometryFactory, IAppConfig appConfig)
            {
                this.imageLoader = imageLoader;
                this.fitsImageHeaderRecordFactory = fitsImageHeaderRecordFactory;
                this.fitsImageManager = fitsImageManager;
                this.fitsImageStatisticsProgressFactory = fitsImageStatisticsProgressFactory;
                this.fitsImageStatisticsFactory = fitsImageStatisticsFactory;
                this.fitsImagePhotometryFactory = fitsImagePhotometryFactory;
                this.appConfig = appConfig;
            }

            public IFitsImageViewModel Create(string file)
            {
                return Create(file, appConfig.MaxImageSize, appConfig.MaxImageWidth, appConfig.MaxImageHeight);
            }

            public IFitsImageViewModel Create(string file, long maxInputSize = -1, int maxWidth = -1, int maxHeight = -1)
            {
                return new FitsImageViewModel(imageLoader, fitsImageHeaderRecordFactory, fitsImageManager, fitsImageStatisticsProgressFactory, fitsImageStatisticsFactory, fitsImagePhotometryFactory, file,
                    maxInputSize < 0 ? appConfig.MaxImageSize : maxInputSize, maxWidth < 0 ? appConfig.MaxImageWidth : maxWidth, maxHeight < 0 ? appConfig.MaxImageHeight : maxHeight);
            }

            public IFitsImageViewModel Create(IFitsImage image)
            {
                return new FitsImageViewModel(fitsImageHeaderRecordFactory, fitsImageManager, fitsImageStatisticsProgressFactory, fitsImageStatisticsFactory, fitsImagePhotometryFactory, image);
            }
        }


        private bool _isAutoLoaded;
        public bool IsAutoLoaded
        {
            get => _isAutoLoaded;
            set => this.RaiseAndSetIfChanged(ref _isAutoLoaded, value);
        }

        //TODO Implement parameters for red/green/blue channels

        private bool _preserveColorBalance = true;
        public bool PreserveColorBalance
        {
            get => _preserveColorBalance;
            set => this.RaiseAndSetIfChanged(ref _preserveColorBalance, value);
        }

        private float _shadows;
        public float Shadows
        {
            get => _shadows;
            set => this.RaiseAndSetIfChanged(ref _shadows, value);
        }

        private float _midtones;
        public float Midtones
        {
            get => _midtones;
            set => this.RaiseAndSetIfChanged(ref _midtones, value);
        }

        private float _highlights;
        public float Highlights
        {
            get => _highlights;
            set => this.RaiseAndSetIfChanged(ref _highlights, value);
        }

        private Bitmap? _bitmap;
        public Bitmap? Bitmap
        {
            get => _bitmap;
            private set => this.RaiseAndSetIfChanged(ref _bitmap, value);
        }

        public bool HasImage
        {
            get => IsImageValid && Bitmap != null;
        }

        private uint[]? _histogram;
        public uint[]? Histogram
        {
            get => _histogram;
            private set => this.RaiseAndSetIfChanged(ref _histogram, value);
        }

        private uint[]? _stretchedHistogram;
        public uint[]? StretchedHistogram
        {
            get => _stretchedHistogram;
            private set => this.RaiseAndSetIfChanged(ref _stretchedHistogram, value);
        }

        private bool _isImageDataValid;
        public bool IsImageDataValid
        {
            get => _isImageDataValid;
            private set => this.RaiseAndSetIfChanged(ref _isImageDataValid, value);
        }

        private bool _isStatisticsValid;
        public bool IsStatisticsValid
        {
            get => _isStatisticsValid;
            private set => this.RaiseAndSetIfChanged(ref _isStatisticsValid, value);
        }

        private bool _isImageValid;
        public bool IsImageValid
        {
            get => _isImageValid;
            private set => this.RaiseAndSetIfChanged(ref _isImageValid, value);
        }

        private bool _invalidateStatisticsAndPhotometry;
        public bool InvalidateStatisticsAndPhotometry
        {
            get => _invalidateStatisticsAndPhotometry;
            set => this.RaiseAndSetIfChanged(ref _invalidateStatisticsAndPhotometry, value);
        }

        public string File { get; }

        public string FileName => Path.GetFileName(File);

        private bool _isUpdating;
        public bool IsUpdating
        {
            get => _isUpdating;
            private set { this.RaiseAndSetIfChanged(ref _isUpdating, value); }
        }

        private BitmapInterpolationMode _interpolationMode = BitmapInterpolationMode.HighQuality;
        public BitmapInterpolationMode InterpolationMode
        {
            get => _interpolationMode;
            set
            {
                if (_interpolationMode != value)
                {
                    this.RaiseAndSetIfChanged(ref _interpolationMode, value);
                    this.RaisePropertyChanged(nameof(IsNearestNeighbor));
                    this.RaisePropertyChanged(nameof(IsInterpolated));
                }
            }
        }

        public bool IsNearestNeighbor
        {
            get => InterpolationMode == BitmapInterpolationMode.Default;
            set
            {
                if (value)
                {
                    InterpolationMode = BitmapInterpolationMode.Default;
                }
                else
                {
                    InterpolationMode = BitmapInterpolationMode.HighQuality;
                }
            }
        }

        public bool IsInterpolated
        {
            get => !IsNearestNeighbor;
            set => IsNearestNeighbor = !value;
        }

        private readonly List<IFitsImageHeaderRecordViewModel> _header = new();
        public IReadOnlyList<IFitsImageHeaderRecordViewModel> Header { get => _header; }

        IEnumerable<IFitsImageHeaderRecord> IFitsImageMetadata.Header { get => _header; }

        public int ImageWidth => fitsImage.ImageWidth;

        public int ImageHeight => fitsImage.ImageHeight;


        private IFitsImageViewerViewModel? _owner;
        public IFitsImageViewerViewModel? Owner
        {
            get => _owner;
            set => this.RaiseAndSetIfChanged(ref _owner, value);
        }


        public ReactiveCommand<Unit, Unit> ResetStretchParameters { get; }

        public ReactiveCommand<Unit, IFitsImageStatisticsViewModel?> CalculateStatistics { get; }

        public ReactiveCommand<Unit, IFitsImageStatisticsProgressViewModel?> CalculateStatisticsWithProgress { get; }

        public ReactiveCommand<Unit, Unit> CalculateStatisticsWithProgressDialog { get; }

        public Interaction<IFitsImageStatisticsProgressViewModel, Unit> CalculateStatisticsProgressDialog { get; } = new();

        public ReactiveCommand<Unit, IEnumerable<IFitsImagePhotometryViewModel>?> CalculatePhotometry { get; }



        public ObservableCollection<IFitsImage> FitsImages { get; } = new();



        private readonly IFitsImage fitsImage;
        private readonly IDisposable fitsImageRef;
        private IDisposable? fitsImageContainerRegistration;

        private FitsImageLoaderParameters loaderParameters = new() { monoColorOutline = false, saturation = 1.0f };

        private ImageStretchParameters computedStretch;



        private readonly IFitsImageHeaderRecordViewModel.IFactory fitsImageHeaderRecordFactory;
        private readonly IFitsImageManager fitsImageManager;

        private FitsImageViewModel(IFitsImageHeaderRecordViewModel.IFactory fitsImageHeaderRecordFactory, IFitsImageManager fitsImageManager,
            IFitsImageStatisticsProgressViewModel.IFactory fitsImageStatisticsProgressFactory, IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory,
            IFitsImagePhotometryViewModel.IFactory fitsImagePhotometryFactory, string file)
        {
            this.fitsImageHeaderRecordFactory = fitsImageHeaderRecordFactory;
            this.fitsImageManager = fitsImageManager;

            fitsImage = null!; // Set in other constructors
            fitsImageRef = null!;
            File = file;

            ResetStretchParameters = ReactiveCommand.CreateFromTask(async () =>
            {
                var stretch = await Task.Run(() =>
                {
                    fitsImage.ComputeStretch(out var p);
                    return p;
                });
                SetStretchParameters(stretch);
            });

            CalculateStatistics = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsImageDataValid)
                {
                    return fitsImageStatisticsFactory.Create(await Task.Run(() =>
                    {
                        if (InvalidateStatisticsAndPhotometry || !fitsImage.GetStatistics(out var stats))
                        {
                            InvalidateStatisticsAndPhotometry = false;
                            fitsImage.ComputeStatisticsAndPhotometry();
                            fitsImage.GetStatistics(out stats);
                        }
                        return stats;
                    }), 0);
                }
                return null;
            });

            CalculateStatisticsWithProgress = ReactiveCommand.Create(() =>
            {
                if (IsImageDataValid)
                {
                    return fitsImageStatisticsProgressFactory.Create(callback => () => Task.Run(() =>
                    {
                        if (InvalidateStatisticsAndPhotometry || !fitsImage.GetStatistics(out var stats))
                        {
                            InvalidateStatisticsAndPhotometry = false;
                            fitsImage.ComputeStatisticsAndPhotometry(callback);
                            fitsImage.GetStatistics(out stats);
                        }
                        return (PhotometryStatistics?)stats;
                    }));
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

            CalculatePhotometry = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsImageDataValid)
                {
                    var list = new List<IFitsImagePhotometryViewModel>();
                    foreach (var photometry in await Task.Run(() =>
                    {
                        if (InvalidateStatisticsAndPhotometry || !fitsImage.GetPhotometry(out var photometry))
                        {
                            InvalidateStatisticsAndPhotometry = false;
                            fitsImage.ComputeStatisticsAndPhotometry();
                            fitsImage.GetPhotometry(out photometry);
                        }
                        return photometry ?? Array.Empty<PhotometryObject>();
                    }))
                    {
                        list.Add(fitsImagePhotometryFactory.Create(photometry));
                    }
                    return (IEnumerable<IFitsImagePhotometryViewModel>)list;
                }
                return null;
            });
        }

        private FitsImageViewModel(IFitsImageHeaderRecordViewModel.IFactory fitsImageHeaderRecordFactory, IFitsImageManager fitsImageManager,
            IFitsImageStatisticsProgressViewModel.IFactory fitsImageStatisticsProgressFactory, IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory,
            IFitsImagePhotometryViewModel.IFactory fitsImagePhotometryFactory, IFitsImage image)
            : this(fitsImageHeaderRecordFactory, fitsImageManager, fitsImageStatisticsProgressFactory, fitsImageStatisticsFactory, fitsImagePhotometryFactory, image.File)
        {
            fitsImage = image;
            fitsImageRef = image.Ref();

            if (!image.IsImageDataValid)
            {
                throw new Exception("Invalid FITS image data");
            }

            IsImageDataValid = true;

            Histogram = image.Histogram;

            Init();
        }

        private FitsImageViewModel(IFitsImageLoader imageLoader, IFitsImageHeaderRecordViewModel.IFactory fitsImageHeaderRecordFactory, IFitsImageManager fitsImageManager,
            IFitsImageStatisticsProgressViewModel.IFactory fitsImageStatisticsProgressFactory, IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory,
            IFitsImagePhotometryViewModel.IFactory fitsImagePhotometryFactory, string file, long maxInputSize, int maxWidth, int maxHeight)
            : this(fitsImageHeaderRecordFactory, fitsImageManager, fitsImageStatisticsProgressFactory, fitsImageStatisticsFactory, fitsImagePhotometryFactory, file)
        {
            fitsImage = imageLoader.LoadFit(file, maxInputSize, maxWidth, maxHeight)!;
            if (fitsImage == null)
            {
                throw new Exception($"Failed loading FITS '{file}'");
            }

            fitsImageRef = fitsImage.Ref();

            if (!fitsImage.LoadImageData(loaderParameters))
            {
                throw new Exception($"Failed loading FITS '{file}': Invalid image data");
            }

            IsImageDataValid = fitsImage.IsImageDataValid;

            Histogram = fitsImage.Histogram;

            Init();
        }

        private void Init()
        {
            CloseFile(); // Close file after initial loading. When data needs to be read again it'll be reopened.

            this.WhenAnyValue(x => x.Bitmap).Subscribe(x =>
            {
                this.RaisePropertyChanged(nameof(HasImage));
            });

            foreach (var record in fitsImage.Header.Values)
            {
                _header.Add(fitsImageHeaderRecordFactory.Create(record));
            }

            fitsImage.ComputeStretch(out var stretch);

            SetStretchParameters(stretch);

            FitsImages.Add(fitsImage);
            fitsImageContainerRegistration = fitsImageManager.RegisterImageContainer(this);
        }

        private void SetStretchParameters(ImageStretchParameters p)
        {
            computedStretch = p;
            Shadows = p.rk.shadows;
            Midtones = p.rk.midtones;
            Highlights = p.rk.highlights;
        }

        private ImageStretchParameters GetStretchParameters()
        {
            int maxInput = computedStretch.rk.max_input;

            float s;
            float m;
            float h;

            float shadowsFactor = Shadows / computedStretch.rk.shadows;
            float midtonesFactor = Midtones / computedStretch.rk.midtones;
            float highlightsFactor = Highlights / computedStretch.rk.highlights;

            if (PreserveColorBalance)
            {
                if (fitsImage.OutDim.Channels > 1)
                {
                    s = (computedStretch.rk.shadows * shadowsFactor + computedStretch.g.shadows * shadowsFactor + computedStretch.b.shadows * shadowsFactor) / 3.0f;
                    m = (computedStretch.rk.midtones * midtonesFactor + computedStretch.g.midtones * midtonesFactor + computedStretch.b.midtones * midtonesFactor) / 3.0f;
                    h = (computedStretch.rk.highlights * highlightsFactor + computedStretch.g.highlights * highlightsFactor + computedStretch.b.highlights * highlightsFactor) / 3.0f;
                }
                else
                {
                    s = Shadows;
                    m = Midtones;
                    h = Highlights;
                }

                return new ImageStretchParameters
                {
                    rk = new ChannelStretchParameters { max_input = maxInput, shadows = s, midtones = m, highlights = h },
                    g = new ChannelStretchParameters { max_input = maxInput, shadows = s, midtones = m, highlights = h },
                    b = new ChannelStretchParameters { max_input = maxInput, shadows = s, midtones = m, highlights = h }
                };
            }
            else
            {
                return new ImageStretchParameters
                {
                    rk = new ChannelStretchParameters
                    {
                        max_input = maxInput,
                        shadows = computedStretch.rk.shadows * shadowsFactor,
                        midtones = computedStretch.rk.midtones * midtonesFactor,
                        highlights = computedStretch.rk.highlights * highlightsFactor,
                    },
                    g = new ChannelStretchParameters
                    {
                        max_input = maxInput,
                        shadows = computedStretch.g.shadows * shadowsFactor,
                        midtones = computedStretch.g.midtones * midtonesFactor,
                        highlights = computedStretch.g.highlights * highlightsFactor,
                    },
                    b = new ChannelStretchParameters
                    {
                        max_input = maxInput,
                        shadows = computedStretch.b.shadows * shadowsFactor,
                        midtones = computedStretch.b.midtones * midtonesFactor,
                        highlights = computedStretch.b.highlights * highlightsFactor,
                    }
                };
            }
        }

        public void CloseFile()
        {
            fitsImage.CloseFile();
        }

        public Bitmap? UpdateOrCreateBitmap(bool disposeBeforeSwap = true)
        {
            IsUpdating = true;
            try
            {
                var oldBitmap = Bitmap;
                if (disposeBeforeSwap || !fitsImage.IsImageDataValid)
                {
                    Bitmap = null;
                    oldBitmap?.Dispose();
                    if (!fitsImage.IsImageDataValid)
                    {
                        return null;
                    }
                }
                FitsImageLoaderParameters loaderParameters = this.loaderParameters;
                loaderParameters.stretchParameters = GetStretchParameters();
                if (fitsImage.ProcessImage(false, loaderParameters, out var data) && data is NativeFitsImageData nativeData && fitsImage.IsImageValid)
                {
                    Bitmap = new Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Unpremul, nativeData.Ptr, new Avalonia.PixelSize(fitsImage.OutDim.Width, fitsImage.OutDim.Height), new Avalonia.Vector(96, 96), fitsImage.OutDim.Width * 4);
                    IsImageValid = true;
                }
                else
                {
                    Bitmap = null;
                    IsImageValid = false;
                }
                this.RaisePropertyChanged(nameof(HasImage));
                StretchedHistogram = null;
                StretchedHistogram = fitsImage.StretchedHistogram;
                if (!disposeBeforeSwap)
                {
                    oldBitmap?.Dispose();
                }
            }
            finally
            {
                IsUpdating = false;
            }
            return Bitmap;
        }

        public async Task<Bitmap?> UpdateOrCreateBitmapAsync(bool disposeBeforeSwap = true, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            IsUpdating = true;
            try
            {
                var oldBitmap = Bitmap;
                if (disposeBeforeSwap || !fitsImage.IsImageDataValid)
                {
                    Bitmap = null;
                    oldBitmap?.Dispose();
                    if (!fitsImage.IsImageDataValid)
                    {
                        return null;
                    }
                }
                FitsImageLoaderParameters loaderParameters = this.loaderParameters;
                loaderParameters.stretchParameters = GetStretchParameters();
                var task = Task.Run(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    if (fitsImage.ProcessImage(false, loaderParameters, out var data))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (data is NativeFitsImageData nativeData && fitsImage.IsImageValid)
                        {
                            return new Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Unpremul, nativeData.Ptr, new Avalonia.PixelSize(fitsImage.OutDim.Width, fitsImage.OutDim.Height), new Avalonia.Vector(96, 96), fitsImage.OutDim.Width * 4);
                        }
                    }
                    ct.ThrowIfCancellationRequested();
                    return null;
                });
                Bitmap = await task;
                IsImageValid = fitsImage.IsImageValid && Bitmap != null;
                this.RaisePropertyChanged(nameof(HasImage));
                StretchedHistogram = null;
                StretchedHistogram = fitsImage.StretchedHistogram;
                if (!disposeBeforeSwap)
                {
                    oldBitmap?.Dispose();
                }
            }
            finally
            {
                IsUpdating = false;
            }
            return Bitmap;
        }

        public void Dispose()
        {
            fitsImageContainerRegistration?.Dispose();
            fitsImageContainerRegistration = null;

            IsImageValid = false;

            var bitmap = Bitmap;
            Bitmap = null;
            bitmap?.Dispose();

            fitsImageRef?.Dispose();

            StretchedHistogram = null;
        }
    }
}
