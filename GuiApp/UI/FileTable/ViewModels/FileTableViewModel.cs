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
using Avalonia.Utilities;
using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.Common.Utils;
using FitsRatingTool.FitsLoader.Models;
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.FileTable.ViewModels
{
    public class FileTableViewModel : ViewModelBase, IFileTableViewModel
    {
        public class Factory : IFileTableViewModel.IFactory
        {
            private readonly IFitsImageManager manager;

            public Factory(IFitsImageManager manager)
            {
                this.manager = manager;
            }

            public IFileTableViewModel Create()
            {
                return new FileTableViewModel(manager);
            }
        }



        public AvaloniaList<IFileTableViewModel.Record> Records { get; } = new();

        private IFileTableViewModel.Record? _selectedRecord;
        public IFileTableViewModel.Record? SelectedRecord
        {
            get => _selectedRecord;
            set => this.RaiseAndSetIfChanged(ref _selectedRecord, value);
        }

        public ReactiveCommand<IEnumerable, Unit> RemoveRecords { get; }


        private readonly IFitsImageManager manager;

        public FileTableViewModel(IFitsImageManager manager)
        {
            this.manager = manager;

            using (DelayChangeNotifications())
            {
                foreach (var file in manager.Files)
                {
                    var record = manager.Get(file);
                    if (record != null)
                    {
                        Records.Add(CreateRecord(record.Id, record.File, record.Metadata));
                    }
                }
            }

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, FileTableViewModel>(manager, nameof(manager.RecordChanged), OnRecordChanged);

            RemoveRecords = ReactiveCommand.CreateFromTask<IEnumerable>(async enumerable =>
            {
                List<IFileTableViewModel.Record> toRemove = new();
                foreach (var obj in enumerable)
                {
                    if (obj is IFileTableViewModel.Record record)
                    {
                        toRemove.Add(record);
                    }
                }
                foreach (var record in toRemove)
                {
                    await record.Remove.Execute();
                }
            });
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.File)
            {
                if (args.AddedOrUpdated)
                {
                    AddRecord(args.File);
                }
                else
                {
                    RemoveRecord(args.File);
                }
            }
            else if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Metadata && args.AddedOrUpdated)
            {
                UpdateRecord(args.File);
            }
        }

        private IFileTableViewModel.Record CreateRecord(long id, string file, IFitsImageMetadata? metadata)
        {
            Dictionary<string, string> header = new();

            List<IFitsImageHeaderRecord> records = new List<IFitsImageHeaderRecord>();

            if (metadata != null)
            {
                foreach (var record in metadata.Header)
                {
                    header.Add(record.Keyword.ToUpper(), record.Value);
                    records.Add(record);
                }
            }

            long fileSize = 0;
            DateTime creationDate = new DateTime();
            DateTime modificationDate = new DateTime();

            try
            {
                var info = new FileInfo(file);
                fileSize = info.Length;
                creationDate = info.CreationTime;
                modificationDate = info.LastWriteTime;
            }
            catch (Exception)
            {
                // OK
            }

            var newRecord = new IFileTableViewModel.Record(
                id, file,
                GetObjectFromHeader(header),
                GetDateStringFromHeader(header),
                GetFilterFromHeader(header),
                GetExposureTimeFromHeader(header),
                fileSize, creationDate, modificationDate,
                metadata, records);

            newRecord.Remove.Subscribe(_ => manager.Remove(newRecord.File));

            return newRecord;
        }

        private void AddRecord(string file)
        {
            var record = manager.Get(file);
            if (record != null)
            {
                Records.Add(CreateRecord(record.Id, file, record.Metadata));
            }
        }

        private void RemoveRecord(string file)
        {
            if (SelectedRecord != null && file.Equals(SelectedRecord.File))
            {
                SelectedRecord = null;
            }

            foreach (var record in Records)
            {
                if (file.Equals(record.File))
                {
                    Records.Remove(record);
                    break;
                }
            }
        }

        private void UpdateRecord(string file)
        {
            var record = manager.Get(file);
            if (record != null)
            {
                int idx = 0;
                foreach (var other in Records)
                {
                    if (file.Equals(other.File))
                    {
                        Records[idx] = CreateRecord(record.Id, file, record.Metadata);
                        break;
                    }
                    ++idx;
                }
            }
        }

        private string? GetObjectFromHeader(Dictionary<string, string> header)
        {
            return header.TryGetValue("OBJECT", out var val) ? val : null;
        }

        private string? GetDateStringFromHeader(Dictionary<string, string> header, string format = "g", CultureInfo? culture = null)
        {
            var date = GetDateFromHeader(header);
            if (date.HasValue)
            {
                return date.Value.ToLocalTime().ToString(format, culture);
            }
            return null;
        }

        private DateTime? GetDateFromHeader(Dictionary<string, string> header)
        {
            string[] dateKeywords = { "DATE-OBS", "DATE" };
            foreach (var dateKeyword in dateKeywords)
            {
                if (header.TryGetValue(dateKeyword, out var val))
                {
                    return FitsUtils.ParseDate(val);
                }
            }
            return null;
        }

        private string? GetFilterFromHeader(Dictionary<string, string> header)
        {
            return FitsUtils.FindFilter(keyword => header.TryGetValue(keyword, out var val) ? val : null);
        }

        private float? GetExposureTimeFromHeader(Dictionary<string, string> header)
        {
            return header.TryGetValue("EXPTIME", out var val) && float.TryParse(val, out var exp) ? exp : null;
        }
    }
}
