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

using FitsRatingTool.Common.Models.FitsImage;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.Progress.ViewModels;
using FitsRatingTool.FitsLoader.Models;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageStatisticsProgressViewModel : CallbackProgressViewModel<IFitsImage.PhotometryCallback, PhotometryStatistics?, IFitsImageStatisticsViewModel, FitsImageStatisticsProgress>, IFitsImageStatisticsProgressViewModel
    {
        public class Factory : IFitsImageStatisticsProgressViewModel.IFactory
        {
            private readonly IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory;

            public Factory(IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory)
            {
                this.fitsImageStatisticsFactory = fitsImageStatisticsFactory;
            }

            public IFitsImageStatisticsProgressViewModel Create(IFitsImageStatisticsProgressViewModel.IFactory.AsyncTaskFunc taskFunc)
            {
                return new FitsImageStatisticsProgressViewModel(fitsImageStatisticsFactory, callback => taskFunc.Invoke(callback));
            }
        }

        private int _numberOfObjects;
        public int NumberOfObjects
        {
            get => _numberOfObjects;
            set => this.RaiseAndSetIfChanged(ref _numberOfObjects, value);
        }

        private int _currentObject;
        public int CurrentObject
        {
            get => _currentObject;
            set => this.RaiseAndSetIfChanged(ref _currentObject, value);
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

        private readonly IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory;

        private FitsImageStatisticsProgressViewModel(IFitsImageStatisticsViewModel.IFactory fitsImageStatisticsFactory, AsyncTaskFunc taskFunc) : base(taskFunc)
        {
            this.fitsImageStatisticsFactory = fitsImageStatisticsFactory;
        }

        protected override void OnProgressChanged(FitsImageStatisticsProgress value)
        {
            ProgressValue = Math.Max(ProgressValue, value.progress);
            NumberOfObjects = Math.Max(NumberOfObjects, value.numberOfObjects);
            CurrentObject = Math.Max(CurrentObject, value.numberOfObjects > 0 ? value.currentObject + 1 : 0);
            NumberOfStars = Math.Max(NumberOfStars, value.numberOfStars);
            Phase = value.phase;
        }

        protected override Task<IFitsImage.PhotometryCallback> CreateCallbackAsync()
        {
            bool callback(PhotometryPhase phase, int nobj, int iobj, int nstars, bool success, PhotometryStatistics? stats)
            {
                float progress = 0.0f;
                switch (phase)
                {
                    case PhotometryPhase.Median:
                        progress = 0.0f; // Started
                        break;
                    case PhotometryPhase.Background:
                        progress = 0.05f; // Median done
                        break;
                    case PhotometryPhase.Extract:
                        progress = 0.1f; // Background done
                        break;
                    case PhotometryPhase.Object:
                        progress = 0.2f; // Extract done
                        break;
                }

                if (phase == PhotometryPhase.Object)
                {
                    float remaining = 0.9f - progress;
                    progress += remaining / nobj * iobj;
                }
                else if (phase == PhotometryPhase.Statistics)
                {
                    progress = 0.9f;
                }

                bool completed = false;

                if (IsCancelling)
                {
                    Finish(CreateInternalCancellation(null));
                    return false;
                }
                else if (phase == PhotometryPhase.Completed)
                {
                    progress = 1.0f;
                    completed = true;
                }

                ReportProgress(new() { numberOfObjects = nobj, currentObject = iobj, numberOfStars = nstars, progress = progress, phase = Enum.GetName(phase)! });

                if (completed)
                {
                    Finish(CreateInternalCompletion(stats));
                    return false;
                }

                return true;
            }
            return Task.FromResult((IFitsImage.PhotometryCallback)callback);
        }

        protected override Task<IFitsImageStatisticsViewModel?> MapResultAsync(PhotometryStatistics? result)
        {
            if (result.HasValue)
            {
                return Task.FromResult<IFitsImageStatisticsViewModel?>(fitsImageStatisticsFactory.Create(result.Value, 0));
            }
            return Task.FromResult<IFitsImageStatisticsViewModel?>(null);
        }
    }
}
