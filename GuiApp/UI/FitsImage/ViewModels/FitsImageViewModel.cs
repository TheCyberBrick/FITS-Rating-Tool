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
using DryIocAttributes;
using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.Common.Services;
using FitsRatingTool.FitsLoader.Models;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    [Export(typeof(IFitsImageViewModel)), TransientReuse, AllowDisposableTransient]
    public class FitsImageViewModel : ViewModelBase, IFitsImageViewModel, IFitsImageContainer, IDisposable
    {
        public FitsImageViewModel(IRegistrar<IFitsImageViewModel, IFitsImageViewModel.OfFile> reg)
        {
            reg.RegisterAndReturn<FitsImageViewModel>();
        }

        public FitsImageViewModel(IRegistrar<IFitsImageViewModel, IFitsImageViewModel.OfImage> reg)
        {
            reg.RegisterAndReturn<FitsImageViewModel>();
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


        public ReactiveCommand<Unit, Unit> ResetStretchParameters { get; }

        public ReactiveCommand<Unit, IFitsImageStatisticsViewModel?> CalculateStatistics { get; }

        public ReactiveCommand<Unit, ITemplatedFactory<IFitsImageStatisticsProgressViewModel, IFitsImageStatisticsProgressViewModel.OfTaskFunc>?> CalculateStatisticsWithProgress { get; }

        public ReactiveCommand<Unit, Unit> CalculateStatisticsWithProgressDialog { get; }

        public Interaction<IFitsImageStatisticsProgressViewModel, Unit> CalculateStatisticsProgressDialog { get; } = new();

        public ReactiveCommand<Unit, IEnumerable<IFitsImagePhotometryViewModel>?> CalculatePhotometry { get; }



        public ObservableCollection<IFitsImage> FitsImages { get; } = new();



        private readonly IFitsImage fitsImage;
        private readonly IDisposable fitsImageRef;
        private IDisposable? fitsImageContainerRegistration;

        private FitsImageLoaderParameters loaderParameters = new() { monoColorOutline = false, saturation = 1.0f };

        private ImageStretchParameters computedStretch;

        private bool disposed;


        private readonly IFitsImageManager fitsImageManager;

        private FitsImageViewModel(string file,
            IFitsImageManager fitsImageManager,
            IContainer<IFitsImageStatisticsProgressViewModel, IFitsImageStatisticsProgressViewModel.OfTaskFunc> fitsImageStatisticsProgressContainer,
            IFactoryRoot<IFitsImageStatisticsProgressViewModel, IFitsImageStatisticsProgressViewModel.OfTaskFunc> fitsImageStatisticsProgressFactory)
        {
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
                    return (IFitsImageStatisticsViewModel)new FitsImageStatisticsViewModel(new IFitsImageStatisticsViewModel.OfStatistics(await Task.Run(() =>
                    {
                        if (InvalidateStatisticsAndPhotometry || !fitsImage.GetStatistics(out var stats))
                        {
                            InvalidateStatisticsAndPhotometry = false;
                            fitsImage.ComputeStatisticsAndPhotometry();
                            fitsImage.GetStatistics(out stats);
                        }
                        return stats;
                    }), 0));
                }
                return null;
            });

            CalculateStatisticsWithProgress = ReactiveCommand.Create(() =>
            {
                if (IsImageDataValid)
                {
                    return fitsImageStatisticsProgressFactory.Templated(new IFitsImageStatisticsProgressViewModel.OfTaskFunc(callback => () => Task.Run(() =>
                    {
                        if (InvalidateStatisticsAndPhotometry || !fitsImage.GetStatistics(out var stats))
                        {
                            InvalidateStatisticsAndPhotometry = false;
                            fitsImage.ComputeStatisticsAndPhotometry(callback);
                            fitsImage.GetStatistics(out stats);
                        }
                        return (PhotometryStatistics?)stats;
                    })));
                }
                return null;
            });

            CalculateStatisticsWithProgressDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var factory = await CalculateStatisticsWithProgress.Execute();
                if (factory != null)
                {
                    await factory.DoAsync(fitsImageStatisticsProgressContainer, async vm =>
                    {
                        await CalculateStatisticsProgressDialog.Handle(vm);
                    });
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
                        list.Add(new FitsImagePhotometryViewModel(new IFitsImagePhotometryViewModel.OfPhotometry(photometry)));
                    }
                    return (IEnumerable<IFitsImagePhotometryViewModel>)list;
                }
                return null;
            });
        }

        private FitsImageViewModel(IFitsImageViewModel.OfImage args,
            IFitsImageManager fitsImageManager,
            IContainer<IFitsImageStatisticsProgressViewModel, IFitsImageStatisticsProgressViewModel.OfTaskFunc> fitsImageStatisticsProgressContainer,
            IFactoryRoot<IFitsImageStatisticsProgressViewModel, IFitsImageStatisticsProgressViewModel.OfTaskFunc> fitsImageStatisticsProgressFactory)
            : this(args.Image.File, fitsImageManager, fitsImageStatisticsProgressContainer, fitsImageStatisticsProgressFactory)
        {
            fitsImage = args.Image;
            fitsImageRef = args.Image.Ref();

            if (!args.Image.IsImageDataValid)
            {
                throw new Exception("Invalid FITS image data");
            }

            IsImageDataValid = true;

            Histogram = args.Image.Histogram;

            Init();
        }

        private FitsImageViewModel(IFitsImageViewModel.OfFile args,
            IFitsImageLoader imageLoader,
            IFitsImageManager fitsImageManager,
            IAppConfig appConfig,
            IContainer<IFitsImageStatisticsProgressViewModel, IFitsImageStatisticsProgressViewModel.OfTaskFunc> fitsImageStatisticsProgressContainer,
            IFactoryRoot<IFitsImageStatisticsProgressViewModel, IFitsImageStatisticsProgressViewModel.OfTaskFunc> fitsImageStatisticsProgressFactory)
            : this(args.File, fitsImageManager, fitsImageStatisticsProgressContainer, fitsImageStatisticsProgressFactory)
        {
            fitsImage = imageLoader.LoadFit(args.File, args.MaxInputSize ?? appConfig.MaxImageSize, args.MaxWidth ?? appConfig.MaxImageWidth, args.MaxHeight ?? appConfig.MaxImageHeight)!;
            if (fitsImage == null)
            {
                throw new Exception($"Failed loading FITS '{args.File}'");
            }

            fitsImageRef = fitsImage.Ref();

            if (!fitsImage.LoadImageData(loaderParameters))
            {
                throw new Exception($"Failed loading FITS '{args.File}': Invalid image data");
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
                _header.Add(new FitsImageHeaderRecordViewModel(new IFitsImageHeaderRecordViewModel.OfRecord(record)));
            }

            fitsImage.ComputeStretch(out var stretch);

            SetStretchParameters(stretch);

            FitsImages.Add(fitsImage);

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                lock (this)
                {
                    if (!disposed)
                    {
                        fitsImageContainerRegistration = fitsImageManager.RegisterImageContainer(this);
                    }
                }
            });
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

                var newBitmap = fitsImage.WithLock(() =>
                {
                    if (fitsImage.ProcessImage(false, loaderParameters, out var data) && data is NativeFitsImageData nativeData && fitsImage.IsImageValid)
                    {
                        return new Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Unpremul, nativeData.Ptr, new Avalonia.PixelSize(fitsImage.OutDim.Width, fitsImage.OutDim.Height), new Avalonia.Vector(96, 96), fitsImage.OutDim.Width * 4);
                    }
                    return null;
                });

                Bitmap = newBitmap;

                try
                {
                    IsImageValid = fitsImage.IsImageValid && newBitmap != null;

                    this.RaisePropertyChanged(nameof(HasImage));

                    StretchedHistogram = null;
                    StretchedHistogram = fitsImage.StretchedHistogram;
                }
                finally
                {
                    if (!disposeBeforeSwap)
                    {
                        oldBitmap?.Dispose();
                    }
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

                var newBitmap = await Task.Run(() =>
                {
                    ct.ThrowIfCancellationRequested();

                    return fitsImage.WithLock(() =>
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

                        return null;
                    });
                });

                Bitmap = newBitmap;

                try
                {
                    IsImageValid = fitsImage.IsImageValid && newBitmap != null;

                    this.RaisePropertyChanged(nameof(HasImage));

                    StretchedHistogram = null;
                    StretchedHistogram = fitsImage.StretchedHistogram;
                }
                finally
                {
                    if (!disposeBeforeSwap)
                    {
                        oldBitmap?.Dispose();
                    }
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
            lock (this)
            {
                disposed = true;

                fitsImageContainerRegistration?.Dispose();
                fitsImageContainerRegistration = null;
            }

            IsImageValid = false;

            var bitmap = Bitmap;
            Bitmap = null;
            bitmap?.Dispose();

            fitsImageRef?.Dispose();

            StretchedHistogram = null;
        }
    }
}
