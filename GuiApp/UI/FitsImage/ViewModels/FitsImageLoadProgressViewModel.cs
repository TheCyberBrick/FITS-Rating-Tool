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

using FitsRatingTool.Common.Utils;
using Microsoft.VisualStudio.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.Progress;
using FitsRatingTool.GuiApp.UI.Progress.ViewModels;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageLoadProgressViewModel : SimpleProgressViewModel<List<IFitsImageViewModel>, FitsImageLoadProgress>, IFitsImageLoadProgressViewModel
    {
        public class Factory : IFitsImageLoadProgressViewModel.IFactory
        {
            private readonly IFitsImageViewModel.IFactory fitsImageFactory;
            private readonly IAppConfig appConfig;

            public Factory(IFitsImageViewModel.IFactory fitsImageFactory, IAppConfig appConfig)
            {
                this.fitsImageFactory = fitsImageFactory;
                this.appConfig = appConfig;
            }

            public IFitsImageLoadProgressViewModel Create(IEnumerable<string> files, Action<IFitsImageViewModel>? consumer)
            {
                return new FitsImageLoadProgressViewModel(fitsImageFactory, appConfig, files, consumer);
            }
        }


        private int _numberOfFiles;
        public int NumberOfFiles
        {
            get => _numberOfFiles;
            set => this.RaiseAndSetIfChanged(ref _numberOfFiles, value);
        }

        private int _currentFile;
        public int CurrentFile
        {
            get => _currentFile;
            set => this.RaiseAndSetIfChanged(ref _currentFile, value);
        }

        private string _currentFilePath = "";
        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => this.RaiseAndSetIfChanged(ref _currentFilePath, value);
        }

        private string _currentFileName = "";
        public string CurrentFileName
        {
            get => _currentFileName;
            set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
        }


        private float _currentProgressValue;
        public float ProgressValue
        {
            get => _currentProgressValue;
            set => this.RaiseAndSetIfChanged(ref _currentProgressValue, value);
        }




        private readonly IFitsImageViewModel.IFactory fitsImageFactory;
        private readonly IAppConfig appConfig;

        private readonly List<CancellationTokenSource> loadingCts = new();
        private readonly List<string> files;
        private readonly Action<IFitsImageViewModel>? consumer;

        private FitsImageLoadProgressViewModel(IFitsImageViewModel.IFactory fitsImageFactory, IAppConfig appConfig, IEnumerable<string> files, Action<IFitsImageViewModel>? consumer) : base(null)
        {
            this.fitsImageFactory = fitsImageFactory;
            this.appConfig = appConfig;
            this.files = new List<string>(files);
            this.consumer = consumer;
        }

        protected override Func<Task<Result<List<IFitsImageViewModel>>>> CreateTask(ProgressSynchronizationContext synchronizationContext)
        {
            return async () =>
            {
                using var cts = new CancellationTokenSource();

                loadingCts.Add(cts);

                try
                {
                    List<IFitsImageViewModel> images = new();

                    CancellationToken ct;
                    try
                    {
                        ct = cts.Token;
                    }
                    catch (ObjectDisposedException)
                    {
                        return CreateCancellation(images);
                    }

                    int n = files.Count;

                    var ioThrottle = new AsyncSemaphore(1);

                    int currentLoadingIndex = 0;

                    void reportNextFile()
                    {
                        if (currentLoadingIndex < n)
                        {
                            ReportProgress(new FitsImageLoadProgress
                            {
                                numberOfFiles = n,
                                currentFile = currentLoadingIndex,
                                currentFilePath = files[currentLoadingIndex]
                            });
                        }

                        ++currentLoadingIndex;
                    }

                    reportNextFile();

                    try
                    {
                        await BufferedOrderedConverter.RunAsync(16, files, (file, ct) => Task.Run(async () =>
                        {
                            IFitsImageViewModel? image = null;
                            try
                            {
                                // Limit I/O to loading one image at a time for sequential reads
                                using (await ioThrottle.EnterAsync(ct))
                                {
                                    image = fitsImageFactory.Create(file, appConfig.MaxImageSize, appConfig.MaxThumbnailWidth, appConfig.MaxThumbnailHeight);
                                }
                                image.PreserveColorBalance = false;
                                await image.UpdateOrCreateBitmapAsync(true, ct);
                            }
                            catch (Exception)
                            {
                            }
                            return image;
                        }), image =>
                        {
                            lock (images)
                            {
                                if (image != null)
                                {
                                    images.Add(image);
                                    consumer?.Invoke(image);
                                }

                                reportNextFile();
                            }
                        }, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        // OK, expected
                    }

                    if (IsCancelling)
                    {
                        return CreateCancellation(images);
                    }
                    else
                    {
                        return CreateCompletion(images);
                    }
                }
                finally
                {
                    loadingCts.Remove(cts);
                }
            };
        }

        protected override void OnCancelling()
        {
            try
            {
                loadingCts.ForEach(c =>
                {
                    try
                    {
                        c.Cancel();
                    }
                    catch (Exception)
                    {
                    }
                });
            }
            catch (Exception)
            {
            }
        }

        protected override void OnProgressChanged(FitsImageLoadProgress value)
        {
            NumberOfFiles = value.numberOfFiles;
            CurrentFile = value.currentFile + 1;
            CurrentFilePath = value.currentFilePath;
            CurrentFileName = Path.GetFileName(value.currentFilePath);
            ProgressValue = Math.Max(ProgressValue, value.currentFile / (float)value.numberOfFiles);
        }
    }
}
