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
using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.FitsLoader.Models;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.FileTable
{
    public interface IFileTableViewModel
    {
        public interface IFactory
        {
            IFileTableViewModel Create();
        }

        public class Record
        {
            public long Id { get; }

            public long IdPlusOne => Id + 1;

            public string File { get; }

            public string? Object { get; }

            public string? Date { get; }

            public string? Filter { get; }

            public float? ExposureTime { get; }

            public string FileName => Path.GetFileName(File);

            public long FileSize { get; }

            public DateTime CreationDate { get; }

            public DateTime ModificationDate { get; }

            public IFitsImageMetadata? Metadata { get; }

            public IReadOnlyList<IFitsImageHeaderRecord> Header { get; }

            public ReactiveCommand<Unit, Unit> Remove { get; }

            public Record(long id, string file, string? obj, string? date, string? filter, float? exposureTime, long fileSize,
                DateTime creationDate, DateTime modificationDate, IFitsImageMetadata? metadata, IReadOnlyList<IFitsImageHeaderRecord> header)
            {
                Id = id;
                File = file;
                Object = obj;
                Date = date;
                Filter = filter;
                ExposureTime = exposureTime;
                FileSize = fileSize;
                CreationDate = creationDate;
                ModificationDate = modificationDate;
                Metadata = metadata;
                Header = header;
                Remove = ReactiveCommand.Create(() => { });
            }
        }

        AvaloniaList<Record> Records { get; }

        Record? SelectedRecord { get; set; }

        ReactiveCommand<IEnumerable, Unit> RemoveRecords { get; }
    }
}
