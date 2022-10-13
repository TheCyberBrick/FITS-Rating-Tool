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
using System.Collections.Generic;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.Models;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IFitsImageManager
    {
        public interface IRecord : IFitsImageFile
        {
            long Id { get; }

            IFitsImageStatisticsViewModel? Statistics { get; set; }

            IEnumerable<IFitsImagePhotometryViewModel>? Photometry { get; set; }

            IFitsImageMetadata? Metadata { get; set; }

            IEnumerable<IFitsImageContainer> ImageContainers { get; }

            bool IsOutdated { get; set; }

            bool IsValid { get; }
        }

        public class RecordChangedEventArgs : EventArgs
        {
            public enum DataType
            {
                File, Statistics, Photometry, Metadata, ImageContainers, Outdated
            }

            public string File { get; }

            public DataType Type { get; }

            public bool Removed => _removed;

            public bool AddedOrUpdated => !_removed;


            private readonly bool _removed;


            public RecordChangedEventArgs(string file, DataType type, bool removed)
            {
                File = file;
                Type = type;
                _removed = removed;
            }
        }

        public class CurrentFileChangedEventArgs : EventArgs
        {
            public string? OldFile { get; }

            public string? NewFile { get; }

            public CurrentFileChangedEventArgs(string? oldFile, string? newFile)
            {
                OldFile = oldFile;
                NewFile = newFile;
            }
        }

        IReadOnlyList<string> Files { get; }

        bool Contains(string file);

        IRecord? Get(string file);

        IRecord GetOrAdd(string file);

        IRecord? Remove(string file);

        string? CurrentFile { get; set; }

        IDisposable RegisterImageContainer(IFitsImageContainer container);

        event EventHandler<RecordChangedEventArgs> RecordChanged;

        event EventHandler<CurrentFileChangedEventArgs> CurrentFileChanged;
    }
}
