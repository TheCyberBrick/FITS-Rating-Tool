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
using ReactiveUI;
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
            public string File { get; }

            public string? Object { get; }

            public string? Date { get; }

            public string? Filter { get; }

            public float? ExposureTime { get; }

            public string FileName => Path.GetFileName(File);

            public IFitsImageMetadata? Metadata { get; }

            public IReadOnlyList<IFitsImageHeaderRecord> Header { get; }

            public ReactiveCommand<Unit, Unit> Remove { get; }

            public Record(string file, string? obj, string? date, string? filter, float? exposureTime, IFitsImageMetadata? metadata, IReadOnlyList<IFitsImageHeaderRecord> header)
            {
                File = file;
                Object = obj;
                Date = date;
                Filter = filter;
                ExposureTime = exposureTime;
                Metadata = metadata;
                Header = header;
                Remove = ReactiveCommand.Create(() => { });
            }
        }

        AvaloniaList<Record> Records { get; }

        IFileTableViewModel.Record? SelectedRecord { get; set; }

        ReactiveCommand<IEnumerable, Unit> RemoveRecords { get; }
    }
}
