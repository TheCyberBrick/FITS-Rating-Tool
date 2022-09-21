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
using System.Collections.Concurrent;
using System.Collections.Generic;
using FitsRatingTool.GuiApp.Repositories;
using FitsRatingTool.GuiApp.UI.FitsImage;
using Avalonia.Utilities;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class FitsImageManager : IFitsImageManager
    {
        private class Record : IFitsImageManager.IRecord
        {
            public string File { get; }

            public IFitsImageStatisticsViewModel? Statistics
            {
                get => manager.analysisRepository.GetStatistics(File);
                set
                {
                    if (value != null)
                    {
                        EnsureInRepository();
                        manager.analysisRepository.AddStatistics(File, value);
                        manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.Statistics, false);
                    }
                    else
                    {
                        if (manager.analysisRepository.RemoveStatistics(File) != null)
                        {
                            manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.Statistics, true);
                        }
                    }
                }
            }

            public IEnumerable<IFitsImagePhotometryViewModel>? Photometry
            {
                get => manager.analysisRepository.GetPhotometry(File);
                set
                {
                    if (value != null)
                    {
                        EnsureInRepository();
                        manager.analysisRepository.AddPhotometry(File, value);
                        manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.Photometry, false);
                    }
                    else
                    {
                        if (manager.analysisRepository.RemovePhotometry(File) != null)
                        {
                            manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.Photometry, true);
                        }
                    }
                }
            }

            public IFitsImageMetadata? Metadata
            {
                get => manager.metadataRepository.GetMetadata(File);
                set
                {
                    if (value != null)
                    {
                        EnsureInRepository();
                        manager.metadataRepository.AddMetadata(value);
                        manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.Metadata, false);
                    }
                    else
                    {
                        if (manager.metadataRepository.RemoveMetadata(File) != null)
                        {
                            manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.Metadata, true);
                        }
                    }
                }
            }

            private bool _outdated = false;
            public bool IsOutdated
            {
                get => _outdated;
                set
                {
                    if (_outdated != value)
                    {
                        _outdated = value;
                        manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.Outdated, false);
                    }
                }
            }

            public bool IsValid => manager.fileRepository.ContainsFile(File);


            private readonly FitsImageManager manager;

            public Record(string file, FitsImageManager manager)
            {
                File = file;
                this.manager = manager;
            }

            private void EnsureInRepository()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("File '" + File + "' is no longer in the file repository");
                }
            }
        }

        private readonly ConcurrentDictionary<string, Record> records = new();


        private readonly IFileRepository fileRepository;
        private readonly IAnalysisRepository analysisRepository;
        private readonly IFitsImageMetadataRepository metadataRepository;


        public FitsImageManager(IFileRepository fileRepository, IAnalysisRepository analysisRepository, IFitsImageMetadataRepository metadataRepository, IInstrumentProfileManager instrumentProfileManager)
        {
            this.fileRepository = fileRepository;
            this.analysisRepository = analysisRepository;
            this.metadataRepository = metadataRepository;

            WeakEventHandlerManager.Subscribe<IInstrumentProfileManager, IInstrumentProfileManager.ProfileChangedEventArgs, FitsImageManager>(instrumentProfileManager, nameof(instrumentProfileManager.CurrentProfileChanged), OnCurrentProfileChanged);
        }

        private void OnCurrentProfileChanged(object? sender, IInstrumentProfileManager.ProfileChangedEventArgs e)
        {
            foreach (var record in records.Values)
            {
                record.IsOutdated = true;
            }
        }

        private void NotifyChange(IFitsImageManager.IRecord record, IFitsImageManager.RecordChangedEventArgs.DataType type, bool removed)
        {
            _recordChanged?.Invoke(this, new IFitsImageManager.RecordChangedEventArgs(record.File, type, removed));
        }

        public IReadOnlyCollection<string> Files => fileRepository.Files;

        public bool Contains(string file)
        {
            return records.ContainsKey(file);
        }

        public IFitsImageManager.IRecord? Get(string file)
        {
            if (records.TryGetValue(file, out var record))
            {
                return record;
            }
            return null;
        }

        public IFitsImageManager.IRecord GetOrAdd(string file)
        {
            Record? newRecord = null;
            var record = records.GetOrAdd(file, f =>
            {
                newRecord = new Record(f, this);
                return newRecord;
            });
            if (record == newRecord)
            {
                fileRepository.AddFile(file);

                NotifyChange(record, IFitsImageManager.RecordChangedEventArgs.DataType.File, false);
            }
            return record;
        }

        public IFitsImageManager.IRecord? Remove(string file)
        {
            if (records.Remove(file, out var record))
            {
                // Remove this file from all repositories
                fileRepository.RemoveFile(file);
                analysisRepository.RemoveStatistics(file);
                analysisRepository.RemovePhotometry(file);
                metadataRepository.RemoveMetadata(file);

                NotifyChange(record, IFitsImageManager.RecordChangedEventArgs.DataType.File, true);

                return record;
            }
            return null;
        }

        private event EventHandler<IFitsImageManager.RecordChangedEventArgs>? _recordChanged;
        public event EventHandler<IFitsImageManager.RecordChangedEventArgs> RecordChanged
        {
            add
            {
                _recordChanged += value;
            }
            remove
            {
                _recordChanged -= value;
            }
        }
    }
}
