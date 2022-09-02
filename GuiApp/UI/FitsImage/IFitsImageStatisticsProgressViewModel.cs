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
using System;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.Progress;
using FitsRatingTool.FitsLoader.Models;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public struct FitsImageStatisticsProgress
    {
        public int numberOfObjects;
        public int currentObject;
        public int numberOfStars;
        public float progress;
        public string phase;
    }

    public interface IFitsImageStatisticsProgressViewModel : IProgressViewModel<PhotometryStatistics?, IFitsImageStatisticsViewModel, FitsImageStatisticsProgress>
    {
        public interface IFactory
        {
            public delegate Func<Task<PhotometryStatistics?>> AsyncTaskFunc(IFitsImage.PhotometryCallback callback);

            IFitsImageStatisticsProgressViewModel Create(AsyncTaskFunc taskFunc);
        }

        public int NumberOfObjects { get; }

        public int CurrentObject { get; }

        public int NumberOfStars { get; }

        public float ProgressValue { get; }

        public string Phase { get; }
    }
}
