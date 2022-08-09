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
using FitsRatingTool.GuiApp.UI.Progress;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public struct FitsImageLoadProgress
    {
        public int numberOfFiles;
        public int currentFile;
        public string currentFilePath;
    }

    public interface IFitsImageLoadProgressViewModel : IProgressViewModel<List<IFitsImageViewModel>, List<IFitsImageViewModel>, FitsImageLoadProgress>
    {
        public interface IFactory
        {
            IFitsImageLoadProgressViewModel Create(IEnumerable<string> files, Action<IFitsImageViewModel>? consumer);
        }

        public int NumberOfFiles { get; }

        public int CurrentFile { get; }

        public string CurrentFilePath { get; }

        public string CurrentFileName { get; }

        public float ProgressValue { get; }
    }
}
