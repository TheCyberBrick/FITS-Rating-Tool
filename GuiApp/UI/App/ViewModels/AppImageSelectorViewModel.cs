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
using FitsRatingTool.GuiApp.Models;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.FitsImage;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppImageSelectorViewModel : ViewModelBase, IAppImageSelectorViewModel
    {
        public AppImageSelectorViewModel(IRegistrar<IAppImageSelectorViewModel, IAppImageSelectorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<AppImageSelectorViewModel>();
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

        private IFitsImageViewModel? _selectedImage;
        public IFitsImageViewModel? SelectedImage
        {
            get => _selectedImage;
            private set => this.RaiseAndSetIfChanged(ref _selectedImage, value);
        }

        public Predicate<IFitsImage> SelectedImageFilter { get; set; }


        private string? prevSelectedFile;

        private bool isImageLoading;

        private readonly IFitsImageManager fitsImageManager;
        private readonly IContainer<IFitsImageViewModel, IFitsImageViewModel.OfImage> fitsImageContainer;

        private AppImageSelectorViewModel(IAppImageSelectorViewModel.Of args, IFitsImageManager fitsImageManager,
            IContainer<IFitsImageViewModel, IFitsImageViewModel.OfImage> fitsImageContainer)
        {
            this.fitsImageManager = fitsImageManager;
            this.fitsImageContainer = fitsImageContainer.ToSingleton();

            fitsImageContainer.ToSingletonWithObservable().Subscribe(vm => SelectedImage = vm);

            // Select only full images by default
            SelectedImageFilter = i => i.OutDim.Width == i.ImageWidth && i.OutDim.Height == i.ImageHeight;

            SubscribeToEvent<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, AppImageSelectorViewModel>(fitsImageManager, nameof(fitsImageManager.RecordChanged), OnRecordChanged);
            SubscribeToEvent<IFitsImageManager, IFitsImageManager.CurrentFileChangedEventArgs, AppImageSelectorViewModel>(fitsImageManager, nameof(fitsImageManager.CurrentFileChanged), OnCurrentFileChanged);
        }

        protected override void OnInstantiated()
        {
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

            this.WhenAnyValue(x => x.SelectedFile).Subscribe(file =>
            {
                prevSelectedFile = file;
                UpdateSelectedImage();
            });

            UpdateFiles();
            UpdateSelectedImage();
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.ImageContainers)
            {
                UpdateFiles();

                if (!isImageLoading)
                {
                    UpdateSelectedImage();
                }
            }
            else if (args.Type == IFitsImageManager.RecordChangedEventArgs.DataType.File && args.Removed)
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

            UpdateSelectedFile();
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
            UpdateSelectedFile();
        }

        private void UpdateSelectedFile()
        {
            if (SelectCurrentFile)
            {
                var prev = prevSelectedFile;

                var currentFile = fitsImageManager.CurrentFile;

                if (currentFile != null && Files.Contains(currentFile))
                {
                    SelectedFile = currentFile;
                }
                else
                {
                    SelectedFile = null;
                }

                prevSelectedFile = prev;
            }
        }

        private void UpdateSelectedImage()
        {
            var selectedFile = SelectedFile;
            var selectedImage = SelectedImage;

            if (selectedFile == null)
            {
                fitsImageContainer.Destroy();
            }
            else if (selectedImage == null || selectedImage.File != selectedFile)
            {
                var image = FindImage(selectedFile);
                if (image != null)
                {
                    try
                    {
                        isImageLoading = true;
                        fitsImageContainer.Instantiate(new IFitsImageViewModel.OfImage(image));
                    }
                    finally
                    {
                        isImageLoading = false;
                    }
                }
                else
                {
                    fitsImageContainer.Destroy();
                }
            }
        }

        private IFitsImage? FindImage(string file)
        {
            var record = fitsImageManager.Get(file);
            if (record != null)
            {
                return record.FindImage(file, SelectedImageFilter);
            }
            return null;
        }
    }
}
