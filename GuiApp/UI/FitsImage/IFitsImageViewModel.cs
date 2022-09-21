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
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public interface IFitsImageViewModel : IFitsImageMetadata, IDisposable
    {
        public interface IFactory
        {
            public IFitsImageViewModel Create(string file);

            public IFitsImageViewModel Create(string file, long maxInputSize, int maxWidth, int maxHeight);

            public IFitsImageViewModel Create(IFitsImage image);
        }

        IFitsImageViewerViewModel? Owner { get; set; }

        bool IsAutoLoaded { get; set; }

        #region +++ Stretch +++
        bool PreserveColorBalance { get; set; }

        float Shadows { get; set; }

        float Midtones { get; set; }

        float Highlights { get; set; }
        #endregion

        #region +++ Image +++
        Bitmap? Bitmap { get; }

        bool HasImage { get; }

        bool IsImageDataValid { get; }

        bool IsStatisticsValid { get; }

        bool IsImageValid { get; }

        bool IsUpdating { get; }

        BitmapInterpolationMode InterpolationMode { get; set; }

        bool IsNearestNeighbor { get; set; }

        bool IsInterpolated { get; set; }

        void CloseFile();

        Bitmap? UpdateOrCreateBitmap(bool disposeBeforeSwap = true);

        Task<Bitmap?> UpdateOrCreateBitmapAsync(bool disposeBeforeSwap = true, CancellationToken ct = default);

        new IReadOnlyList<IFitsImageHeaderRecordViewModel> Header { get; }
        #endregion

        #region +++ Statistics +++
        uint[]? Histogram { get; }

        uint[]? StretchedHistogram { get; }

        bool InvalidateStatisticsAndPhotometry { get; set; }
        #endregion

        #region +++ Commands +++
        ReactiveCommand<Unit, Unit> ResetStretchParameters { get; }

        ReactiveCommand<Unit, IFitsImageStatisticsViewModel?> CalculateStatistics { get; }

        ReactiveCommand<Unit, IFitsImageStatisticsProgressViewModel?> CalculateStatisticsWithProgress { get; }

        ReactiveCommand<Unit, Unit> CalculateStatisticsWithProgressDialog { get; }

        Interaction<IFitsImageStatisticsProgressViewModel, Unit> CalculateStatisticsProgressDialog { get; }

        ReactiveCommand<Unit, IEnumerable<IFitsImagePhotometryViewModel>?> CalculatePhotometry { get; }
        #endregion

    }
}
