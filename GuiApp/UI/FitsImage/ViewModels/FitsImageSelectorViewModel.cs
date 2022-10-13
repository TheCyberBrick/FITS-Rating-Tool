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

using Avalonia.Utilities;
using FitsRatingTool.Common.Models.FitsImage;
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageSelectorViewModel : ViewModelBase, IFitsImageSelectorViewModel
    {
        public class Factory : IFitsImageSelectorViewModel.IFactory
        {
            private readonly IFitsImageManager fitsImageManager;
            private readonly IFitsImageViewModel.IFactory fitsImageFactory;

            public Factory(IFitsImageManager fitsImageManager, IFitsImageViewModel.IFactory fitsImageFactory)
            {
                this.fitsImageManager = fitsImageManager;
                this.fitsImageFactory = fitsImageFactory;
            }

            public IFitsImageSelectorViewModel Create()
            {
                return new FitsImageSelectorViewModel(fitsImageManager, fitsImageFactory);
            }
        }

        public ObservableCollection<string> Files { get; } = new();

        private bool _selectCurrentFile;
        public bool SelectCurrentFile
        {
            get => _selectCurrentFile;
            set => this.RaiseAndSetIfChanged(ref _selectCurrentFile, value);
        }

        private string? _selectedFile;
        public string? SelectedFile
        {
            get => _selectedFile;
            set => this.RaiseAndSetIfChanged(ref _selectedFile, value);
        }

        public IFitsImageViewModel? SelectedImage { get; private set; }


        private string? prevSelectedFile;

        private readonly IFitsImageManager fitsImageManager;
        private readonly IFitsImageViewModel.IFactory fitsImageFactory;

        private FitsImageSelectorViewModel(IFitsImageManager fitsImageManager, IFitsImageViewModel.IFactory fitsImageFactory)
        {
            this.fitsImageManager = fitsImageManager;
            this.fitsImageFactory = fitsImageFactory;

            this.WhenAnyValue(x => x.SelectedFile).Subscribe(file =>
            {
                prevSelectedFile = file;
                UpdateSelectedImage();
            });

            this.WhenAnyValue(x => x.SelectCurrentFile).Subscribe(x =>
            {
                if (x)
                {
                    var prev = SelectedFile;
                    SelectedFile = fitsImageManager.CurrentFile;
                    prevSelectedFile = prev;
                }
                else
                {
                    SelectedFile = prevSelectedFile;
                }
            });

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, FitsImageSelectorViewModel>(fitsImageManager, nameof(fitsImageManager.RecordChanged), OnRecordChanged);
            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.CurrentFileChangedEventArgs, FitsImageSelectorViewModel>(fitsImageManager, nameof(fitsImageManager.CurrentFileChanged), OnCurrentFileChanged);

            UpdateFiles();
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.ImageContainers || (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.File && args.Removed))
            {
                UpdateFiles();
            }
        }

        private void UpdateFiles()
        {
            var newFiles = FindFilesWithImages();
            var newFilesSet = new HashSet<string>(newFiles);

            for (int i = Files.Count - 1; i >= 0; --i)
            {
                var file = Files[i];

                if (!newFilesSet.Contains(file))
                {
                    Files.RemoveAt(i);
                }
            }

            foreach (var newFile in newFiles)
            {
                if (!Files.Contains(newFile))
                {
                    Files.Add(newFile);
                }
            }
        }

        private List<string> FindFilesWithImages()
        {
            List<string> foundFiles = new();

            foreach (var file in fitsImageManager.Files)
            {
                if (FindImage(file) != null)
                {
                    foundFiles.Add(file);
                }
            }

            return foundFiles;
        }

        private void OnCurrentFileChanged(object? sender, IFitsImageManager.CurrentFileChangedEventArgs args)
        {
            if (SelectCurrentFile)
            {
                var prev = prevSelectedFile;
                SelectedFile = args.NewFile;
                prevSelectedFile = prev;
            }
        }

        private void UpdateSelectedImage()
        {
            var selectedFile = SelectedFile;
            var selectedImage = SelectedImage;

            if (selectedFile == null)
            {
                DisposeSelectedImage();
            }
            else if (selectedImage == null)
            {
                SelectedImage = CreateImage(selectedFile);
            }
            else if (selectedImage.File != selectedFile)
            {
                DisposeSelectedImage();
                SelectedImage = CreateImage(selectedFile);
            }
        }

        private IFitsImageViewModel? CreateImage(string file)
        {
            var image = FindImage(file);
            if (image != null)
            {
                return fitsImageFactory.Create(image);
            }
            return null;
        }

        private IFitsImage? FindImage(string file)
        {
            var record = fitsImageManager.Get(file);
            if (record != null)
            {
                return record.FindImage(file, i => i.OutDim.Width == i.ImageWidth && i.OutDim.Height == i.ImageHeight);
            }
            return null;
        }

        private void DisposeSelectedImage()
        {
            var selectedImage = SelectedImage;
            SelectedImage = null;
            selectedImage?.Dispose();
        }

        public void Dispose()
        {
            DisposeSelectedImage();
        }
    }
}
