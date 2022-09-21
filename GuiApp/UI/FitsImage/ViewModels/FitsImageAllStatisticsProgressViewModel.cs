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
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Progress;
using FitsRatingTool.GuiApp.UI.Progress.ViewModels;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageAllStatisticsProgressViewModel : SimpleProgressViewModel<Dictionary<string, IFitsImageStatisticsViewModel?>, FitsImageAllStatisticsProgress>, IFitsImageAllStatisticsProgressViewModel
    {
        public class Factory : IFitsImageAllStatisticsProgressViewModel.IFactory
        {
            private readonly IFitsImageManager manager;
            private readonly IFitsImageViewModel.IFactory fitsImageFactory;
            private readonly IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory;
            private readonly IAppConfig appConfig;

            public Factory(IFitsImageManager manager, IFitsImageViewModel.IFactory fitsImageFactory, IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory, IAppConfig appConfig)
            {
                this.manager = manager;
                this.fitsImageFactory = fitsImageFactory;
                this.fitsImageStatisticsFactory = fitsImageStatisticsFactory;
                this.appConfig = appConfig;
            }

            public IFitsImageAllStatisticsProgressViewModel Create(IEnumerable<string> images, bool useRepository)
            {
                return new FitsImageAllStatisticsProgressViewModel(manager, fitsImageFactory, fitsImageStatisticsFactory, appConfig, useRepository, images);
            }
        }

        private int _numberOfImages;
        public int NumberOfImages
        {
            get => _numberOfImages;
            private set => this.RaiseAndSetIfChanged(ref _numberOfImages, value);
        }

        private int _currentImage;
        public int CurrentImage
        {
            get => _currentImage;
            private set => this.RaiseAndSetIfChanged(ref _currentImage, value);
        }

        private string _currentImageFile = "";
        public string CurrentImageFile
        {
            get => _currentImageFile;
            private set => this.RaiseAndSetIfChanged(ref _currentImageFile, value);
        }

        private string _currentImageFileName = "";
        public string CurrentImageFileName
        {
            get => _currentImageFileName;
            private set => this.RaiseAndSetIfChanged(ref _currentImageFileName, value);
        }

        private int _numberOfObjects;
        public int NumberOfObjects
        {
            get => _numberOfObjects;
            private set => this.RaiseAndSetIfChanged(ref _numberOfObjects, value);
        }

        private int _currentObject;
        public int CurrentObject
        {
            get => _currentObject;
            private set => this.RaiseAndSetIfChanged(ref _currentObject, value);
        }

        private int _numberOfStars;
        public int NumberOfStars
        {
            get => _numberOfStars;
            private set => this.RaiseAndSetIfChanged(ref _numberOfStars, value);
        }

        private float _progress;
        public float ProgressValue
        {
            get => _progress;
            private set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        private string _phase = "";
        public string Phase
        {
            get => _phase;
            private set => this.RaiseAndSetIfChanged(ref _phase, value);
        }


        private readonly List<string> images = new();


        private readonly Dictionary<string, IFitsImageStatisticsViewModel?> results = new();

        private IFitsImageStatisticsProgressViewModel? currentProgressViewModel = null;

        private readonly IFitsImageManager manager;
        private readonly IFitsImageViewModel.IFactory fitsImageFactory;
        private readonly IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory;
        private readonly IAppConfig appConfig;

        private readonly bool useRepository;

        private FitsImageAllStatisticsProgressViewModel(IFitsImageManager manager, IFitsImageViewModel.IFactory fitsImageFactory, IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory,
            IAppConfig appConfig, bool useRepository, IEnumerable<string> images) : base(null)
        {
            this.manager = manager;
            this.fitsImageFactory = fitsImageFactory;
            this.fitsImageStatisticsFactory = fitsImageStatisticsFactory;
            this.appConfig = appConfig;
            this.useRepository = useRepository;
            this.images.AddRange(images);
        }

        protected override Func<Task<Result<Dictionary<string, IFitsImageStatisticsViewModel?>>>> CreateTask(ProgressSynchronizationContext synchronizationContext)
        {
            return async () =>
            {
                int nimages = images.Count;
                int iimage = 0;

                float progressPerImage = 1.0f / nimages;

                foreach (var image in images)
                {
                    if (IsCancelling)
                    {
                        break;
                    }

                    if (useRepository)
                    {
                        var record = manager.Get(image);

                        var statistics = manager.Get(image)?.Statistics;

                        if (statistics != null && (record == null || !record.IsOutdated))
                        {
                            results.Add(image, statistics);
                            iimage++;
                            continue;
                        }
                    }

                    var imagevm = await Task.Run(() => fitsImageFactory.Create(image, appConfig.MaxImageSize, appConfig.MaxImageWidth, appConfig.MaxImageHeight));

                    try
                    {
                        var vm = await imagevm.CalculateStatisticsWithProgress.Execute();

                        if (vm != null)
                        {
                            currentProgressViewModel = vm;

                            Progress<FitsImageStatisticsProgress> progress = new();
                            void progressHandler(object? sender, FitsImageStatisticsProgress value)
                            {
                                // Delegate progress to self
                                ReportProgress(new FitsImageAllStatisticsProgress
                                {
                                    numberOfImages = nimages,
                                    currentImage = iimage,
                                    currentImageFile = imagevm.File,
                                    numberOfObjects = value.numberOfObjects,
                                    currentObject = value.currentObject,
                                    numberOfStars = value.numberOfStars,
                                    progress = (iimage + value.progress) * progressPerImage,
                                    phase = value.phase
                                });
                            }
                            progress.ProgressChanged += progressHandler;

                            vm.Progress = progress;

                            var result = await vm.Run.Execute();

                            if (result.Status != ResultStatus.Cancelled)
                            {
                                var stats = result.Value;

                                if (stats != null)
                                {
                                    results.Add(image, stats);

                                    if (useRepository)
                                    {
                                        var photometry = await imagevm.CalculatePhotometry.Execute();

                                        var record = manager.GetOrAdd(image);

                                        if (photometry != null)
                                        {
                                            // Also cache photometry, otherwise the caching won't benefit
                                            // the GUI at all
                                            record.Photometry = photometry;

                                            // Update stars count of statistics
                                            stats = fitsImageStatisticsFactory.Create(stats, photometry.Count());
                                        }

                                        record.Statistics = stats;

                                        record.IsOutdated = false;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        imagevm.Dispose();
                    }

                    iimage++;
                }

                if (IsCancelling)
                {
                    return CreateCancellation(null);
                }
                else
                {
                    return CreateCompletion(results);
                }
            };
        }

        protected override void OnCancelling()
        {
            currentProgressViewModel?.SetCancelling();
        }

        protected override void OnProgressChanged(FitsImageAllStatisticsProgress value)
        {
            NumberOfImages = Math.Max(NumberOfImages, value.numberOfImages);
            ProgressValue = Math.Max(ProgressValue, value.progress);
            if (CurrentImage == value.currentImage)
            {
                NumberOfObjects = Math.Max(NumberOfObjects, value.numberOfObjects);
                CurrentObject = Math.Max(CurrentObject, value.numberOfObjects > 0 ? value.currentObject + 1 : 0);
                NumberOfStars = Math.Max(NumberOfStars, value.numberOfStars);
            }
            else
            {
                // Allow resetting per image stats when image changed
                NumberOfObjects = value.numberOfObjects;
                CurrentObject = value.numberOfObjects > 0 ? value.currentObject + 1 : 0;
                NumberOfStars = value.numberOfStars;
            }
            CurrentImageFile = value.currentImageFile;
            CurrentImageFileName = Path.GetFileName(value.currentImageFile);
            CurrentImage = Math.Max(CurrentImage, value.numberOfImages > 0 ? value.currentImage + 1 : 0);
            Phase = value.phase;
        }
    }
}
