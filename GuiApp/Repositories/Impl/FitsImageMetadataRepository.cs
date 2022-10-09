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
using FitsRatingTool.FitsLoader.Models;
using System.IO;

namespace FitsRatingTool.GuiApp.Repositories.Impl
{
    public class FitsImageMetadataRepository : IFitsImageMetadataRepository
    {
        private class Metadata : IFitsImageMetadata
        {
            public string File { get; }

            public string FileName => Path.GetFileName(File);

            private readonly List<IFitsImageHeaderRecord> records = new();

            IEnumerable<IFitsImageHeaderRecord> IFitsImageMetadata.Header => records;

            public int ImageWidth { get; }

            public int ImageHeight { get; }

            public Metadata(IFitsImageMetadata image)
            {
                File = image.File;
                records.AddRange(image.Header);
                ImageWidth = image.ImageWidth;
                ImageHeight = image.ImageHeight;
            }
        }

        private readonly ConcurrentDictionary<string, IFitsImageMetadata> repository = new();

        public void AddMetadata(IFitsImageMetadata image)
        {
            // Creating a copy here so that only the metadata is
            // stored/remains referenced instead of the entire
            // image view model
            var metadata = new Metadata(image);
            repository.AddOrUpdate(image.File, metadata, (file, old) => metadata);
            _metadataChanged?.Invoke(this, new IFitsImageMetadataRepository.RepositoryChangedEventArgs(image.File));
        }

        public IFitsImageMetadata? GetMetadata(string file)
        {
            if (repository.TryGetValue(file, out var metadata))
            {
                return metadata;
            }
            return null;
        }

        public IFitsImageMetadata? RemoveMetadata(string file)
        {
            if (repository.Remove(file, out var metadata))
            {
                _metadataChanged?.Invoke(this, new IFitsImageMetadataRepository.RepositoryChangedEventArgs(file));
                return metadata;
            }
            return null;
        }

        private event EventHandler<IFitsImageMetadataRepository.RepositoryChangedEventArgs>? _metadataChanged;
        public event EventHandler<IFitsImageMetadataRepository.RepositoryChangedEventArgs> MetadataChanged
        {
            add
            {
                _metadataChanged += value;
            }
            remove
            {
                _metadataChanged -= value;
            }
        }
    }
}
