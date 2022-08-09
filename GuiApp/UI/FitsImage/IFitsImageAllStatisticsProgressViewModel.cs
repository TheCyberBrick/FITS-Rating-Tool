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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.UI.Progress;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public struct FitsImageAllStatisticsProgress
    {
        public int numberOfImages;
        public int currentImage;
        public string currentImageFile;
        public int numberOfObjects;
        public int currentObject;
        public int numberOfStars;
        public float progress;
        public string phase;
    }

    public interface IFitsImageAllStatisticsProgressViewModel : IProgressViewModel<Dictionary<string, IFitsImageStatisticsViewModel?>, Dictionary<string, IFitsImageStatisticsViewModel?>, FitsImageAllStatisticsProgress>
    {
        public interface IFactory
        {
            IFitsImageAllStatisticsProgressViewModel Create(IEnumerable<string> images, bool useRepository);
        }

        int NumberOfImages { get; }

        int CurrentImage { get; }

        string CurrentImageFile { get; }

        string CurrentImageFileName { get; }

        int NumberOfObjects { get; }

        int CurrentObject { get; }

        int NumberOfStars { get; }

        float ProgressValue { get; }

        string Phase { get; }
    }
}
