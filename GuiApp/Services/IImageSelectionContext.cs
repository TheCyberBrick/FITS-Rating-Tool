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

namespace FitsRatingTool.GuiApp.Services
{
    public interface IImageSelectionContext
    {
        public class FileChangedEventArgs : EventArgs
        {
            public string? OldFile { get; }

            public string? NewFile { get; }

            public FileChangedEventArgs(string? oldFile, string? newFile)
            {
                OldFile = oldFile;
                NewFile = newFile;
            }
        }

        public class RecordChangedEventArgs : EventArgs
        {
            public IFitsImageManager.IRecord? OldRecord { get; }

            public IFitsImageManager.IRecord? NewRecord { get; }

            public IFitsImageManager.RecordChangedEventArgs.ChangeType Type { get; }

            public RecordChangedEventArgs(IFitsImageManager.IRecord? oldRecord, IFitsImageManager.IRecord? newRecord, IFitsImageManager.RecordChangedEventArgs.ChangeType type)
            {
                OldRecord = oldRecord;
                NewRecord = newRecord;
                Type = type;
            }
        }


        string? CurrentFile { get; set; }

        string? CurrentFileName { get; }

        IFitsImageManager.IRecord? CurrentRecord { get; set; }


        void LoadFromOther(IImageSelectionContext ctx);


        event EventHandler<FileChangedEventArgs> CurrentFileChanged;

        event EventHandler<RecordChangedEventArgs> CurrentRecordChanged;
    }
}
