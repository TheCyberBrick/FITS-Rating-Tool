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
using System.Threading;
using FitsRatingTool.GuiApp.Models;
using System.Collections.Specialized;
using System.IO;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class FitsImageManager : IFitsImageManager
    {
        private class Record : IFitsImageManager.IRecord
        {
            public long Id { get; }

            public string File { get; }

            public string FileName => Path.GetFileName(File);

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

            private List<IFitsImageContainer> _containers = new();
            public IEnumerable<IFitsImageContainer> ImageContainers => _containers;

            public bool IsValid => manager.fileRepository.ContainsFile(File);


            private readonly FitsImageManager manager;

            public Record(long id, string file, FitsImageManager manager)
            {
                Id = id;
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

            internal void OnContainerImageAdded(IFitsImageContainer container)
            {
                if (ContainsImage(container))
                {
                    if (!_containers.Contains(container))
                    {
                        _containers.Add(container);
                    }
                    manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.ImageContainers, false);
                }
            }

            internal void OnContainerImageRemoved(IFitsImageContainer container, bool forceRemove)
            {
                if (forceRemove || !ContainsImage(container))
                {
                    if (_containers.Remove(container))
                    {
                        manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.ImageContainers, true);
                    }
                }
                else
                {
                    manager.NotifyChange(this, IFitsImageManager.RecordChangedEventArgs.DataType.ImageContainers, false);
                }
            }

            private bool ContainsImage(IFitsImageContainer container)
            {
                foreach (var image in container.FitsImages)
                {
                    if (image.File == File)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private readonly ConcurrentDictionary<string, Record> records = new();
        private long idCounter = -1;

        private readonly ConcurrentDictionary<IFitsImageContainer, int> containers = new();

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

        public IReadOnlyList<string> Files => fileRepository.Files;

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
                newRecord = new Record(Interlocked.Increment(ref idCounter), f, this);
                return newRecord;
            });
            if (record == newRecord)
            {
                fileRepository.AddFile(file);

                NotifyChange(record, IFitsImageManager.RecordChangedEventArgs.DataType.File, false);

                lock (containers)
                {
                    foreach (var container in containers.Keys)
                    {
                        foreach (var image in container.FitsImages)
                        {
                            if (image.File == file)
                            {
                                record.OnContainerImageAdded(container);
                            }
                        }
                    }
                }
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

        public IDisposable RegisterImageContainer(IFitsImageContainer container)
        {
            lock (containers)
            {
                if (containers.AddOrUpdate(container, 1, (p, i) => i + 1) == 1)
                {
                    var images = new List<IFitsImage>(container.FitsImages);

                    container.FitsImages.CollectionChanged += OnContainerFitsImagesChanged;

                    foreach (var image in images)
                    {
                        var record = Get(image.File);
                        if (record is Record r)
                        {
                            r.OnContainerImageAdded(container);
                        }
                    }
                }
            }
            return new ImageContainerRegistrationReleaser(this, container);
        }

        private void UnregisterImageContainer(IFitsImageContainer container)
        {
            lock (containers)
            {
                if (containers.AddOrUpdate(container, 0, (p, i) => i - 1) <= 0)
                {
                    var images = new List<IFitsImage>(container.FitsImages);

                    container.FitsImages.CollectionChanged -= OnContainerFitsImagesChanged;

                    if (containers.TryRemove(container, out var _))
                    {
                        foreach (var image in images)
                        {
                            var record = Get(image.File);
                            if (record is Record r)
                            {
                                r.OnContainerImageRemoved(container, true);
                            }
                        }
                    }
                }
            }
        }

        private void OnContainerFitsImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is IFitsImageContainer container)
            {
                if (e.OldItems != null && (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Reset))
                {
                    foreach (var i in e.OldItems)
                    {
                        if (i is IFitsImage image)
                        {
                            var record = Get(image.File);
                            if (record is Record r)
                            {
                                r.OnContainerImageRemoved(container, false);
                            }
                        }
                    }
                }

                if (e.NewItems != null && (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Reset))
                {
                    foreach (var i in e.NewItems)
                    {
                        if (i is IFitsImage image)
                        {
                            var record = Get(image.File);
                            if (record is Record r)
                            {
                                r.OnContainerImageAdded(container);
                            }
                        }
                    }
                }
            }
        }

        private event EventHandler<IFitsImageManager.RecordChangedEventArgs>? _recordChanged;
        public event EventHandler<IFitsImageManager.RecordChangedEventArgs> RecordChanged
        {
            add => _recordChanged += value;
            remove => _recordChanged -= value;
        }

        private string? _currentFile;
        public string? CurrentFile
        {
            get => _currentFile;
            set
            {
                if (_currentFile != value)
                {
                    var old = _currentFile;
                    _currentFile = value;
                    _currentFileChanged?.Invoke(this, new IFitsImageManager.CurrentFileChangedEventArgs(old, value));
                }
            }
        }

        private event EventHandler<IFitsImageManager.CurrentFileChangedEventArgs>? _currentFileChanged;
        public event EventHandler<IFitsImageManager.CurrentFileChangedEventArgs> CurrentFileChanged
        {
            add => _currentFileChanged += value;
            remove => _currentFileChanged -= value;
        }

        private class ImageContainerRegistrationReleaser : IDisposable
        {
            private readonly FitsImageManager manager;
            private readonly IFitsImageContainer container;

            private volatile bool disposed;

            public ImageContainerRegistrationReleaser(FitsImageManager manager, IFitsImageContainer container)
            {
                this.manager = manager;
                this.container = container;
            }

            public void Dispose()
            {
                lock (this)
                {
                    if (!disposed)
                    {
                        disposed = true;
                        manager.UnregisterImageContainer(container);
                    }
                }
            }
        }
    }
}
