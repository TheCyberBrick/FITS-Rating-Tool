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
using FitsRatingTool.IoC;

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
        public record OfFiles(IEnumerable<string> Files, IContainer<IFitsImageViewModel, IFitsImageViewModel.OfFile> Container, Action<IFitsImageViewModel>? Consumer);

        int NumberOfFiles { get; }

        int CurrentFile { get; }

        string CurrentFilePath { get; }

        string CurrentFileName { get; }

        float ProgressValue { get; }
    }
}
