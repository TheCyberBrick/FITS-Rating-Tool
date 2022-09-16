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
using Avalonia.Visuals.Media.Imaging;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public interface IFitsImageViewerViewModel : IDisposable
    {
        public interface IFactory
        {
            public IFitsImageViewerViewModel Create();
        }

        public interface IOverlayFactory
        {
            public IOverlay Create(IFitsImageViewerViewModel viewer);
        }

        public interface IOverlay
        {
            IFitsImageViewerViewModel Viewer { get; }

            void TransferPropertiesFrom(IOverlay overlay);
        }

        string? File { get; set; }

        string? FileName { get; }

        #region +++ Image +++
        IFitsImageViewModel? FitsImage { get; set; }

        bool HasImage { get; }

        bool IsLoadingFromFile { get; }
        #endregion

        #region +++ Viewer settings +++
        long MaxInputSize { get; set; }

        int MaxWidth { get; set; }

        int MaxHeight { get; set; }

        int MaxConcurrentLoadingImages { get; set; }

        bool IsPrimaryViewer { get; set; }

        bool KeepStretch { get; set; }

        bool ShowPhotometry { get; set; }

        bool ShowPhotometryMeasurements { get; set; }

        int MaxShownPhotometry { get; set; }

        bool IsShownPhotometryIncomplete { get; }

        bool AutoCalculateStatistics { get; set; }

        bool AutoSetInterpolationMode { get; set; }

        bool IsPeekViewerEnabled { get; set; }

        int PeekViewerSize { get; set; }

        IFitsImageSectionViewerViewModel? PeekViewer { get; }
        #endregion

        #region +++ Statistics +++
        IFitsImageStatisticsViewModel? Statistics { get; }

        AvaloniaList<IFitsImagePhotometryViewModel> Photometry { get; }

        IFitsImageHistogramViewModel? Histogram { get; }

        IFitsImageHistogramViewModel? StretchedHistogram { get; }
        #endregion

        #region +++ Commands +++
        ReactiveCommand<BitmapInterpolationMode, Unit> SetInterpolationMode { get; }

        ReactiveCommand<Unit, Unit> ResetStretch { get; }

        ReactiveCommand<Unit, Unit> ApplyStretchToAll { get; }

        ReactiveCommand<Unit, Unit> CalculateStatistics { get; }

        ReactiveCommand<Unit, IFitsImageStatisticsProgressViewModel?> CalculateStatisticsWithProgress { get; }

        ReactiveCommand<Unit, Unit> CalculateStatisticsWithProgressDialog { get; }

        Interaction<IFitsImageStatisticsProgressViewModel, Unit> CalculateStatisticsProgressDialog { get; }

        ReactiveCommand<string?, IFitsImageViewModel?> LoadImage { get; }
        #endregion

        #region +++ Overlays +++
        IOverlayFactory? InnerOverlayFactory { get; set; }

        IOverlay? InnerOverlay { get; }

        IOverlayFactory? OuterOverlayFactory { get; set; }

        IOverlay? OuterOverlay { get; }
        #endregion

        Task UnloadAsync();

        void TransferPropertiesFrom(IFitsImageViewerViewModel viewer);
    }
}
