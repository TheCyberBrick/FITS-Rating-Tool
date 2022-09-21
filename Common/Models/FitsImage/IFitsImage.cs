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

using FitsRatingTool.FitsLoader.Models;

namespace FitsRatingTool.Common.Models.FitsImage
{
    public interface IFitsImage : IFitsImageMetadata, IDisposable
    {
        bool IsFileClosed { get; }

        bool IsImageDataValid { get; }

        bool IsStatisticsValid { get; }

        bool IsImageValid { get; }

        uint[] Histogram { get; }

        uint[] StretchedHistogram { get; }

        bool AlwaysUnloadImageData { get; set; }

        new IReadOnlyDictionary<string, FitsImageHeaderRecord> Header { get; }

        FitsImageDim InDim { get; }

        FitsImageDim OutDim { get; }

        public delegate bool PhotometryCallback(PhotometryPhase phase, int nobj, int iobj, int nstars, bool success, PhotometryStatistics? stats);

        void CloseFile();

        bool LoadImageData(FitsImageLoaderParameters parameters);

        void UnloadImageData();

        bool ComputeStatisticsAndPhotometry(PhotometryCallback? callback = null);

        bool GetStatistics(out PhotometryStatistics statistics);

        bool GetPhotometry(out PhotometryObject[]? photometry);

        bool ComputeStretch(out ImageStretchParameters parameters);

        bool ProcessImage(bool computeStretch, FitsImageLoaderParameters parameters, out IFitsImageData? data);

        IDisposable Ref();
    }
}
