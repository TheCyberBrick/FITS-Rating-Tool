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

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static FitsRatingTool.GuiApp.UI.FitsImage.IFitsImageMultiViewerViewModel;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageMultiViewerViewModel : ViewModelBase, IFitsImageMultiViewerViewModel
    {
        public class Factory : IFactory
        {
            private readonly IFitsImageViewerViewModel.IFactory fitsImageViewerFactory;

            public Factory(IFitsImageViewerViewModel.IFactory fitsImageViewerFactory)
            {
                this.fitsImageViewerFactory = fitsImageViewerFactory;
            }

            public IFitsImageMultiViewerViewModel Create()
            {
                return new FitsImageMultiViewerViewModel(fitsImageViewerFactory);
            }
        }


        private int _maxViewers = 5;
        public int MaxViewers
        {
            get => _maxViewers;
            set => this.RaiseAndSetIfChanged(ref _maxViewers, value);
        }

        public ObservableCollection<Instance> Instances { get; } = new();

        private Instance? _selectedInstance;
        public Instance? SelectedInstance
        {
            get => _selectedInstance;
            set => this.RaiseAndSetIfChanged(ref _selectedInstance, value);
        }

        private string? _file;
        public string? File
        {
            get => _file;
            set => this.RaiseAndSetIfChanged(ref _file, value);
        }

        private IFitsImageViewModel? _fitsImage;
        public IFitsImageViewModel? FitsImage
        {
            get => _fitsImage;
            set => this.RaiseAndSetIfChanged(ref _fitsImage, value);
        }

        private EventHandler<ViewerEventArgs>? _viewerLoaded;
        public event EventHandler<ViewerEventArgs> ViewerLoaded
        {
            add => _viewerLoaded += value;
            remove => _viewerLoaded -= value;
        }

        private EventHandler<ViewerEventArgs>? _viewerUnloaded;
        public event EventHandler<ViewerEventArgs> ViewerUnloaded
        {
            add => _viewerUnloaded += value;
            remove => _viewerUnloaded -= value;
        }


        private IFitsImageViewerViewModel.IOverlayFactory? _innerOverlayFactory;
        public IFitsImageViewerViewModel.IOverlayFactory? InnerOverlayFactory
        {
            get => _innerOverlayFactory;
            set
            {
                var oldValue = _innerOverlayFactory;
                this.RaiseAndSetIfChanged(ref _innerOverlayFactory, value);
                if (oldValue != value)
                {
                    foreach (var instance in Instances)
                    {
                        if (instance.Viewer.InnerOverlayFactory == oldValue)
                        {
                            instance.Viewer.InnerOverlayFactory = value;
                        }
                    }
                }
            }
        }

        private IFitsImageViewerViewModel.IOverlayFactory? _outerOverlayFactory;
        public IFitsImageViewerViewModel.IOverlayFactory? OuterOverlayFactory
        {
            get => _outerOverlayFactory;
            set
            {
                var oldValue = _outerOverlayFactory;
                this.RaiseAndSetIfChanged(ref _outerOverlayFactory, value);
                if (oldValue != value)
                {
                    foreach (var instance in Instances)
                    {
                        if (instance.Viewer.OuterOverlayFactory == oldValue)
                        {
                            instance.Viewer.OuterOverlayFactory = value;
                        }
                    }
                }
            }
        }



        private readonly IFitsImageViewerViewModel.IFactory fitsImageViewerFactory;

        // Designer only
        public FitsImageMultiViewerViewModel()
        {
            fitsImageViewerFactory = null!;
            Instances.Add(new Instance(null!, true));
        }

        public FitsImageMultiViewerViewModel(IFitsImageViewerViewModel.IFactory fitsImageViewerFactory)
        {
            this.fitsImageViewerFactory = fitsImageViewerFactory;

            this.WhenAnyValue(x => x.SelectedInstance!.Viewer.FitsImage).Subscribe(image => FitsImage = image);
            this.WhenAnyValue(x => x.SelectedInstance!.Viewer.File).Subscribe(file => File = file);

            this.WhenAnyValue(x => x.FitsImage).Subscribe(image =>
            {
                var instance = image == null ? null : FindInstance(image);
                if (instance != null)
                {
                    SelectedInstance = instance;
                }
                else if (image != null)
                {
                    ShiftInstances();

                    if (Instances.Count > 0)
                    {
                        var first = Instances.First();
                        first.Viewer.FitsImage = image;
                        SelectedInstance = first;
                    }
                }
            });

            this.WhenAnyValue(x => x.File).Subscribe(file =>
            {
                var instance = file == null ? null : FindInstance(file);
                if (instance != null)
                {
                    SelectedInstance = instance;
                }
                else if (file != null)
                {
                    ShiftInstances();

                    if (Instances.Count > 0)
                    {
                        var first = Instances.First();
                        first.Viewer.File = file;
                        SelectedInstance = first;
                    }
                }
            });

            // Add first instance that always exists
            AddNewInstance();
        }

        private void ShiftInstances()
        {
            if (Instances.Count > 0)
            {
                var baseViewer = Instances.First().Viewer;

                if (baseViewer.FitsImage != null || baseViewer.File != null)
                {
                    AddNewInstance(baseViewer.FitsImage, baseViewer.File, baseViewer.FitsImage?.Owner == baseViewer);
                }
            }
            else
            {
                AddNewInstance();
            }
        }

        private Instance AddNewInstance(IFitsImageViewModel? image = null, string? imageFile = null, bool setOwner = false)
        {
            var newViewer = fitsImageViewerFactory.Create();

            newViewer.InnerOverlayFactory = InnerOverlayFactory;
            newViewer.OuterOverlayFactory = OuterOverlayFactory;

            if (Instances.Count > 0)
            {
                CopyViewerSettings(Instances.First().Viewer, newViewer);
            }

            if (image != null)
            {
                if (setOwner)
                {
                    image.Owner = newViewer;
                }
                newViewer.FitsImage = image;
            }
            else if (imageFile != null)
            {
                newViewer.File = imageFile;
            }

            bool isFirstInstance = Instances.Count == 0;

            var newInstance = new Instance(newViewer, !isFirstInstance);

            newInstance.Close.Subscribe(_ => RemoveInstance(newInstance));

            newInstance.Viewer.ApplyStretchToAll.Subscribe(_ => ApplyStretchToAll(newInstance));

            if (isFirstInstance)
            {
                Instances.Add(newInstance);
                newInstance.Viewer.IsPrimaryViewer = true;
            }
            else
            {
                while (MaxViewers > 0 && Instances.Count >= MaxViewers)
                {
                    RemoveInstance(Instances.Last());
                }

                if (Instances.Count == 0)
                {
                    Instances.Add(newInstance);
                    newInstance.Viewer.IsPrimaryViewer = true;
                }
                else
                {
                    Instances.Insert(1, newInstance);
                    newInstance.Viewer.IsPrimaryViewer = false;
                }
            }

            _viewerLoaded?.Invoke(this, new ViewerEventArgs(newInstance.Viewer, false));

            return newInstance;
        }

        private void CopyViewerSettings(IFitsImageViewerViewModel from, IFitsImageViewerViewModel to)
        {
            to.TransferPropertiesFrom(from);

            if (from.InnerOverlay != null && to.InnerOverlay != null)
            {
                to.InnerOverlay.TransferPropertiesFrom(from.InnerOverlay);
            }

            if (from.OuterOverlay != null && to.OuterOverlay != null)
            {
                to.OuterOverlay.TransferPropertiesFrom(from.OuterOverlay);
            }
        }

        private void RemoveInstance(Instance instance)
        {
            if (SelectedInstance == instance)
            {
                SelectedInstance = null;
            }

            Instances.Remove(instance);

            _viewerUnloaded?.Invoke(this, new ViewerEventArgs(instance.Viewer, true));

            DisposeViewer(instance.Viewer);
        }

        private void ApplyStretchToAll(Instance instance)
        {
            var image = instance.Viewer.FitsImage;
            if (image != null)
            {
                foreach (var otherInstance in Instances)
                {
                    var otherImage = otherInstance.Viewer.FitsImage;

                    if (otherImage != null)
                    {
                        otherImage.Shadows = image.Shadows;
                        otherImage.Midtones = image.Midtones;
                        otherImage.Highlights = image.Highlights;
                        otherImage.PreserveColorBalance = image.PreserveColorBalance;
                        otherInstance.Viewer.KeepStretch = true;
                    }
                }
            }
        }

        private async void DisposeViewer(IFitsImageViewerViewModel viewer)
        {
            var image = viewer.FitsImage;
            bool requiresDisposal = image?.Owner == viewer;

            viewer.FitsImage = null;
            viewer.File = null;

            await viewer.UnloadAsync();

            viewer.Dispose();

            if (image != null && requiresDisposal)
            {
                image.Dispose();
            }
        }

        private Instance? FindInstance(IFitsImageViewModel image)
        {
            return FindInstance(image.File);
        }

        private Instance? FindInstance(string file)
        {
            foreach (var instance in Instances)
            {
                if (file.Equals(instance.Viewer.File) || file.Equals(instance.Viewer.FitsImage?.File))
                {
                    return instance;
                }
            }
            return null;
        }

        public void Dispose()
        {
            List<Instance> instances = new(Instances);
            foreach (var instance in instances)
            {
                RemoveInstance(instance);
            }
        }
    }
}
