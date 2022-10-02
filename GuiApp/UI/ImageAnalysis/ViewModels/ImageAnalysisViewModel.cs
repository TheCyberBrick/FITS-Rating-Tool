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

using Avalonia.Utilities;
using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.FitsImage;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.ImageAnalysis.ViewModels
{
    public class ImageAnalysisViewModel : ViewModelBase, IImageAnalysisViewModel
    {
        public class Factory : IImageAnalysisViewModel.IFactory
        {
            private readonly IFitsImageManager fitsImageManager;
            private readonly IStarSampler starSampler;

            public Factory(IFitsImageManager fitsImageManager, IStarSampler starSampler)
            {
                this.fitsImageManager = fitsImageManager;
                this.starSampler = starSampler;
            }

            public IImageAnalysisViewModel Create(string file)
            {
                return new ImageAnalysisViewModel(file, fitsImageManager, starSampler);
            }
        }


        public string File { get; }

        public string FileName { get; }

        private int _dataKeyIndex = 0;
        public int DataKeyIndex
        {
            get => _dataKeyIndex;
            set => this.RaiseAndSetIfChanged(ref _dataKeyIndex, value);
        }

        private int _maxSamples = 250;
        public int MaxSamples
        {
            get => _maxSamples;
            set => this.RaiseAndSetIfChanged(ref _maxSamples, value);
        }

        private float _smoothness = 0.1f;
        public float Smoothness
        {
            get => _smoothness;
            set => this.RaiseAndSetIfChanged(ref _smoothness, Math.Max(0.0f, value));
        }

        private int _steps = 6;
        public int Steps
        {
            get => _steps;
            set => this.RaiseAndSetIfChanged(ref _steps, value);
        }

        private int _horizontalResolution = 256;
        public int HorizontalResolution
        {
            get => _horizontalResolution;
            set => this.RaiseAndSetIfChanged(ref _horizontalResolution, value);
        }

        private int _verticalResolution = 256;
        public int VerticalResolution
        {
            get => _verticalResolution;
            set => this.RaiseAndSetIfChanged(ref _verticalResolution, value);
        }

        private double[,] _rawData = new double[0, 0];
        public double[,] RawData
        {
            get => _rawData;
            private set => this.RaiseAndSetIfChanged(ref _rawData, value);
        }

        private double[,] _data = new double[0, 0];
        public double[,] Data
        {
            get => _data;
            private set => this.RaiseAndSetIfChanged(ref _data, value);
        }

        private double _dataStepSize = 0.0;
        public double DataStepSize
        {
            get => _dataStepSize;
            private set => this.RaiseAndSetIfChanged(ref _dataStepSize, value);
        }

        private double[] _dataSteps = new double[0];
        public double[] DataSteps
        {
            get => _dataSteps;
            private set => this.RaiseAndSetIfChanged(ref _dataSteps, value);
        }



        private readonly IFitsImageManager fitsImageManager;
        private readonly IStarSampler starSampler;

        private ImageAnalysisViewModel(string file, IFitsImageManager fitsImageManager, IStarSampler starSampler)
        {
            this.fitsImageManager = fitsImageManager;
            this.starSampler = starSampler;

            File = file;
            FileName = Path.GetFileName(file);

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, ImageAnalysisViewModel>(fitsImageManager, nameof(fitsImageManager.RecordChanged), OnRecordChanged);

            UpdateData();

            this.WhenAnyValue(x => x.DataKeyIndex)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateData());

            this.WhenAnyValue(x => x.MaxSamples)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateData());

            this.WhenAnyValue(x => x.Smoothness)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateData());

            this.WhenAnyValue(x => x.Steps)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateData());

            this.WhenAnyValue(x => x.VerticalResolution)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateData());

            this.WhenAnyValue(x => x.HorizontalResolution)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateData());
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.File == File)
            {
                if (args.Removed)
                {
                    RawData = Data = new double[0, 0];
                }
                else if (args.AddedOrUpdated && args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Photometry)
                {
                    UpdateData();
                }
            }
        }

        private void UpdateData()
        {
            var record = fitsImageManager.Get(File);

            var metadata = record?.Metadata;
            var photometry = record?.Photometry;

            if (metadata != null && photometry != null && IsValidDataKeyIndex(DataKeyIndex))
            {
                UpdateData(DataKeyIndex, metadata, new List<IFitsImagePhotometryViewModel>(photometry));
            }
            else
            {
                RawData = Data = new double[0, 0];
            }
        }

        private async void UpdateData(int dataKeyIndex, IFitsImageMetadata metadata, List<IFitsImagePhotometryViewModel> photometry)
        {
            var tps = new ThinPlateSpline()
            {
                Regularization = 100f * Smoothness
            };

            List<Point> points = new();
            List<double> values = new();

            photometry = await Task.Run(() => starSampler.Sample(photometry, MaxSamples, out var _));

            foreach (var p in photometry)
            {
                points.Add(new Point(p.PSF.X, p.PSF.Y));
                values.Add(GetValue(dataKeyIndex, p));
            }

            var newData = await Task.Run(() =>
            {
                if (tps.Solve(points, values, out var solution))
                {
                    double[,] rawData = new double[HorizontalResolution, VerticalResolution];

                    double min = double.PositiveInfinity;
                    double max = double.NegativeInfinity;

                    for (int y = 0; y < VerticalResolution; ++y)
                    {
                        for (int x = 0; x < HorizontalResolution; ++x)
                        {
                            double value = rawData[x, VerticalResolution - 1 - y] = solution.Interpolate(new Point(metadata.ImageWidth / HorizontalResolution * (x + 0.5f), metadata.ImageHeight / VerticalResolution * (y + 0.5f)));
                            min = Math.Min(value, min);
                            max = Math.Max(value, max);
                        }
                    }

                    double[,] data = rawData;

                    List<double> dataSteps = new();

                    if (Steps > 0)
                    {
                        for (int i = 0; i <= Steps; ++i)
                        {
                            double v1 = i / (double)Steps * (max - min) + min;
                            double v2 = (i + 1) / (double)Steps * (max - min) + min;
                            dataSteps.Add(0.5 * (v1 + v2));
                        }

                        data = new double[HorizontalResolution, VerticalResolution];

                        for (int y = 0; y < VerticalResolution; ++y)
                        {
                            for (int x = 0; x < HorizontalResolution; ++x)
                            {
                                data[x, y] = Math.Round((rawData[x, y] - min) / (max - min) * Steps) / Steps * (max - min) + min;
                            }
                        }
                    }

                    return Tuple.Create(rawData, data, min, max, dataSteps);
                }

                return null;
            });

            RawData = newData?.Item1 ?? new double[0, 0];
            Data = newData?.Item2 ?? new double[0, 0];

            if (newData != null && Steps > 0)
            {
                DataStepSize = (newData.Item4 - newData.Item3) / Steps;
                DataSteps = newData.Item5.ToArray();
            }
            else
            {
                DataStepSize = 0.0;
                DataSteps = new double[0];
            }
        }

        private bool IsValidDataKeyIndex(int dataKeyIndex)
        {
            return dataKeyIndex >= 0 && dataKeyIndex <= 3;
        }

        private double GetValue(int dataKeyIndex, IFitsImagePhotometryViewModel photometry)
        {
            return DataKeyIndex switch
            {
                0 => photometry.HFD,
                1 => photometry.PSF.FWHM,
                2 => photometry.PSF.Eccentricity,
                3 => photometry.PSF.Residual,
                _ => 0
            };
        }
    }
}
